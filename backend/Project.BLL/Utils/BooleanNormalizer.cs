using Project.Domain.Enums;

namespace Project.BLL.Utils;

/// <summary>
/// تطبيع قيم الإجابات المنطقية (صح/خطأ) من أي صيغة نصية إلى قيمة منطقية موحّدة،
/// وتطبيع <see cref="QuestionType.TrueFalse"/> القيم المخزّنة في عمود CorrectAnswer إلى الصيغة الكانونية "True"/"False".
/// هذا الـ helper هو المصدر الوحيد للحقيقة لتفسير قيم True/False في النظام بأكمله
/// لمنع تكرار المنطق (Code Duplication) الذي تسبّب سابقاً في عدم التعرّف على بعض المرادفات مثل "صواب".
/// </summary>
public static class BooleanNormalizer
{
    /// <summary>
    /// يحوّل نص إجابة صح/خطأ إلى قيمة منطقية.
    /// يقبل المرادفات العربية والإنجليزية الشائعة (غير حساس لحالة الأحرف).
    /// يرجع <c>null</c> لو القيمة فارغة أو غير معروفة.
    /// </summary>
    public static bool? NormalizeBoolean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "true" or "صح" or "صحيح" or "صحيحة" or "صواب" or "نعم" or "yes" or "y" or "1" => true,
            "false" or "خطأ" or "خطا" or "خطاء" or "لا" or "no" or "n" or "0" => false,
            _ => null
        };
    }

    /// <summary>
    /// يطبّع قيمة CorrectAnswer إلى الصيغة الكانونية الموحّدة قبل تخزينها في الـ DB.
    /// - للـ TrueFalse: يرجّع "True" أو "False" (أو يسيب القيمة زي ما هي لو مش متعرّف عليها، لتُرفض لاحقاً بالـ validation).
    /// - لأنواع الأسئلة الأخرى (MCQ/FillBlank/Essay): يرجّع النص زي ما هو بدون أي تعديل.
    /// </summary>
    public static string? NormalizeCanonicalCorrectAnswer(QuestionType type, string? value)
    {
        if (type != QuestionType.TrueFalse)
            return value;

        return NormalizeBoolean(value) switch
        {
            true => "True",
            false => "False",
            _ => value
        };
    }

    /// <summary>
    /// يتحقق إن قيمة CorrectAnswer لسؤال TrueFalse صالحة (قابلة للتفسير كقيمة منطقية).
    /// القيمة null أو الفارغة تُعتبر صالحة (سؤال بدون إجابة صحيحة محددة بعد).
    /// </summary>
    public static bool IsValidTrueFalseAnswer(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return true; // مسموح بتركها فارغة مؤقتاً

        return NormalizeBoolean(value).HasValue;
    }
}
