using Project.Domain.Enums;

namespace Project.BLL.AI.Models;

public class FileData
{
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
}

public class ImportedStudentDto
{
    public string FullName { get; set; } = string.Empty;
    public string? NationalId { get; set; }
    public Gender? Gender { get; set; }
    public DateOnly? BirthDate { get; set; }
}

public class ImportPreviewResult
{
    public List<ImportedStudentDto> Students { get; set; } = new();
    public List<string> Errors { get; set; } = new();
}

public class ImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public int ErrorCount { get; set; }
    public List<string> Errors { get; set; } = new();
}
