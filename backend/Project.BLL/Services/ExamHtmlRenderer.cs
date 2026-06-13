using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.DAL.Context;
using Project.Domain.Entities;
using Project.Domain.Enums;

namespace Project.BLL.Services
{
    public class ExamHtmlRenderer : IExamHtmlRenderer
    {
        private readonly AppDbContext _db;

        public ExamHtmlRenderer(AppDbContext db)
        {
            _db = db;
        }

        public async Task<string> RenderExamAsync(int examId, CancellationToken ct = default)
        {
            var exam = await _db.Exams
                .Include(e => e.ClassSubjectTeacher).ThenInclude(c => c.Subject)
                .FirstOrDefaultAsync(e => e.Id == examId, ct);

            if (exam == null || exam.IsDeleted)
                return "<p>الامتحان غير موجود</p>";

            var groups = await _db.Set<ExamQuestionGroup>()
                .Include(g => g.Questions.Where(q => !q.IsDeleted)
                    .OrderBy(q => q.DisplayOrder))
                    .ThenInclude(q => q.Options.Where(o => !o.IsDeleted)
                        .OrderBy(o => o.DisplayOrder))
                .Where(g => g.ExamId == examId && !g.IsDeleted)
                .OrderBy(g => g.DisplayOrder)
                .ToListAsync(ct);

            var standalone = await _db.ExamQuestions
                .Include(q => q.Options.Where(o => !o.IsDeleted).OrderBy(o => o.DisplayOrder))
                .Where(q => q.ExamId == examId && q.GroupId == null && !q.IsDeleted)
                .OrderBy(q => q.DisplayOrder)
                .ToListAsync(ct);

            var subj = exam.ClassSubjectTeacher?.Subject?.Name ?? "–";
            var uid = exam.Uid;

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang='ar' dir='rtl'><head><meta charset='utf-8'>");
            sb.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.Append("<title>").Append(Enc(exam.Title)).Append("</title>");
            sb.Append("<style>").Append(RenderCss()).Append("</style>");
            sb.Append("</head><body>");

            sb.Append("<div id='app'>");

            // ── Toolbar ──
            sb.Append(RenderToolbar());

            // ── Header ──
            sb.Append(RenderHeader(exam, subj));

            // ── Questions ──
            sb.Append("<main class='exam-body' id='examBody'>");

            int qNumber = 1;
            var blocks = BuildBlocks(groups, standalone);
            foreach (var block in blocks)
            {
                if (block.Group != null)
                    sb.Append(RenderGroup(block.Group, ref qNumber, uid));
                else if (block.Question != null)
                    sb.Append(RenderSingleQuestion(block.Question, ref qNumber, uid));
            }

            sb.Append("</main>");

            // ── Footer ──
            sb.Append(RenderFooter());

            sb.Append("</div>");

            // ── JavaScript ──
            sb.Append("<script>").Append(RenderJavaScript(uid)).Append("</script>");
            sb.Append("</body></html>");

            return sb.ToString();
        }

