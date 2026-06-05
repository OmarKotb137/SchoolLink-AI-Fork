class ChatManager {
    constructor() {
        this.messagesContainer = document.getElementById('chatMessages');
        this.userInput = document.getElementById('userInput');
        this.sendButton = document.getElementById('sendButton');
        this.loadingIndicator = document.getElementById('loadingIndicator');

        this.conversationId = this.generateConversationId();
        this.messages = [];
        this.localStorageKey = `chat_history_${this.conversationId}`;

        this.setupEventListeners();
        this.loadSavedMessages();
    }

    generateConversationId() {
        let id = sessionStorage.getItem('examAgentConvId');
        if (!id) {
            id = 'conv_' + Date.now() + '_' + Math.random().toString(36).substr(2, 9);
            sessionStorage.setItem('examAgentConvId', id);
        }
        return id;
    }

    loadSavedMessages() {
        const saved = localStorage.getItem(this.localStorageKey);
        if (saved) {
            try {
                this.messages = JSON.parse(saved);
                this.messages.forEach(msg => {
                    this.displayMessage(msg.content, msg.sender, false);
                });
                this.scrollToBottom();
            } catch (e) {
                console.error('Error loading saved messages:', e);
            }
        }
    }

    saveMessage(content, sender) {
        const message = {
            content: content,
            sender: sender,
            timestamp: new Date().toISOString()
        };
        this.messages.push(message);

        try {
            localStorage.setItem(this.localStorageKey, JSON.stringify(this.messages));
        } catch (e) {
            console.warn('Could not save to localStorage:', e);
        }

        this.saveToServer(message);
    }

    async saveToServer(message) {
        try {
            await fetch('/api/ai/exam-agent/save-message', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    conversationId: this.conversationId,
                    content: message.content,
                    sender: message.sender,
                    timestamp: message.timestamp
                })
            });
        } catch (error) {
            console.warn('Could not save message to server:', error);
        }
    }

    setupEventListeners() {
        this.sendButton.addEventListener('click', () => this.sendMessage());
        this.userInput.addEventListener('keypress', (e) => {
            if (e.key === 'Enter' && !e.shiftKey) {
                e.preventDefault();
                this.sendMessage();
            }
        });
    }

    async sendMessage() {
        const message = this.userInput.value.trim();
        if (!message) return;

        this.displayMessage(message, 'user');
        this.saveMessage(message, 'user');
        this.userInput.value = '';
        this.userInput.focus();

        this.showLoading(true);

        try {
            const response = await fetch('/api/ai/exam-agent/chat', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    message: message,
                    conversationId: this.conversationId
                })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const data = await response.json();

            if (data.conversationId) {
                this.conversationId = data.conversationId;
                sessionStorage.setItem('examAgentConvId', this.conversationId);
                this.localStorageKey = `chat_history_${this.conversationId}`;
            }

            let responseText = '';
            if (data.response) {
                if (typeof data.response === 'string') {
                    responseText = data.response;
                } else if (data.response.answer) {
                    responseText = data.response.answer;
                } else if (typeof data.response === 'object') {
                    responseText = JSON.stringify(data.response, null, 2);
                }
            }

            if (responseText) {
                this.displayMessage(responseText, 'bot');
                this.saveMessage(responseText, 'bot');
            }

            this.scrollToBottom();
        } catch (error) {
            console.error('Error:', error);
            const errorMsg = 'عذراً، حدث خطأ في معالجة طلبك. يرجى المحاولة لاحقاً.';
            this.displayMessage(errorMsg, 'bot');
            this.saveMessage(errorMsg, 'bot');
        } finally {
            this.showLoading(false);
        }
    }

    displayMessage(content, sender, shouldScroll = true) {
        const messageDiv = document.createElement('div');
        messageDiv.className = `message ${sender}-message`;

        const time = new Date().toLocaleTimeString('ar-SA', {
            hour: '2-digit',
            minute: '2-digit'
        });

        let contentHTML = '';
        if (sender === 'bot') {
            contentHTML = this.parseResponse(content);
        } else {
            contentHTML = `<p>${this.escapeHtml(content)}</p>`;
        }

        messageDiv.innerHTML = `
            <div class="message-content">${contentHTML}</div>
            <div class="message-time">${time}</div>
        `;

        this.messagesContainer.appendChild(messageDiv);
        if (shouldScroll) {
            this.scrollToBottom();
        }
    }

    parseResponse(content) {
        if (typeof content !== 'string') {
            content = String(content);
        }

        if (content.trim().startsWith('{') || content.trim().startsWith('[')) {
            try {
                const jsonData = JSON.parse(content);
                return this.formatJson(jsonData);
            } catch {
                return this.formatAsText(content);
            }
        }

        return this.formatAsText(content);
    }

    formatAsText(content) {
        const escaped = this.escapeHtml(content);
        const formatted = escaped
            .split('\n')
            .map(line => line.trim() ? `<p>${line}</p>` : '<br>')
            .join('');

        return `<div style="line-height: 1.8; white-space: pre-wrap; word-wrap: break-word;">${formatted}</div>`;
    }

    formatJson(obj, indent = 0) {
        let html = '';

        if (obj === null || obj === undefined) {
            return `<p>-</p>`;
        }

        if (typeof obj === 'string') {
            return `<p>${this.escapeHtml(obj)}</p>`;
        }

        if (typeof obj === 'number' || typeof obj === 'boolean') {
            return `<p>${String(obj)}</p>`;
        }

        if (Array.isArray(obj)) {
            html += '<ul style="margin: 10px 0; padding-right: 20px;">';
            obj.forEach(item => {
                if (item === null || item === undefined) {
                    html += '<li>-</li>';
                } else if (typeof item === 'object') {
                    html += '<li>' + this.formatJson(item, indent + 1) + '</li>';
                } else {
                    html += `<li>${this.escapeHtml(String(item))}</li>`;
                }
            });
            html += '</ul>';
            return html;
        }

        if (typeof obj === 'object') {
            html += '<div style="margin: 10px 0;">';
            const entries = Object.entries(obj);
            if (entries.length === 0) {
                html += '<p style="color: #999;">بدون بيانات</p>';
            } else {
                for (const [key, value] of entries) {
                    const keyName = this.camelToArabic(key);
                    if (value === null || value === undefined) {
                        html += `<div style="margin: 5px 0;"><strong>${keyName}:</strong> -</div>`;
                    } else if (typeof value === 'object') {
                        html += `<div style="margin: 5px 0;"><strong>${keyName}:</strong> ${this.formatJson(value, indent + 1)}</div>`;
                    } else {
                        html += `<div style="margin: 5px 0;"><strong>${keyName}:</strong> ${this.escapeHtml(String(value))}</div>`;
                    }
                }
            }
            html += '</div>';
            return html;
        }

        return `<p>${this.escapeHtml(String(obj))}</p>`;
    }

    camelToArabic(str) {
        const mapping = {
            'title': 'العنوان',
            'content': 'المحتوى',
            'questions': 'الأسئلة',
            'answer': 'الإجابة',
            'options': 'الخيارات',
            'difficulty': 'الصعوبة',
            'id': 'المعرف',
            'name': 'الاسم',
            'description': 'الوصف',
            'lessons': 'الدروس',
            'exam': 'الامتحان'
        };
        return mapping[str.toLowerCase()] || str;
    }

    escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    showLoading(show) {
        this.loadingIndicator.style.display = show ? 'flex' : 'none';
    }

    scrollToBottom() {
        this.messagesContainer.scrollTop = this.messagesContainer.scrollHeight;
    }
}

document.addEventListener('DOMContentLoaded', () => {
    new ChatManager();
});
