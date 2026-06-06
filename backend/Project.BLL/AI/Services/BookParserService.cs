using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Common.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.AI.Interfaces;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.BLL.AI.Services;

public class BookParserService : IBookParserService
{
    private readonly ILlmClient _llmClient;
    private readonly IUnitService _unitService;
    private readonly ILogger<BookParserService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _mistralApiKey;
    private readonly string _openRouterApiKey;
    private readonly string _openRouterModel;

    private const string MistralOcrEndpoint = "https://api.mistral.ai/v1/ocr";
    private const string MistralOcrModel = "mistral-ocr-latest";
    private const string OpenRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    public BookParserService(
        ILlmClient llmClient,
        IUnitService unitService,
        ILogger<BookParserService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _llmClient = llmClient;
        _unitService = unitService;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("MistralOcr");
        _mistralApiKey = configuration["AI:Mistral:ApiKey"] ?? "862c34pvzZx0QPCkggmKbzemlqtjocun";
        _openRouterApiKey = configuration["LlmSettings:OpenRouter:ApiKey"] ?? "";
        _openRouterModel = configuration["LlmSettings:OpenRouter:LessonCorrectionModel"] ?? "openrouter/owl-alpha";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Public API
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<OperationResult<List<ParsedUnitDto>>> PreviewBookAsync(
        Stream pdfStream, string fileName)
    {
        try
        {
            using var memStream = new MemoryStream();
            await pdfStream.CopyToAsync(memStream);
            var pdfBytes = memStream.ToArray();

            _logger.LogInformation("Starting Mistral OCR for {FileName} ({Bytes} bytes)", fileName, pdfBytes.Length);

            var ocrResult = await ExtractTextWithMistralOcrAsync(pdfBytes, fileName);
            if (ocrResult is null)
            {
                _logger.LogWarning("Mistral OCR returned null for {FileName}", fileName);
                return OperationResult<List<ParsedUnitDto>>.Failure(
                    "فشل استخراج النص من ملف PDF عبر Mistral OCR.", 500);
            }

            // نستخدم أول 5 صفحات من OCR كفهرس
            var tocText = BuildTocTextFromOcr(ocrResult, maxPages: 7);
            _logger.LogInformation("Extracted {Len} chars from OCR pages for {FileName}", tocText?.Length ?? 0, fileName);

            if (string.IsNullOrWhiteSpace(tocText))
                return OperationResult<List<ParsedUnitDto>>.Failure(
                    "لم يتم استخراج نص من ملف PDF. تأكد من أن الملف يحتوي على نص قابل للقراءة.", 400);

            var parsed = await AnalyzeWithLlmAsync(tocText, fileName);
            if (parsed is null || parsed.Count == 0)
            {
                _logger.LogWarning("LLM analysis failed for {FileName}, trying fallback parser", fileName);
                parsed = ParseStructureFromText(tocText);
            }

            if (parsed is null || parsed.Count == 0)
                return OperationResult<List<ParsedUnitDto>>.Failure(
                    "لم يتمكن النظام من تحليل هيكل الكتاب. تأكد من أن ملف PDF يحتوي على فهرس (Table of Contents) في أول 7 صفحات.", 400);

            _logger.LogInformation("Parsed {Count} units for {FileName}", parsed.Count, fileName);

            // حساب pageEnd تلقائياً بناءً على بداية الوحدة/الدرس التالي
            var totalPagesCount = ocrResult.Pages?.Count ?? 0;
            FillPageEnds(parsed, totalPagesCount);

            // استخراج المحتوى الخام للوحدات والدروس من OCR
            ExtractContentFromOcr(ocrResult, parsed);

            return OperationResult<List<ParsedUnitDto>>.Success(parsed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "BookParser preview failed for {FileName}", fileName);
            return OperationResult<List<ParsedUnitDto>>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    public async Task<OperationResult<List<UnitDto>>> SaveBookStructureAsync(
        int subjectId, List<CreateUnitDto> units)
    {
        try
        {
            var createdUnits = new List<UnitDto>();
            foreach (var unit in units)
            {
                var result = await _unitService.CreateUnitAsync(subjectId, unit);
                if (result.IsSuccess && result.Data is not null)
                    createdUnits.Add(result.Data);
                else
                    _logger.LogWarning("Failed to create unit {Unit}: {Msg}", unit.Name, result.Message);
            }

            if (createdUnits.Count == 0)
                return OperationResult<List<UnitDto>>.Failure("فشل إنشاء الوحدات في قاعدة البيانات.", 500);

            return OperationResult<List<UnitDto>>.Success(createdUnits);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SaveBookStructure failed for subject {SubjectId}", subjectId);
            return OperationResult<List<UnitDto>>.Failure($"حدث خطأ: {ex.Message}", 500);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Mistral OCR
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<MistralOcrResponse?> ExtractTextWithMistralOcrAsync(byte[] pdfBytes, string fileName)
    {
        try
        {
            var base64 = Convert.ToBase64String(pdfBytes);

            var requestBody = new
            {
                model = MistralOcrModel,
                document = new
                {
                    type = "document_url",
                    document_url = $"data:application/pdf;base64,{base64}"
                },
                include_image_base64 = true
            };

            var json = JsonSerializer.Serialize(requestBody);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var request = new HttpRequestMessage(HttpMethod.Post, MistralOcrEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _mistralApiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Mistral OCR failed ({Status}) for {FileName}: {Error}",
                    (int)response.StatusCode, fileName, error);
                return null;
            }

            var responseJson = await response.Content.ReadAsStringAsync();
            _logger.LogDebug("Mistral OCR response for {FileName}: {Length} chars", fileName, responseJson.Length);

            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<MistralOcrResponse>(responseJson, opts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Mistral OCR for {FileName}", fileName);
            return null;
        }
    }

    private static string BuildTocTextFromOcr(MistralOcrResponse ocrResult, int maxPages)
    {
        if (ocrResult.Pages is null || ocrResult.Pages.Count == 0)
            return ocrResult.Text ?? string.Empty;

        var pages = ocrResult.Pages.Take(maxPages);
        return string.Join("\n---\n",
            pages.Select(p => $"=== الصفحة {p.Index + 1} ===\n{p.Markdown ?? p.Text ?? ""}"));
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Page End Calculation
    // ─────────────────────────────────────────────────────────────────────────

    private static void FillPageEnds(List<ParsedUnitDto> units, int totalPages)
    {
        for (int i = 0; i < units.Count; i++)
        {
            var unit = units[i];

            // pageEnd للوحدة = بداية الوحدة التالية - 1، أو إجمالي الصفحات للوحدة الأخيرة
            if (unit.PageEnd is null && unit.PageStart is not null)
            {
                var nextUnitStart = (i + 1 < units.Count) ? units[i + 1].PageStart : null;
                unit.PageEnd = nextUnitStart.HasValue
                    ? nextUnitStart.Value - 1
                    : (totalPages > 0 ? totalPages : null);
            }

            // حساب pageEnd للدروس داخل الوحدة
            if (unit.Lessons is { Count: > 0 })
            {
                var unitEnd = unit.PageEnd;
                for (int j = 0; j < unit.Lessons.Count; j++)
                {
                    var lesson = unit.Lessons[j];
                    if (lesson.PageEnd is null && lesson.PageStart is not null)
                    {
                        // نهاية الدرس = بداية الدرس التالي - 1
                        // الدرس الأخير ينتهي بنهاية الوحدة
                        var nextLessonStart = (j + 1 < unit.Lessons.Count)
                            ? unit.Lessons[j + 1].PageStart
                            : null;

                        lesson.PageEnd = nextLessonStart.HasValue
                            ? nextLessonStart.Value - 1
                            : unitEnd;
                    }
                }

                // إذا الوحدة ليس لها pageStart بعد، خذها من أول درس
                if (unit.PageStart is null && unit.Lessons[0].PageStart is not null)
                    unit.PageStart = unit.Lessons[0].PageStart;
                if (unit.PageEnd is null && unit.Lessons[^1].PageEnd is not null)
                    unit.PageEnd = unit.Lessons[^1].PageEnd;
            }
        }
    }

    private static void ExtractContentFromOcr(MistralOcrResponse ocrResult, List<ParsedUnitDto> units)
    {
        var pageTextsMap = BuildPageTextMap(ocrResult);
        var totalPages = pageTextsMap.Count;

        foreach (var unit in units)
        {
            if (unit.PageStart is not null)
            {
                var start = Math.Max(1, unit.PageStart.Value);
                var end = unit.PageEnd is not null ? Math.Min(totalPages, unit.PageEnd.Value) : totalPages;

                var pageTexts = new List<string>();
                for (int i = start; i <= end; i++)
                {
                    if (pageTextsMap.TryGetValue(i, out var text) && !string.IsNullOrWhiteSpace(text))
                        pageTexts.Add(text.Trim());
                }
                unit.Content = string.Join("\n\n", pageTexts);
            }

            if (unit.Lessons is not null)
            {
                foreach (var lesson in unit.Lessons)
                {
                    if (lesson.PageStart is null) continue;

                    var lStart = Math.Max(1, lesson.PageStart.Value);
                    var lEnd = lesson.PageEnd is not null ? Math.Min(totalPages, lesson.PageEnd.Value) : totalPages;

                    var lPageTexts = new List<string>();
                    for (int i = lStart; i <= lEnd; i++)
                    {
                        if (pageTextsMap.TryGetValue(i, out var text) && !string.IsNullOrWhiteSpace(text))
                            lPageTexts.Add(text.Trim());
                    }
                    lesson.Content = string.Join("\n\n", lPageTexts);
                }
            }
        }
    }

    private static Dictionary<int, string> BuildPageTextMap(MistralOcrResponse ocrResult)
    {
        var map = new Dictionary<int, string>();

        if (ocrResult.Pages is not null)
        {
            foreach (var page in ocrResult.Pages)
            {
                var pageNum = page.Index + 1;  // Mistral pages are 0-indexed
                map[pageNum] = page.Markdown ?? page.Text ?? "";
            }
        }

        return map;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  LLM Helpers
    // ─────────────────────────────────────────────────────────────────────────

    public async Task<OperationResult<string>> CleanLessonContentWithAiAsync(string rawContent, string title)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(rawContent))
                return OperationResult<string>.Failure("المحتوى فارغ ولا يمكن معالجته.");

            var cleanText = await CleanLessonContentWithLlmAsync(rawContent, title);
            return OperationResult<string>.Success(cleanText);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clean lesson content with AI. Title: {Title}", title);
            return OperationResult<string>.Failure("حدث خطأ أثناء معالجة المحتوى بالذكاء الاصطناعي.");
        }
    }

    private async Task<string> CleanLessonContentWithLlmAsync(string rawText, string lessonTitle)
    {
        var prompt = $$"""
                      أنت خبير تربوي ومصحح لغوي دقيق جداً.

                      سأعطيك نصاً خاماً مستخرجاً من كتاب مدرسي بصيغة PDF لدرس بعنوان "{{lessonTitle}}".
                      هذا النص يحتوي على تشوهات ناتجة عن تقنية الـ OCR (مثل أخطاء إملائية، حروف مقطعة، كلمات ملتصقة، أسطر مكسورة، وأرقام صفحات متناثرة).

                      مهمتك الأساسية هي "إعادة بناء النص وتصحيحه إملائياً وتنظيمه" مع الالتزام الصارم بالآتي:
                      1. تصحيح جميع الأخطاء الإملائية واللغوية الناتجة عن الـ OCR بعناية فائقة.
                      2. الاحتفاظ بجميع فقرات ومعلومات الدرس بالكامل. **لا تقم بتلخيص الدرس أبداً ولا تحذف أي تفصيلة أو سؤال أو معلومة.**
                      3. إزالة الضوضاء (مثل أرقام الصفحات المتناثرة وسط النص، أسماء الفصول التي تتكرر كرؤوس وتذييلات).
                      4. تجميع الأسطر المكسورة لتكوين فقرات مستمرة ومقروءة بشكل سليم.
                      5. استخدام تنسيق Markdown لترتيب الدرس (استخدم # أو ## للعناوين، واستخدم * للقوائم النقطية).
                      6. تنظيم الأسئلة والتدريبات كقوائم رقمية واضحة إن وجدت.

                      أعد النص الكامل المصحح والمنسق فقط باستخدام لغة Markdown (بدون أي رسائل أو مقدمات أو خاتمة).

                      النص الخام:
                      {{rawText}}
                      """;

        // Use OpenRouter if configured, otherwise fall back to main LLM client
        if (!string.IsNullOrWhiteSpace(_openRouterApiKey))
        {
            return await CallOpenRouterAsync(prompt);
        }

        // Fallback to main LLM client
        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.User, prompt)
        };

        var response = await _llmClient.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());
        var content = response.Content;

        if (string.IsNullOrWhiteSpace(content))
            return rawText;

        content = content.Trim();
        if (content.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                content = content[start..end].Trim();
        }
        else if (content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                content = content[start..end].Trim();
        }

        if (string.IsNullOrWhiteSpace(content))
            return rawText;

        return content;
    }

    private async Task<string> CallOpenRouterAsync(string prompt)
    {
        var body = new
        {
            model = _openRouterModel,
            messages = new[] { new { role = "user", content = prompt } },
            temperature = 0.3,
            max_tokens = 8192
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, OpenRouterEndpoint);
        request.Headers.Add("Authorization", $"Bearer {_openRouterApiKey}");
        request.Headers.Add("HTTP-Referer", "https://schoollink.ai");
        request.Headers.Add("X-Title", "SchoolLink AI");
        request.Content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");

        _logger.LogInformation("Calling OpenRouter model {Model} for lesson correction", _openRouterModel);

        using var response = await _httpClient.SendAsync(request);
        var responseBody = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenRouter returned {Status}: {Body}", response.StatusCode, responseBody);
            return string.Empty;
        }

        using var doc = JsonDocument.Parse(responseBody);
        var content = doc.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()?.Trim() ?? string.Empty;

        if (content.StartsWith("```markdown", StringComparison.OrdinalIgnoreCase))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                content = content[start..end].Trim();
        }
        else if (content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                content = content[start..end].Trim();
        }