        private string RenderCss()
        {
            return @"
*{box-sizing:border-box;margin:0;padding:0}
@page{size:A4;margin:15mm 12mm}

body{
  font-family:'Cairo','Traditional Arabic','Times New Roman',sans-serif;
  background:#f0f2f5;
  color:#1a1a2e;
  line-height:1.8;
  font-size:14px;
}

#app{max-width:900px;margin:20px auto;background:#fff;border-radius:16px;box-shadow:0 8px 40px rgba(0,0,0,0.1);overflow:hidden}

/* ── Toolbar ── */
.toolbar{
  display:flex;gap:8px;padding:12px 20px;
  background:linear-gradient(135deg,#667eea,#764ba2);
  position:sticky;top:0;z-index:100;
  flex-wrap:wrap;
}
.toolbar button,.toolbar select{
  padding:8px 18px;border:none;border-radius:8px;
  font-size:13px;font-weight:600;cursor:pointer;
  transition:all .2s;font-family:inherit;
  display:inline-flex;align-items:center;gap:6px;
}
.btn-save{background:#22c55e;color:#fff}
.btn-save:hover{background:#16a34a;transform:translateY(-1px)}
.btn-print{background:#fff;color:#667eea}
.btn-print:hover{background:#f0f0ff;transform:translateY(-1px)}
.btn-edit-toggle{background:#fbbf24;color:#1a1a2e}
.btn-edit-toggle:hover{background:#f59e0b;transform:translateY(-1px)}
.btn-cancel{background:#ef4444;color:#fff}
.btn-cancel:hover{background:#dc2626}
.toast{
  position:fixed;top:20px;left:50%;transform:translateX(-50%);
  padding:12px 28px;border-radius:12px;font-size:14px;font-weight:600;
  z-index:9999;display:none;box-shadow:0 4px 20px rgba(0,0,0,0.15);
  animation:fadeIn .3s;
}
.toast.success{background:#22c55e;color:#fff}
.toast.error{background:#ef4444;color:#fff}
@keyframes fadeIn{from{opacity:0;transform:translateX(-50%) translateY(-10px)}to{opacity:1;transform:translateX(-50%) translateY(0)}}
.toast.show{display:block}

/* ── Header ── */
.exam-header{
  text-align:center;padding:28px 24px 18px;
  background:linear-gradient(180deg,#f8f9ff,#fff);
  border-bottom:2px solid #e8e8ff;
}
.exam-header h1{
  font-size:24px;font-weight:800;margin:0 0 6px;
  background:linear-gradient(135deg,#667eea,#764ba2);
  -webkit-background-clip:text;-webkit-text-fill-color:transparent;
  background-clip:text;
}
.exam-meta{
  display:flex;justify-content:center;gap:24px;flex-wrap:wrap;
  font-size:13px;color:#64748b;
}
.exam-meta span{display:inline-flex;align-items:center;gap:4px}

/* ── Exam Body ── */
.exam-body{padding:20px 28px 10px}
.exam-block{margin-bottom:24px;page-break-inside:avoid}

/* ── Passage ── */
.passage-box{
  background:#f8f9ff;border:1.5px solid #e8e8ff;
  border-radius:12px;padding:16px 20px;margin-bottom:14px;
}
.passage-title{margin:0 0 8px;font-size:15px;font-weight:700;color:#667eea}
.passage-text{text-align:justify;color:#334155}
.exam-figure{text-align:center;margin:12px 0}
.exam-figure img{max-width:100%;max-height:380px;border-radius:8px;border:1px solid #e2e8f0;display:inline-block}
.exam-figure svg{max-width:100%;max-height:380px;display:inline-block}
.exam-figure figcaption{font-size:12px;margin-top:5px;color:#94a3b8}

/* ── Question ── */
.q-item{
  background:#fff;border:1.5px solid #e2e8f0;
  border-radius:12px;padding:16px 18px;margin:10px 0;
  transition:border-color .2s,box-shadow .2s;
  position:relative;
}
.q-item:hover{border-color:#c7d2fe;box-shadow:0 2px 12px rgba(102,126,234,0.08)}
.q-header{display:flex;justify-content:space-between;align-items:flex-start;margin-bottom:6px;gap:8px}
.q-number{font-weight:800;color:#667eea;font-size:15px;white-space:nowrap}
.q-text{font-weight:600;margin:0;flex:1;line-height:1.7}
.q-points{
  font-size:12px;color:#94a3b8;white-space:nowrap;
  background:#f1f5f9;padding:2px 10px;border-radius:20px;
}
.q-options{list-style:none;padding:0;margin:8px 0 0}
.q-options li{
  padding:8px 14px;margin:4px 0;border-radius:10px;
  border:1.5px solid #e2e8f0;cursor:default;
  transition:all .15s;position:relative;
  display:flex;align-items:center;gap:8px;
}
.q-options li:before{
  content:counter(opt-counter,'') '';
  counter-increment:opt-counter;
  display:inline-flex;align-items:center;justify-content:center;
  width:24px;height:24px;border-radius:50%;
  background:#f1f5f9;color:#64748b;font-size:12px;font-weight:700;
  flex-shrink:0;
}
.q-options{ counter-reset:opt-counter; }
.q-options li.correct{border-color:#22c55e;background:#f0fdf4}
.q-options li.correct:before{background:#22c55e;color:#fff}
.answer-line{border-bottom:2px dashed #cbd5e1;min-height:32px;margin:8px 0;width:85%;border-radius:0}
.answer-line.tall{min-height:60px}
.correct-answer{margin-top:6px;padding:6px 12px;background:#f0fdf4;border:1px solid #bbf7d0;border-radius:8px;font-size:13px;color:#15803d;display:inline-block}

/* ── Edit Mode ── */
.editing .q-item{border-color:#fbbf24;box-shadow:0 0 0 3px rgba(251,191,36,0.15)}
.edit-input{
  width:100%;padding:8px 12px;border:2px solid #e2e8f0;
  border-radius:8px;font-family:inherit;font-size:14px;
  transition:border-color .2s;background:#fff;color:#1a1a2e;
}
.edit-input:focus{outline:none;border-color:#667eea;box-shadow:0 0 0 3px rgba(102,126,234,0.15)}
.edit-textarea{
  width:100%;padding:8px 12px;border:2px solid #e2e8f0;
  border-radius:8px;font-family:inherit;font-size:14px;resize:vertical;
  min-height:40px;transition:border-color .2s;background:#fff;color:#1a1a2e;
}
.edit-textarea:focus{outline:none;border-color:#667eea;box-shadow:0 0 0 3px rgba(102,126,234,0.15)}
.edit-points{width:70px;text-align:center}
.edit-opt-row{display:flex;align-items:center;gap:6px;margin:4px 0}
.edit-opt-row input[type=text]{flex:1}
.edit-opt-row input[type=checkbox]{width:18px;height:18px;cursor:pointer}

/* ── Footer ── */
.exam-footer{
  text-align:center;padding:18px 24px;
  border-top:1px solid #e2e8f0;
  color:#94a3b8;font-size:13px;
}

/* ── Loading / Spinner ── */
.spinner{display:inline-block;width:18px;height:18px;border:2px solid #fff;border-top-color:transparent;border-radius:50%;animation:spin .6s linear}
@keyframes spin{to{transform:rotate(360deg)}}

/* ── Print ── */
@media print{
  body{background:#fff}
  #app{max-width:100%;margin:0;border-radius:0;box-shadow:none}
  .toolbar{display:none!important}
  .q-item{border-color:#ccc;box-shadow:none;page-break-inside:avoid}
  .exam-body{padding:10px 15px}
  .q-options li{border-color:#ccc;background:#fff!important}
  .q-options li.correct{border-color:#000!important;background:#f9f9f9!important}
  .edit-input,.edit-textarea{border:none!important;background:transparent!important;padding:0!important;box-shadow:none!important}
  .edit-points{display:none}
  .q-number{display:none}
  .q-points{background:transparent;padding:0}
}

/* ── Tablet / Mobile ── */
@media(max-width:640px){
  #app{margin:10px;border-radius:12px}
  .exam-body{padding:12px 14px}
  .exam-header h1{font-size:19px}
  .exam-meta{gap:12px;font-size:12px}
  .q-item{padding:12px}
  .toolbar{padding:10px 12px;justify-content:center}
  .q-header{flex-wrap:wrap}
}
";
        }

        private string RenderToolbar()
        {
            return @"
<div class='toolbar' id='toolbar'>
  <button class='btn-edit-toggle' id='editToggleBtn' onclick='toggleEdit()'>
    <span>✏️</span><span>تعديل الأسئلة</span>
  </button>
  <button class='btn-save' id='saveBtn' onclick='saveExam()' style='display:none'>
    <span>💾</span><span>حفظ التعديلات</span>
  </button>
  <button class='btn-cancel' id='cancelBtn' onclick='cancelEdit()' style='display:none'>
    <span>❌</span><span>إلغاء</span>
  </button>
  <button class='btn-print' onclick='window.print()'>
    <span>🖨️</span><span>طباعة</span>
  </button>
</div>
<div class='toast' id='toast'></div>
";
        }

        private string RenderHeader(Exam exam, string subjectName)
        {
            return $@"
<header class='exam-header'>
  <h1>{Enc(exam.Title)}</h1>
  <div class='exam-meta'>
    <span>📚 <span id='headerSubject'>{Enc(subjectName)}</span></span>
    <span>⏱ <span id='headerDuration'>{exam.DurationMinutes}</span> دقيقة</span>
    <span>📊 الدرجة الكلية: <span id='headerTotal'>{exam.TotalScore:0.##}</span></span>
  </div>
  <div style='margin-top:10px;font-size:13px;color:#64748b' id='headerMeta'>
    <span id='headerQuestionsCount'></span>
  </div>
</header>
";
        }

        private string RenderGroup(ExamQuestionGroup g, ref int qNumber, Guid uid)
        {
            var sb = new StringBuilder();
            sb.Append("<section class='exam-block'>");

            switch (g.DisplayType)
            {
                case TemplateContentType.Passage:
                    sb.Append("<table class='exam-content' style='width:100%'><tr><td>");
                    sb.Append("<div class='passage-box'>");
                    if (!string.IsNullOrWhiteSpace(g.ContentTitle))
                        sb.Append($"<div class='passage-title'>{Enc(g.ContentTitle)}</div>");
                    sb.Append($"<div class='passage-text'>{Enc(g.ContentText)}</div>");
                    sb.Append("</div>");
                    sb.Append("</td></tr></table>");
                    break;

                case TemplateContentType.Image:
                case TemplateContentType.Map:
                    sb.Append("<figure class='exam-figure'>");
                    if (!string.IsNullOrWhiteSpace(g.ImageUrl))
                        sb.Append($"<img src='{Enc(g.ImageUrl)}' alt='{Enc(g.ContentText)}' />");
                    if (!string.IsNullOrWhiteSpace(g.ContentText))
                        sb.Append($"<figcaption>{Enc(g.ContentText)}</figcaption>");
                    sb.Append("</figure>");
                    break;

                case TemplateContentType.Diagram:
                    sb.Append($"<div class='exam-figure'>{g.ContentText}</div>");
                    break;
            }

            foreach (var q in g.Questions.OrderBy(x => x.DisplayOrder))
                sb.Append(RenderQuestionBody(q, qNumber++, uid));

            sb.Append("</section>");
            return sb.ToString();
        }

        private string RenderSingleQuestion(ExamQuestion q, ref int qNumber, Guid uid)
        {
            var sb = new StringBuilder();
            sb.Append("<section class='exam-block'>");

            if (q.DisplayType == TemplateContentType.Diagram && !string.IsNullOrWhiteSpace(q.ContentText))
                sb.Append($"<div class='exam-figure'>{q.ContentText}</div>");

            sb.Append(RenderQuestionBody(q, qNumber++, uid));
            sb.Append("</section>");
            return sb.ToString();
        }

        private string RenderQuestionBody(ExamQuestion q, int n, Guid uid)
        {
            var sb = new StringBuilder();
            sb.Append($"<div class='q-item' data-qid='{q.Id}' data-qt='{(int)q.QuestionType}'>");
            sb.Append("<div class='q-header'>");
            sb.Append($"<span class='q-number'>س{n}:</span>");
            sb.Append($"<span class='q-text' id='qtext_{q.Id}'>{n}. {Enc(q.QuestionText)}</span>");
            sb.Append($"<span class='q-points' id='qpts_{q.Id}'>{q.Points:0.##}</span>");
            sb.Append("</div>");

            switch (q.QuestionType)
            {
                case QuestionType.MultipleChoice:
                case QuestionType.TrueFalse:
                    sb.Append("<ol class='q-options' id='qopts_").Append(q.Id).Append("'>");
                    foreach (var opt in q.Options.OrderBy(o => o.DisplayOrder))
                    {
                        var cls = opt.IsCorrect ? " class='correct'" : "";
                        sb.Append($"<li{cls} data-optid='{opt.Id}'>{Enc(opt.OptionText)}</li>");
                    }
                    sb.Append("</ol>");
                    break;

                case QuestionType.FillBlank:
                    sb.Append("<div class='answer-line'></div>");
                    if (!string.IsNullOrWhiteSpace(q.CorrectAnswer))
                        sb.Append($"<div class='correct-answer' id='correct_{q.Id}' data-answer='{Enc(q.CorrectAnswer)}'>✅ الإجابة: {Enc(q.CorrectAnswer)}</div>");
                    else
                        sb.Append($"<div class='correct-answer' id='correct_{q.Id}' data-answer='' style='display:none'></div>");
                    break;

                case QuestionType.Essay:
                    sb.Append("<div class='answer-line tall'></div>");
                    sb.Append("<div class='answer-line tall'></div>");
                    if (!string.IsNullOrWhiteSpace(q.CorrectAnswer))
                        sb.Append($"<div class='correct-answer' id='correct_{q.Id}' data-answer='{Enc(q.CorrectAnswer)}'>✅ الإجابة النموذجية: {Enc(q.CorrectAnswer)}</div>");
                    else
                        sb.Append($"<div class='correct-answer' id='correct_{q.Id}' data-answer='' style='display:none'></div>");
                    break;
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string RenderFooter()
        {
            return "<footer class='exam-footer'>— انتهت الأسئلة ، مع تمنياتنا بالتوفيق —</footer>";
        }

        private string RenderJavaScript(Guid uid)
        {
            return $@"
(function(){{
  let editMode = false;
  let originalData = null;

  window.toggleEdit = function(){{
    editMode = !editMode;
    if (editMode) enableEdit(); else disableEdit();
  }};

  window.cancelEdit = function(){{
    if (originalData) restoreData(originalData);
    editMode = false;
    disableEdit();
  }};

  function enableEdit(){{
    document.getElementById('editToggleBtn').innerHTML = '<span>🔒</span><span>إنهاء التعديل</span>';
    document.getElementById('saveBtn').style.display = '';
    document.getElementById('cancelBtn').style.display = '';
    document.body.classList.add('editing');

    // Save original data for cancel
    originalData = captureData();

    // Make questions editable
    document.querySelectorAll('.q-item').forEach(function(item){{
      var qid = item.dataset.qid;
      var qt = parseInt(item.dataset.qt); // question type
      var textEl = document.getElementById('qtext_' + qid);
      if (textEl){{
        var txt = textEl.textContent.replace(/^\d+\.\s*/, '');
        var inp = document.createElement('textarea');
        inp.className = 'edit-textarea';
        inp.value = txt;
        inp.dataset.original = txt;
        inp.id = 'edit_qtext_' + qid;
        textEl.innerHTML = '';
        textEl.appendChild(inp);
      }}

      var ptsEl = document.getElementById('qpts_' + qid);
      if (ptsEl){{
        var pts = ptsEl.textContent.trim();
        var inp = document.createElement('input');
        inp.type = 'number';
        inp.step = '0.5';
        inp.min = '0';
        inp.className = 'edit-input edit-points';
        inp.value = pts;
        inp.dataset.original = pts;
        inp.id = 'edit_qpts_' + qid;
        ptsEl.innerHTML = '';
        ptsEl.appendChild(inp);
      }}

      // Make correct answer editable for FillBlank/Essay
      if (qt === 3 || qt === 4) {{
        var caEl = document.getElementById('correct_' + qid);
        if (caEl) {{
          var answer = caEl.dataset.answer || '';
          caEl.style.display = '';
          caEl.innerHTML = '✅ الإجابة: <input type=""text"" class=""edit-input"" style=""width:70%;display:inline-block"" value=""' + escHtml(answer) + '"" id=""edit_correct_' + qid + '"" data-original=""' + escHtml(answer) + '"">';
        }}
      }}

      // Make options editable
      var optsEl = document.getElementById('qopts_' + qid);
      if (optsEl){{
        var lis = optsEl.querySelectorAll('li');
        lis.forEach(function(li){{
          var oid = li.dataset.optid;
          var txt = li.textContent.trim();
          var isCorrect = li.classList.contains('correct');
          var row = document.createElement('div');
          row.className = 'edit-opt-row';
          row.innerHTML = '<input type=""checkbox"" ' + (isCorrect ? 'checked' : '') + ' id=""edit_optcb_' + oid + '"">' +
            '<input type=""text"" class=""edit-input"" value=""' + escHtml(txt) + '"" id=""edit_opttxt_' + oid + '"" data-original=""' + escHtml(txt) + '"">';
          li.innerHTML = '';
          li.appendChild(row);
        }});
      }}
    }});
  }}

  function disableEdit(){{
    document.getElementById('editToggleBtn').innerHTML = '<span>✏️</span><span>تعديل الأسئلة</span>';
    document.getElementById('saveBtn').style.display = 'none';
    document.getElementById('cancelBtn').style.display = 'none';
    document.body.classList.remove('editing');
    originalData = null;

    // Remove input elements and restore static display
    document.querySelectorAll('.q-item').forEach(function(item){{
      var qid = item.dataset.qid;
      var textEl = document.getElementById('qtext_' + qid);
      var inp = document.getElementById('edit_qtext_' + qid);
      if (textEl && inp){{
        var num = textEl.textContent.match(/^\d+/);
        textEl.textContent = (num ? num[0] + '. ' : '') + inp.value;
      }}

      var ptsEl = document.getElementById('qpts_' + qid);
      var ptsInp = document.getElementById('edit_qpts_' + qid);
      if (ptsEl && ptsInp){{
        ptsEl.textContent = ptsInp.value;
      }}
    }});
  }}

  function captureData(){{
    var data = {{ questions: [] }};
    document.querySelectorAll('.q-item').forEach(function(item){{
      var q = {{ id: parseInt(item.dataset.qid), questionText: '', points: 0, displayOrder: 0, correctAnswer: null, options: [] }};
      data.questions.push(q);
    }});
    return data;
  }}

  function restoreData(data){{
    // Just reload the page for simplicity
    location.reload();
  }}

  function getToken() {{
    try {{
      return localStorage.getItem('access_token') || '';
    }} catch(e) {{
      return '';
    }}
  }}

  window.saveExam = function(){{
    var btn = document.getElementById('saveBtn');
    btn.disabled = true;
    btn.innerHTML = '<span class=""spinner""></span> جاري الحفظ...';

    var dto = {{
      uid: '{uid}',
      title: document.querySelector('.exam-header h1')?.textContent || '',
      durationMinutes: parseInt(document.getElementById('headerDuration')?.textContent) || 30,
      totalScore: parseFloat(document.getElementById('headerTotal')?.textContent) || 0,
      questions: []
    }};

    document.querySelectorAll('.q-item').forEach(function(item){{
      var qid = parseInt(item.dataset.qid);
      var qt = parseInt(item.dataset.qt);
      var textEl = document.getElementById('edit_qtext_' + qid);
      var ptsEl = document.getElementById('edit_qpts_' + qid);
      var txt = textEl ? textEl.value : '';
      var pts = ptsEl ? parseFloat(ptsEl.value || '0') : 0;

      var correctAnswer = null;
      // For fill-blank and essay, get the correct answer
      if (qt === 3 || qt === 4) {{
        var caInp = document.getElementById('edit_correct_' + qid);
        correctAnswer = caInp ? caInp.value || null : null;
      }}

      var q = {{ id: qid, questionText: txt, points: pts, displayOrder: 0, correctAnswer: correctAnswer, options: [] }};

      var optsEl = document.getElementById('qopts_' + qid);
      if (optsEl){{
        optsEl.querySelectorAll('li').forEach(function(li){{
          var oid = parseInt(li.dataset.optid);
          var optTxt = li.querySelector('.edit-opt-row input[type=""text""]')?.value || '';
          var isCorrect = li.querySelector('.edit-opt-row input[type=""checkbox""]')?.checked || false;
          q.options.push({{ id: oid, optionText: optTxt, isCorrect: isCorrect, displayOrder: 0 }});
        }});
      }}

      dto.questions.push(q);
    }});

    var token = getToken();
    var headers = {{ 'Content-Type': 'application/json' }};
    if (token) headers['Authorization'] = 'Bearer ' + token;

    fetch('/api/exam/' + '{uid}' + '/save', {{
      method: 'PUT',
      headers: headers,
      body: JSON.stringify(dto)
    }})
    .then(function(r){{ return r.json() }})
    .then(function(result){{
      btn.disabled = false;
      btn.innerHTML = '<span>💾</span><span>حفظ التعديلات</span>';
      if (result.isSuccess){{
        showToast('✅ تم حفظ التعديلات بنجاح', 'success');
        setTimeout(function(){{ location.reload(); }}, 1200);
      }} else {{
        showToast('❌ ' + (result.message || 'فشل الحفظ'), 'error');
      }}
    }})
    .catch(function(err){{
      btn.disabled = false;
      btn.innerHTML = '<span>💾</span><span>حفظ التعديلات</span>';
      showToast('❌ خطأ في الاتصال: ' + err.message, 'error');
    }});
  }};

  function showToast(msg, type){{
    var el = document.getElementById('toast');
    el.textContent = msg;
    el.className = 'toast ' + type + ' show';
    setTimeout(function(){{ el.className = 'toast'; }}, 3000);
  }}

  function escHtml(s){{
    if (!s) return '';
    return s.replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/""/g,'&quot;');
  }}
}})();
</script>
";
        }

        private static string Enc(string? s) => string.IsNullOrEmpty(s) ? "" : WebUtility.HtmlEncode(s);

        private static List<(ExamQuestionGroup? Group, ExamQuestion? Question)> BuildBlocks(
            List<ExamQuestionGroup> groups, List<ExamQuestion> standalone)
        {
            var list = new List<(ExamQuestionGroup? Group, ExamQuestion? Question)>();
            foreach (var g in groups)
                list.Add((g, null));
            foreach (var q in standalone)
                list.Add((null, q));
            return list.OrderBy(b => b.Group?.DisplayOrder ?? b.Question?.DisplayOrder ?? 0).ToList();
        }
    }
}
