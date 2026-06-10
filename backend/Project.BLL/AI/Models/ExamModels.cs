namespace Project.BLL.AI.Models;

public class Lesson
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Content { get; set; } = "";
}

public class ExamRequest
{
    public string LessonContent { get; set; } = "";
    public int QuestionCount { get; set; } = 5;
    public string Difficulty { get; set; } = "medium";
    public string Style { get; set; } = "multiple_choice";
}

public class ExamResponse
{
    public string Content { get; set; } = "";
}