        return string.IsNullOrWhiteSpace(content) ? string.Empty : content;
    }

    private async Task<string> CleanContentWithLlmAsync(string rawText, string unitName)
    {
        var prompt = $$"""

                      أنت مساعد تنظيف نصوص كتب مدرسية.

                      سأعطيك نصاً خاماً مستخرجاً من PDF لوحدة دراسية بعنوان "{{unitName}}".
                      هذا النص قد يكون به مشاكل في التنسيق بسبب استخراجه من PDF (أسطر مقطوعة، كلمات ملتصقة،
                      تنسيق سيء، أرقام صفحات متداخلة، رؤوس وتذييلات).

                      مهمتك:
                      1. نظف النص بالكامل
                      2. رتب المحتوى درساً درساً إن أمكن
                      3. صحح تنسيق الجمل والفقرات
                      4. أزل أرقام الصفحات والرؤوس والتذييلات
                      5. حافظ على المحتوى الأكاديمي كما هو بدون تغيير أو إضافة
                      6. استخدم فواصل الأسطر بين الدروس: "---\n\n[الدرس الثاني]"

                      أعد النص المنظف فقط (بدون مقدمات أو تعليقات).

                      النص الخام:
                      {{rawText}}
                      """;

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.User, prompt)
        };

        var response = await _llmClient.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());
        var cleaned = response.Content?.Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? rawText : cleaned;
    }

    private async Task<List<ParsedUnitDto>?> AnalyzeWithLlmAsync(string text, string fileName)
    {
        var prompt = $$"""

                      أنت محلل كتب دراسية متخصص.

                      سأعطيك النص المستخرج من أول 7 صفحات من كتاب "{{fileName}}".
                      هذه الصفحات تحتوي غالباً على فهرس الكتاب (Table of Contents).

                      مهمتك:
                      1. حلل الفهرس وحدد الوحدات (Units) الموجودة في الكتاب
                      2. داخل كل وحدة، حدد الدروس (Lessons) الموجودة
                      3. لكل درس، حدد رقم الصفحة التي يبدأ فيها والصفحة التي ينتهي فيها
                      4. إذا كانت المادة لا تحتوي على دروس داخل الوحدات (مثل اللغة الإنجليزية)، فأرجع الوحدات فقط مع مصفوفة lessons فارغة

                      ملاحظات:
                      - الأرقام التي تظهر بجانب العناوين في الفهرس غالباً هي أرقام الصفحات
                      - قد تظهر أرقام الصفحات في الجانب الأيسر أو الأيمن
                      - استخدم أرقام الصفحات الحقيقية من الكتاب (وليس رقم الصفحة في ملف PDF)
                      - حقل content للوحدة سيتم ملؤه لاحقاً من النص الفعلي للصفحات - اتركه فارغاً ""

                      أرجع النتيجة بصيغة JSON فقط (بدون أي نص إضافي):
                      [
                        {
                          "name": "اسم الوحدة كاملاً",
                          "content": "",
                          "pageStart": 1,
                          "pageEnd": 30,
                          "displayOrder": 1,
                          "lessons": [
                            {
                              "title": "اسم الدرس كاملاً",
                              "pageStart": 12,
                              "pageEnd": 25,
                              "displayOrder": 1
                            }
                          ]
                        }
                      ]

                      نص الكتاب:
                      {{text}}
                      """;

        try { System.IO.File.WriteAllText(@"d:\ITI_2026\Final\SchoolLink-AI - 2\0\backend\Project.API\logs\ocr_dump.txt", text); } catch { }

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.User, prompt)
        };

        var response = await _llmClient.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());
        var content = response.Content;
        
        _logger.LogInformation("LLM raw response for {FileName} (first 500 chars): {Resp}",
            fileName, content?.Length > 500 ? content[..500] : content);

        if (string.IsNullOrWhiteSpace(content))
        {
            _logger.LogWarning("LLM returned empty response for {FileName}", fileName);
            return null;
        }

        content = content.Trim();
        if (content.StartsWith("```"))
        {
            var start = content.IndexOf('\n') + 1;
            var end = content.LastIndexOf("```");
            if (start > 0 && end > start)
                content = content[start..end].Trim();
        }

        try
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<List<ParsedUnitDto>>(content, options);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse LLM JSON for {FileName}. Response: {Resp}", fileName, content);
            return null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Fallback Text Parser
    // ─────────────────────────────────────────────────────────────────────────

    private static List<ParsedUnitDto>? ParseStructureFromText(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("==="))
                        .ToList();

        var units = new List<ParsedUnitDto>();
        ParsedUnitDto? currentUnit = null;

        foreach (var line in lines)
        {
            var normalizedLine = NormalizeArabicNumbers(line);
            
            // Remove Arabic diacritics (tashkeel) for reliable regex matching
            var cleanLine = System.Text.RegularExpressions.Regex.Replace(normalizedLine, @"\p{Mn}", "");

            // Unit detection - must be a heading line (starts with ## or contains الوحدة at beginning)
            // Exclude table rows (start with |), page references, and book metadata lines
            var isTableRow = cleanLine.StartsWith("|") || cleanLine.StartsWith("---");
            var isPageRef  = System.Text.RegularExpressions.Regex.IsMatch(cleanLine, @"^\d+\s*$");
            var isMetadata = cleanLine.Contains("للصف") || cleanLine.Contains("الفصل الدراسي") 
                             || cleanLine.Contains("كتاب") || cleanLine.Contains("مدرسة");

            var isUnitHeading = !isTableRow && !isPageRef && !isMetadata && (
                // ## الوحدة الأولى : ...
                System.Text.RegularExpressions.Regex.IsMatch(cleanLine, @"^#{1,3}\s*الوحدة", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                // الوحدة الأولى at start of line (TOC entry without ##)
                System.Text.RegularExpressions.Regex.IsMatch(cleanLine, @"^الوحدة\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase) ||
                // Unit 1 / Unit One
                System.Text.RegularExpressions.Regex.IsMatch(cleanLine, @"^Unit\s+", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            );

            if (isUnitHeading)
            {
                var name = System.Text.RegularExpressions.Regex.Replace(cleanLine, @"[#*|_]", "").Trim();
                if (name.Length > 3)
                {
                    // Check if we already have a unit that starts with the same 2 words (e.g., "الوحدة الأولى")
                    var nameParts = name.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                    ParsedUnitDto? existingUnit = null;

                    if (nameParts.Length >= 2)
                    {
                        existingUnit = units.FirstOrDefault(u => 
                        {
                            var uParts = u.Name.Split(new[] { ' ', ':' }, StringSplitOptions.RemoveEmptyEntries);
                            return uParts.Length >= 2 && uParts[0] == nameParts[0] && uParts[1] == nameParts[1];
                        });
                    }
                    else
                    {
                        existingUnit = units.FirstOrDefault(u => u.Name == name);
                    }

                    if (existingUnit != null)
                    {
                        currentUnit = existingUnit;
                        // Optionally update name if the new one is longer (might be more complete from TOC)
                        if (name.Length > existingUnit.Name.Length)
                            existingUnit.Name = name;
                    }
                    else
                    {
                        currentUnit = new ParsedUnitDto
                        {
                            Name = name,
                            Content = "",
                            DisplayOrder = units.Count + 1,
                            Lessons = new List<ParsedLessonDto>()
                        };
                        units.Add(currentUnit);
                    }
                    continue;
                }
            }

            if (currentUnit != null)
            {
                // Try to extract lesson from line
                // Format 1: | 12 | Lesson Title |
                var m = System.Text.RegularExpressions.Regex.Match(cleanLine, @"(?:^|\s|\|)\s*(\d+)\s*(?:\||-|\s)+(.*?)(?:\||$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var pageNum = int.Parse(m.Groups[1].Value);
                    var title = m.Groups[2].Value.Trim(' ', '|', '-', '_', '*');
                    if (title.Length > 3 && !title.Contains("---") && !title.Contains("ص ") && pageNum > 0 && pageNum < 500)
                    {
                        if (currentUnit.PageStart == null) currentUnit.PageStart = pageNum;
                        currentUnit.Lessons.Add(new ParsedLessonDto
                        {
                            Title = title,
                            PageStart = pageNum,
                            DisplayOrder = currentUnit.Lessons.Count + 1
                        });
                        continue;
                    }
                }
                
                // Format 2: Lesson Title ...... 12
                var m2 = System.Text.RegularExpressions.Regex.Match(cleanLine, @"(?:^|\|)\s*(.+?)\s*(?:\||-|\s|\.)+\s*(\d+)\s*(?:\||$)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (m2.Success)
                {
                    var title = m2.Groups[1].Value.Trim(' ', '|', '-', '_', '*');
                    var pageNum = int.Parse(m2.Groups[2].Value);
                    if (title.Length > 3 && !title.Contains("---") && !title.Contains("ص ") && pageNum > 0 && pageNum < 500)
                    {
                        if (currentUnit.PageStart == null) currentUnit.PageStart = pageNum;
                        currentUnit.Lessons.Add(new ParsedLessonDto
                        {
                            Title = title,
                            PageStart = pageNum,
                            DisplayOrder = currentUnit.Lessons.Count + 1
                        });
                        continue;
                    }
                }
            }
        }

        return units.Count > 0 ? units : null;
    }

    private static string NormalizeArabicNumbers(string input)
    {
        return input.Replace('٠', '0').Replace('١', '1').Replace('٢', '2')
                    .Replace('٣', '3').Replace('٤', '4').Replace('٥', '5')
                    .Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Mistral OCR Response Models
// ─────────────────────────────────────────────────────────────────────────────

public class MistralOcrResponse
{
    [JsonPropertyName("pages")]
    public List<MistralOcrPage>? Pages { get; set; }

    /// <summary>Flat text fallback if the API returns a top-level "text" field.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class MistralOcrPage
{
    [JsonPropertyName("index")]
    public int Index { get; set; }

    /// <summary>Markdown-formatted text (primary field returned by mistral-ocr-latest).</summary>
    [JsonPropertyName("markdown")]
    public string? Markdown { get; set; }

    /// <summary>Plain text fallback.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("images")]
    public List<MistralOcrImage>? Images { get; set; }
}

public class MistralOcrImage
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("image_base64")]
    public string? ImageBase64 { get; set; }
}
