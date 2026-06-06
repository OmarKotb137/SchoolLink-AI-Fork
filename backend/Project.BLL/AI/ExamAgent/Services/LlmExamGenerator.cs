using System.Text.Json;
using Microsoft.Extensions.Logging;
using Project.BLL.AI.ExamAgent.Interfaces;
using Project.BLL.AI.ExamAgent.Models;
using Project.BLL.AI.Interfaces;
using Project.BLL.AI.Models;
using Project.BLL.DTOs.Exam;
using Project.BLL.Interfaces;
using Project.Domain.Enums;

namespace Project.BLL.AI.ExamAgent.Services;

public class LlmExamGenerator : IExamGenerator
{
    private readonly ILlmClient _llm;
    private readonly IExamService _examService;
    private readonly ILogger<LlmExamGenerator> _logger;

    public LlmExamGenerator(ILlmClient llm, IExamService examService, ILogger<LlmExamGenerator> logger)
    {
        _llm = llm;
        _examService = examService;
        _logger = logger;
    }

    public async Task<ExamResponse> GenerateAsync(ExamRequest request)
    {
        var styleLabel = request.Style switch
        {
            "multiple_choice" => "اختيار من متعدد",
            "true_false" => "صح أم خطأ",
            "open_ended" => "مقالي",
            _ => request.Style
        };

        var diffLabel = request.Difficulty switch
        {
            "easy" => "سهل",
            "medium" => "متوسط",
            "hard" => "صعب",
            _ => request.Difficulty
        };

        var prompt = $$"""
            أنت أستاذ خبير في إعداد الامتحانات.
            اقرأ محتوى الدرس التالي واستخرج منه بالضبط {{request.QuestionCount}} سؤال.

            المستوى: {{diffLabel}}
            نوع الأسئلة: {{styleLabel}}

            محتوى الدرس:
            {{request.LessonContent}}

            يجب أن يكون الخرج بصيغة JSON تطابق البنية التالية (بدون علامات ترقيم خارجية، فقط JSON صالح):

            {
              "title": "عنوان الامتحان",
              "totalScore": 50,
              "durationMinutes": 60,
              "category": 0,
              "groups": [
                {
                  "displayType": 0,
                  "contentTitle": "عنوان المجموعة (اختياري)",
                  "contentText": "نص المقدمة أو المقطع (اختياري)",
                  "displayOrder": 1,
                  "questions": [
                    {
                      "questionText": "نص السؤال",
                      "questionType": 0,
                      "points": 5,
                      "displayOrder": 1,
                      "options": [
                        { "text": "خيار أ", "isCorrect": true, "displayOrder": 1 },
                        { "text": "خيار ب", "isCorrect": false, "displayOrder": 2 }
                      ]
                    }
                  ]
                }
              ],
              "standaloneQuestions": [
                {
                  "questionText": "نص السؤال المستقل",
                  "questionType": 0,
                  "points": 5,
                  "displayOrder": 1,
                  "correctAnswer": "الإجابة الصحيحة"
                }
              ]
            }

            displayType: 0=بدون, 1=نص, 2=صورة, 3=خريطة, 4=رسم بياني
            questionType: 0=اختيار من متعدد, 1=صح/خطأ, 2=مطابقة, 3=تعبئة فراغ, 4=مقالي
            category: 0=أول فصل, 1=ثاني فصل, 2=نهائي, 3=اختبار قصير

            للأسئلة المقالية (questionType=4) لا تضف options بل ضع correctAnswer.
            لأسئلة الاختيار من متعدد (questionType=0) ضع options بدون correctAnswer (الصحيح داخل options).
            لأسئلة صح/خطأ (questionType=1) ضع options: [{"text":"صح","isCorrect":true},{"text":"خطأ","isCorrect":false}].
            """;

        var messages = new List<LlmChatMessage>
        {
            new(MessageRole.System, "أنت مساعد متخصص في إعداد الامتحانات. أخرج JSON فقط."),
            new(MessageRole.User, prompt)
        };

        var response = await _llm.ChatAsync(messages, Enumerable.Empty<FunctionDefinition>());

        var rawJson = response.Content?.Trim();
        if (string.IsNullOrEmpty(rawJson))
            return new ExamResponse { Content = "لم يتم توليد الامتحان." };

        // Clean markdown code fences if present
        if (rawJson.StartsWith("```"))
        {
            var start = rawJson.IndexOf('\n') + 1;
            var end = rawJson.LastIndexOf("```");
            if (end > start)
                rawJson = rawJson[start..end].Trim();
        }

        return new ExamResponse { Content = rawJson };
    }
}
