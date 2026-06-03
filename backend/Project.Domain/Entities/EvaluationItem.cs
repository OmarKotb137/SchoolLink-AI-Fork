using Project.Domain.Enums;

namespace Project.Domain.Entities
{
    public class EvaluationItem : BaseEntity
    {
        public int TemplateId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MaxScore { get; set; }
        public decimal Weight { get; set; } = 1;
        public ItemType ItemType { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsVisible { get; set; } = true;
        public AutoCalcType AutoCalcType { get; set; } = AutoCalcType.None;
        public decimal? AbsenceMaxScore { get; set; }

        // Navigation Properties
        public EvaluationTemplate Template { get; set; } = null!;
    }
}
