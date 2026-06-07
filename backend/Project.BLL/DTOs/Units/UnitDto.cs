namespace Project.BLL.DTOs;

public class UnitDto
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public string? SubjectName { get; set; }
    public List<LessonDto>? Lessons { get; set; }
}

public class LessonDto
{
    public int Id { get; set; }
    public int UnitId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class CreateLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}

public class CreateUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public List<CreateLessonDto>? Lessons { get; set; }
}

public class SubjectWithStructureDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? GradeLevelName { get; set; }
    public int UnitCount { get; set; }
    public int LessonCount { get; set; }
}

public class BookPreviewResult
{
    public string PreviewId { get; set; } = string.Empty;
    public List<ParsedUnitDto> Units { get; set; } = new();
}

public class ParsedUnitDto
{
    public string Name { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
    public List<ParsedLessonDto>? Lessons { get; set; }
}

public class ParsedLessonDto
{
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public int DisplayOrder { get; set; }
    public int? PageStart { get; set; }
    public int? PageEnd { get; set; }
}
