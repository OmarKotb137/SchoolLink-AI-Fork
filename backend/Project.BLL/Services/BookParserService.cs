using System.Text.Json;
using Common.Results;
using Microsoft.Extensions.Logging;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.DTOs;
using Project.BLL.Interfaces;

namespace Project.BLL.Services;

public class BookParserService : IBookParserService
{
    private readonly ILlmClient _llmClient;
    private readonly IUnitService _unitService;
    private readonly ILogger<BookParserService> _logger;

    public BookParserService(
        ILlmClient llmClient,
        IUnitService unitService,
        ILogger<BookParserService> logger)
    {
        _llmClient = llmClient;
        _unitService = unitService;
        _logger = logger;
    }

    public async Task<OperationResult<List<ParsedUnitDto>>> PreviewBookAsync(
        Stream pdfStream, string fileName)
    {
        try
        {
            using var memStream = new MemoryStream();
            await pdfStream.CopyToAsync(memStream);
            memStream.Position = 0;

            var tocText = ExtractTextFromFirstPages(memStream, 5);
            _logger.LogInformation("Extracted {Len} chars from first 5 pages of {FileName}", tocText?.Length ?? 0, fileName);
            _logger.LogDebug("Extracted text from {FileName}: {Text}", fileName, tocText?.Length > 2000 ? tocText[..2000] + "..." : tocText);

            if (string.IsNullOrWhiteSpace(tocText))
                return OperationResult<List<ParsedUnitDto>>.Failure(
                    "لم يتم استخراج نص من ملف PDF. تأكد من أن الملف يحتوي على نص قابل للقراءة.", 400);

            if (!tocText.Any(char.IsDigit))
                _logger.LogWarning("No digits found in extracted text from {FileName} — PDF may be scanned images", fileName);

            var parsed = await AnalyzeWithLlmAsync(tocText, fileName);
            if (parsed is null || parsed.Count == 0)
            {
                _logger.LogWarning("LLM analysis failed for {FileName}, trying fallback parser", fileName);
                parsed = ParseStructureFromText(tocText);
            }

            if (parsed is null || parsed.Count == 0)
                return OperationResult<List<ParsedUnitDto>>.Failure(
                    "لم يتمكن النظام من تحليل هيكل الكتاب. تأكد من أن ملف PDF يحتوي على فهرس (Table of Contents) في أول 5 صفحات.", 400);

            _logger.LogInformation("Parsed {Count} units for {FileName}", parsed.Count, fileName);

            var missingContent = parsed.Where(u =>
                u is { Lessons.Count: 0, PageStart: not null } && string.IsNullOrWhiteSpace(u.Content)).ToList();

            if (missingContent.Count > 0)
            {
                memStream.Position = 0;
                await ExtractUnitContentAsync(memStream, missingContent);
            }

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

    private static string ExtractTextFromFirstPages(Stream stream, int pageCount)
    {
        using var pdf = PdfDocument.Open(stream);
        var pages = pdf.GetPages().Take(pageCount);
        return string.Join("\n---\n", pages.Select(p => $"=== الصفحة {p.Number} ===\n{ExtractPageText(p)}"));
    }

    private static string ExtractPageText(Page page)
    {
        var letters = page.Letters;
        if (letters is null || letters.Count == 0)
            return page.Text ?? "";

        const double yThreshold = 8;
        const double wordGapMultiplier = 1.5;

        var ordered = letters.OrderBy(l => l.StartBaseLine.Y).ThenBy(l => l.StartBaseLine.X).ToList();

        var lineGroups = new List<List<Letter>>();
        var cur = new List<Letter> { ordered[0] };
        for (int i = 1; i < ordered.Count; i++)
        {
            if (Math.Abs(ordered[i].StartBaseLine.Y - ordered[i - 1].StartBaseLine.Y) > yThreshold)
            {
                lineGroups.Add(cur);
                cur = new List<Letter>();
            }
            cur.Add(ordered[i]);
        }
        if (cur.Count > 0) lineGroups.Add(cur);

        return string.Join("\n", lineGroups.Select(line =>
        {
            var sorted = line.OrderBy(l => l.StartBaseLine.X).ToList();
            var avgWidth = sorted.Average(l => l.Width);
            var gapThreshold = avgWidth * wordGapMultiplier;

            var words = new List<string>();
            var chars = new List<string> { sorted[0].Value };
            for (int i = 1; i < sorted.Count; i++)
            {
                var prev = sorted[i - 1];
                var curr = sorted[i];
                var gap = curr.StartBaseLine.X - (prev.StartBaseLine.X + prev.Width);
                if (gap > gapThreshold)
                {
                    words.Add(string.Concat(chars));
                    chars.Clear();
                }
                chars.Add(curr.Value);
            }
            if (chars.Count > 0) words.Add(string.Concat(chars));

            if (words.Any(w => w.Any(c => c >= 0x600 && c <= 0x6FF)))
                words.Reverse();

            return string.Join(" ", words);
        }));
    }

    private async Task ExtractUnitContentAsync(Stream stream, List<ParsedUnitDto> units)
    {
        using var pdf = PdfDocument.Open(stream);
        var totalPages = pdf.NumberOfPages;

        foreach (var unit in units)
        {
            if (unit.PageStart is null) continue;

            var start = Math.Max(1, unit.PageStart.Value);
            var end = unit.PageEnd is not null ? Math.Min(totalPages, unit.PageEnd.Value) : totalPages;

            var pageTexts = new List<string>();
            for (int i = start; i <= end && i <= totalPages; i++)
            {
                try
                {
                    var page = pdf.GetPage(i);
                    var t = ExtractPageText(page)?.Trim();
                    if (!string.IsNullOrWhiteSpace(t))
                        pageTexts.Add(t);
                }
                catch
                {
                }
            }

            var rawText = string.Join("\n\n", pageTexts);
            if (!string.IsNullOrWhiteSpace(rawText))
                unit.Content = await CleanContentWithLlmAsync(rawText, unit.Name);
            else
                unit.Content = "";
        }
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

    private static List<ParsedUnitDto>? ParseStructureFromText(string text)
    {
        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Select(l => l.Trim())
                        .Where(l => !string.IsNullOrWhiteSpace(l) && !l.StartsWith("==="))
                        .ToList();

        if (lines.Count == 0) return null;

        var result = TryParseStructuredToc(lines);
        if (result is not null) return result;

        result = TryParseSimpleNumberedList(lines);
        if (result is not null) return result;

        return null;
    }

    private static List<ParsedUnitDto>? TryParseStructuredToc(List<string> lines)
    {
        var units = new List<ParsedUnitDto>();
        string? currentUnitName = null;
        int? currentUnitPage = null;
        var currentLessons = new List<ParsedLessonDto>();

        foreach (var line in lines)
        {
            var m = System.Text.RegularExpressions.Regex.Match(line,
                @"(?:الوحدة\s*(.+?)|Unit\s*\d+\s*[:：\-–]?\s*(.+?))\s*[.:：\-–]?\s*(\d+)\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (m.Success && int.TryParse(m.Groups[3].Value, out var pageNum))
            {
                if (currentUnitName is not null)
                    units.Add(MakeUnit(currentUnitName, currentUnitPage, currentLessons));

                currentUnitName = (m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value).Trim().TrimEnd(':', '،', ',', '-', '–');
                currentUnitPage = pageNum;
                currentLessons.Clear();
                continue;
            }

            var lessonMatch = System.Text.RegularExpressions.Regex.Match(line,
                @"(?:الدرس\s*(.+?)|Lesson\s*\d+\s*[:：\-–]?\s*(.+?))\s*[.:：\-–]?\s*(\d+)\s*$",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            if (lessonMatch.Success && int.TryParse(lessonMatch.Groups[3].Value, out var lessonPage))
            {
                var title = (lessonMatch.Groups[1].Success ? lessonMatch.Groups[1].Value : lessonMatch.Groups[2].Value).Trim().TrimEnd(':', '،', ',', '-', '–');
                currentLessons.Add(new ParsedLessonDto
                {
                    Title = title,
                    PageStart = lessonPage,
                    PageEnd = null,
                    DisplayOrder = currentLessons.Count + 1
                });
            }
        }

        if (currentUnitName is not null)
            units.Add(MakeUnit(currentUnitName, currentUnitPage, currentLessons));

        return units.Count > 0 ? units : null;
    }

    private static List<ParsedUnitDto>? TryParseSimpleNumberedList(List<string> lines)
    {
        var units = new List<ParsedUnitDto>();

        foreach (var line in lines)
        {
            var cleaned = System.Text.RegularExpressions.Regex.Replace(line, @"[^\w\s\u0600-\u06FF\-–.():،]", "");
            if (string.IsNullOrWhiteSpace(cleaned)) continue;

            var m = System.Text.RegularExpressions.Regex.Match(cleaned, @"(.+?)\s*[.:]?\s*(\d+)\s*$");
            if (!m.Success || !int.TryParse(m.Groups[2].Value, out var page) || page < 1 || page > 1000)
            {
                m = System.Text.RegularExpressions.Regex.Match(cleaned, @"^\s*(\d+)\s*[.)\-–]\s*(.+)$");
                if (m.Success && int.TryParse(m.Groups[1].Value, out var num) && num >= 1 && num <= 100)
                {
                    units.Add(new ParsedUnitDto
                    {
                        Name = m.Groups[2].Value.Trim().TrimEnd(':', '،', ',', '-', '–'),
                        Content = "",
                        PageStart = null,
                        PageEnd = null,
                        DisplayOrder = units.Count + 1,
                        Lessons = new List<ParsedLessonDto>()
                    });
                }
                continue;
            }

            var name = m.Groups[1].Value.Trim().TrimEnd(':', '،', ',', '-', '–');
            if (name.Length < 3) continue;

            units.Add(new ParsedUnitDto
            {
                Name = name,
                Content = "",
                PageStart = page,
                PageEnd = null,
                DisplayOrder = units.Count + 1,
                Lessons = new List<ParsedLessonDto>()
            });
        }

        return units.Count > 0 ? units : null;
    }

    private static ParsedUnitDto MakeUnit(string name, int? page, List<ParsedLessonDto> lessons)
    {
        return new ParsedUnitDto
        {
            Name = name,
            Content = "",
            PageStart = page,
            PageEnd = null,
            DisplayOrder = 0,
            Lessons = new List<ParsedLessonDto>(lessons)
        };
    }

    private async Task<List<ParsedUnitDto>?> AnalyzeWithLlmAsync(string text, string fileName)
    {
        var prompt = $$"""

                      أنت محلل كتب دراسية متخصص.

                      سأعطيك النص المستخرج من أول 5 صفحات من كتاب "{{fileName}}".
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

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.User, prompt)
        };

        var response = await _llmClient.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());
        var content = response.Content;
        _logger.LogInformation("LLM raw response for {FileName} (first 500 chars): {Resp}", fileName, content?.Length > 500 ? content[..500] : content);

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
}