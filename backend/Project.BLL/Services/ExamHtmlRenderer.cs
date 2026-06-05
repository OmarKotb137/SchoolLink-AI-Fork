using System.Net;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Project.BLL.Interfaces;
using Project.DAL.Context;
using Project.Domain.Entities;

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

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html><html lang='ar' dir='rtl'><head><meta charset='utf-8'>");
            sb.Append("<style>");
            sb.Append(RenderCss());
            sb.Append("</style></head><body>");
            sb.Append(RenderHeader(exam));
            sb.Append("<main class='exam-body'>");

            int qNumber = 1;
            var blocks = BuildBlocks(groups, standalone);

            foreach (var block in blocks)
            {
                if (block.Group != null)
                    sb.Append(RenderGroup(block.Group, ref qNumber));
                else if (block.Question != null)
                    sb.Append(RenderSingleQuestion(block.Question, ref qNumber));
            }

            sb.Append("</main>");
            sb.Append(RenderFooter());
            sb.Append("</body></html>");

            return sb.ToString();
        }

        private string RenderCss()
        {
            return @"
*{box-sizing:border-box;margin:0;padding:0}
@page{size:A4;margin:18mm 15mm}
body{font-family:'Cairo','Times New Roman',serif;color:#000;background:#fff;font-size:14px;line-height:1.7}
.exam-header{text-align:center;border-bottom:2px solid #000;padding-bottom:10px;margin-bottom:18px}
.exam-header h1{margin:0 0 4px;font-size:20px}
.exam-meta{display:flex;justify-content:space-between;font-size:13px}
.exam-body{padding:0 4px}
.exam-block{margin-bottom:22px;page-break-inside:avoid}
.passage-box{border:1.5px solid #000;padding:12px 14px;margin-bottom:12px}
.passage-title{margin:0 0 8px;font-size:15px;font-weight:700}
.passage-text{text-align:justify}
.exam-figure{text-align:center;margin:10px 0}
.exam-figure img{max-width:100%;max-height:380px;filter:grayscale(100%);display:inline-block;border:1px solid #000}
.exam-figure svg{max-width:100%;max-height:380px;display:inline-block}
.exam-figure figcaption{font-size:12px;margin-top:5px}
.q-item{margin:10px 0;padding:4px 0}
.q-text{font-weight:600;margin:0 0 4px}
.q-options{list-style:upper-alpha;padding-inline-start:24px;margin:4px 0}
.q-options li{margin:2px 0}
.q-points{font-size:12px;float:left}
.answer-line{border-bottom:1px dotted #000;min-height:22px;margin:6px 0;width:80%}
.answer-line.tall{min-height:60px}
.exam-footer{border-top:1px solid #000;margin-top:24px;padding-top:8px;text-align:center;font-size:12px}
table.exam-content{width:100%;border-collapse:collapse;margin-bottom:12px}
table.exam-content td,table.exam-content th{border:1px solid #000;padding:8px}
table.exam-content th{background:#f0f0f0;font-weight:700}
@media print{.no-print{display:none}}
";
        }

        private string RenderHeader(Exam exam)
        {
            var subj = exam.ClassSubjectTeacher?.Subject?.Name ?? "—";
            return $@"
<header class='exam-header'>
<h1>{Enc(exam.Title)}</h1>
<div class='exam-meta'>
<span>المادة: {Enc(subj)}</span>
<span>الزمن: {exam.DurationMinutes} دقيقة</span>
<span>الدرجة الكلية: {exam.TotalScore}</span>
</div>
</header>";
        }

        private string RenderGroup(ExamQuestionGroup g, ref int qNumber)
        {
            var sb = new StringBuilder();
            sb.Append("<section class='exam-block'>");

            switch (g.DisplayType)
            {
                case Domain.Enums.TemplateContentType.Passage:
                    sb.Append("<table class='exam-content'><tr><td>");
                    if (!string.IsNullOrWhiteSpace(g.ContentTitle))
                        sb.Append($"<div class='passage-title'>{Enc(g.ContentTitle)}</div>");
                    sb.Append($"<div class='passage-text'>{Enc(g.ContentText)}</div>");
                    sb.Append("</td></tr></table>");
                    break;

                case Domain.Enums.TemplateContentType.Image:
                case Domain.Enums.TemplateContentType.Map:
                    sb.Append("<figure class='exam-figure'>");
                    if (!string.IsNullOrWhiteSpace(g.ImageUrl))
                        sb.Append($"<img src='{Enc(g.ImageUrl)}' alt='{Enc(g.ContentText)}' />");
                    if (!string.IsNullOrWhiteSpace(g.ContentText))
                        sb.Append($"<figcaption>{Enc(g.ContentText)}</figcaption>");
                    sb.Append("</figure>");
                    break;

                case Domain.Enums.TemplateContentType.Diagram:
                    sb.Append($"<div class='exam-figure'>{g.ContentText}</div>");
                    break;
            }

            foreach (var q in g.Questions.OrderBy(x => x.DisplayOrder))
                sb.Append(RenderQuestionBody(q, qNumber++));

            sb.Append("</section>");
            return sb.ToString();
        }

        private string RenderSingleQuestion(ExamQuestion q, ref int qNumber)
        {
            var sb = new StringBuilder();
            sb.Append("<section class='exam-block'>");

            if (q.DisplayType == Domain.Enums.TemplateContentType.Diagram && !string.IsNullOrWhiteSpace(q.ContentText))
                sb.Append($"<div class='exam-figure'>{q.ContentText}</div>");

            sb.Append(RenderQuestionBody(q, qNumber++));
            sb.Append("</section>");
            return sb.ToString();
        }

        private string RenderQuestionBody(ExamQuestion q, int n)
        {
            var sb = new StringBuilder();
            sb.Append("<div class='q-item'>");
            sb.Append($"<span class='q-points'>({q.Points} درجة)</span>");
            sb.Append($"<p class='q-text'>{n}. {Enc(q.QuestionText)}</p>");

            switch (q.QuestionType)
            {
                case Domain.Enums.QuestionType.MultipleChoice:
                case Domain.Enums.QuestionType.TrueFalse:
                    sb.Append("<ol class='q-options'>");
                    foreach (var opt in q.Options.OrderBy(o => o.DisplayOrder))
                        sb.Append($"<li>{Enc(opt.OptionText)}</li>");
                    sb.Append("</ol>");
                    break;

                case Domain.Enums.QuestionType.FillBlank:
                    sb.Append("<div class='answer-line'></div>");
                    break;

                case Domain.Enums.QuestionType.Essay:
                    sb.Append("<div class='answer-line tall'></div>");
                    sb.Append("<div class='answer-line tall'></div>");
                    break;
            }

            sb.Append("</div>");
            return sb.ToString();
        }

        private string RenderFooter()
        {
            return "<footer class='exam-footer'>— انتهت الأسئلة ، مع تمنياتنا بالتوفيق —</footer>";
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
