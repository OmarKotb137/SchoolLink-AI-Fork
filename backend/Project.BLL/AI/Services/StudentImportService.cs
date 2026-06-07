using System.IO.Compression;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Xml.Linq;
using Common.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.DAL.Interfaces;
using Project.Domain.Entities;

namespace Project.BLL.AI.Services;

public class StudentImportService : IStudentImportService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StudentImportService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    private const string SystemPrompt =
        "أنت مساعد استخراج بيانات طلاب من ملفات. مهمتك هي استخراج أسماء الطلاب كما هي تماماً في الملف دون أي تغيير أو اختلاق.\n" +
        "أعد JSON Array بالشكل التالي:\n" +
        "[\n" +
        "  { \"fullName\": \"أحمد محمد السيد\", \"nationalId\": \"29801120123456\", \"gender\": \"male\", \"birthDate\": \"1998-01-12\" },\n" +
        "  { \"fullName\": \"سارة علي حسن\", \"nationalId\": null, \"gender\": \"female\", \"birthDate\": \"2000-05-20\" },\n" +
        "  { \"fullName\": \"محمود إبراهيم\", \"nationalId\": null, \"gender\": null, \"birthDate\": null }\n" +
        "]\n" +
        "تنبيهات مهمة:\n" +
        "- استخرج الأسماء بنفس الصيغة التي تظهر بها في الملف حرفياً\n" +
        "- لا تغير ولا تختلق ولا تضيف أي أسماء غير موجودة فعلاً\n" +
        "- إذا لم تجد أي أسماء، أعد [] فارغة\n" +
        "- fullName: إجباري\n" +
        "- nationalId: اختياري (14 رقم)\n" +
        "- gender: اختياري (male/female/null)\n" +
        "- birthDate: اختياري (YYYY-MM-DD/null)\n" +
        "- إذا ما لقيتش بيانات زيادة، حط null في الحقول الاختيارية\n" +
        "- أعد JSON Array فقط بدون أي نص إضافي";

    public StudentImportService(
        IHttpClientFactory httpFactory,
        IUnitOfWork unitOfWork,
        ILogger<StudentImportService> logger,
        IConfiguration config)
    {
        _httpFactory = httpFactory;
        _unitOfWork = unitOfWork;
        _logger = logger;
        var provider = config["LlmSettings:StudentImport:Provider"] ?? "Gemini";
        _model = config["LlmSettings:StudentImport:Model"] ?? "gemini-2.0-flash";
        _apiKey = config.GetSection($"LlmSettings:{provider}")["ApiKey"] ?? "";
    }

    public async Task<OperationResult<ImportPreviewResult>> PreviewImportAsync(List<FileData> files, CancellationToken ct = default)
    {
        if (files == null || files.Count == 0)
            return OperationResult<ImportPreviewResult>.Failure("يجب رفع ملف واحد على الأقل", 400);

        var parts = new List<object>();
        foreach (var f in files)
        {
            var ext = Path.GetExtension(f.FileName)?.ToLowerInvariant();
            if (IsGeminiInlineSupported(ext))
            {
                var contentType = NormalizeMimeType(f.ContentType, f.FileName);
                var b64 = Convert.ToBase64String(f.Data);
                parts.Add(new { inlineData = new { mimeType = contentType, data = b64 } });
            }
            else
            {
                var text = ExtractText(f.Data, ext);
                if (!string.IsNullOrWhiteSpace(text))
                    parts.Add(new { text });
            }
        }
        parts.Add(new { text = "استخرج أسماء الطلاب من هذه الملفات كما هي تماماً دون تغيير، وأعد JSON Array فقط. إذا لم تجد أسماء واضحة أعد []." });

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";
        var body = new
        {
            system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
            contents = new[]
            {
                new { role = "user", parts }
            },
            generationConfig = new { temperature = 0.2 }
        };

        try
        {
            var http = _httpFactory.CreateClient();
            using var res = await http.PostAsJsonAsync(url, body, ct);
            var responseBody = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Gemini returned {Status}: {Body}", res.StatusCode, responseBody);
                return OperationResult<ImportPreviewResult>.Failure(
                    $"فشل Gemini: {responseBody}", 502);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var text = doc.RootElement
                .GetProperty("candidates")[0]
                .GetProperty("content")
                .GetProperty("parts")[0]
                .GetProperty("text")
                .GetString();

            if (string.IsNullOrWhiteSpace(text))
                return OperationResult<ImportPreviewResult>.Failure("لم يتمكن Gemini من تحليل الملفات", 400);

            var students = ParseStudentsFromJson(text);
            var preview = new ImportPreviewResult();
            var errors = new List<string>();

            foreach (var s in students)
            {
                if (string.IsNullOrWhiteSpace(s.FullName))
                {
                    errors.Add("تم تخطي طالب بدون اسم");
                    continue;
                }
                preview.Students.Add(s);
            }

            preview.Errors = errors;
            return OperationResult<ImportPreviewResult>.Success(preview,
                $"تم استخراج {preview.Students.Count} طالب" + (errors.Count > 0 ? $" مع {errors.Count} تحذير" : ""));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Preview import failed");
            return OperationResult<ImportPreviewResult>.Failure("حدث خطأ أثناء تحليل الملفات: " + ex.Message, 500);
        }
    }

    private static bool IsGeminiInlineSupported(string? ext) => ext switch
    {
        ".jpg" or ".jpeg" or ".png" or ".webp" or ".gif" or ".bmp" => true,
        ".pdf" => true,
        _ => false
    };

    private static string ExtractText(byte[] data, string? ext)
    {
        try
        {
            if (ext is ".docx" or ".xlsx")
            {
                using var ms = new MemoryStream(data);
                using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

                if (ext == ".docx")
                {
                    var entry = archive.GetEntry("word/document.xml");
                    if (entry == null) return string.Empty;
                    using var reader = new StreamReader(entry.Open());
                    var xml = reader.ReadToEnd();
                    return XElement.Parse(xml).Descendants().Where(e => e.Name.LocalName == "t").Aggregate("", (s, e) => s + e.Value + " ");
                }

                if (ext == ".xlsx")
                {
                    var sb = new StringBuilder();
                    var sharedStrings = new List<string>();

                    var ssEntry = archive.GetEntry("xl/sharedStrings.xml");
                    if (ssEntry != null)
                    {
                        using var sr = new StreamReader(ssEntry.Open());
                        sharedStrings = XElement.Parse(sr.ReadToEnd())
                            .Descendants().Where(e => e.Name.LocalName == "t")
                            .Select(e => e.Value).ToList();
                    }

                    var sheetIdx = 0;
                    while (true)
                    {
                        var sheetEntry = archive.GetEntry($"xl/worksheets/sheet{++sheetIdx}.xml");
                        if (sheetEntry == null) break;

                        using var sr = new StreamReader(sheetEntry.Open());
                        var doc = XElement.Parse(sr.ReadToEnd());
                        foreach (var c in doc.Descendants().Where(e => e.Name.LocalName == "c"))
                        {
                            var v = c.Descendants().FirstOrDefault(e => e.Name.LocalName == "v");
                            if (v == null) continue;
                            var t = c.Attribute("t")?.Value;
                            if (t == "s" && int.TryParse(v.Value, out var si) && si < sharedStrings.Count)
                                sb.Append(sharedStrings[si] + " ");
                            else
                                sb.Append(v.Value + " ");
                        }
                        sb.AppendLine();
                    }

                    return sb.ToString();
                }
            }

            if (ext is ".csv" or ".txt")
                return Encoding.UTF8.GetString(data);

            return string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string NormalizeMimeType(string contentType, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(contentType) && contentType != "application/octet-stream")
            return contentType;

        var ext = Path.GetExtension(fileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            _ => "image/jpeg"
        };
    }

    public async Task<OperationResult<ImportResult>> ImportWithAiAsync(List<ImportedStudentDto> students, int classId, int? academicYearId, CancellationToken ct = default)
    {
        if (students == null || students.Count == 0)
            return OperationResult<ImportResult>.Failure("يجب توفير طالب واحد على الأقل", 400);

        var classEntity = await _unitOfWork.Classes.GetByIdAsync(classId, ct);
        if (classEntity == null || classEntity.IsDeleted)
            return OperationResult<ImportResult>.Failure("الفصل غير موجود", 404);

        var result = new ImportResult();

        foreach (var s in students)
        {
            if (string.IsNullOrWhiteSpace(s.FullName))
            {
                result.ErrorCount++;
                result.Errors.Add("تم تخطي طالب بدون اسم");
                continue;
            }

            if (!string.IsNullOrWhiteSpace(s.NationalId))
            {
                var existing = await _unitOfWork.Students.GetByNationalIdAsync(s.NationalId);
                if (existing != null && !existing.IsDeleted)
                {
                    result.SkippedCount++;
                    result.Errors.Add($"الطالب '{s.FullName}' - الرقم القومي '{s.NationalId}' موجود بالفعل للطالب {existing.FullName}");
                    continue;
                }
            }

            var student = new Student
            {
                FullName = s.FullName.Trim(),
                NationalId = s.NationalId?.Trim(),
                Gender = s.Gender,
                BirthDate = s.BirthDate,
                IsActive = true
            };

            await _unitOfWork.Students.AddAsync(student, ct);
            result.ImportedCount++;
        }

        await _unitOfWork.SaveChangesAsync(ct);

        return OperationResult<ImportResult>.Success(result,
            $"تم استيراد {result.ImportedCount} طالب" + (result.SkippedCount > 0 ? $" (تخطي {result.SkippedCount} مكرر)" : ""));
    }

    private List<ImportedStudentDto> ParseStudentsFromJson(string json)
    {
        try
        {
            json = json.Trim();
            if (json.StartsWith("```json"))
                json = json[7..];
            else if (json.StartsWith("```"))
                json = json[3..];
            if (json.EndsWith("```"))
                json = json[..^3];

            var start = json.IndexOf('[');
            var end = json.LastIndexOf(']');
            if (start >= 0 && end > start)
                json = json[start..(end + 1)];

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            };

            return JsonSerializer.Deserialize<List<ImportedStudentDto>>(json, options) ?? new List<ImportedStudentDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse Gemini JSON response");
            return new List<ImportedStudentDto>();
        }
    }
}
