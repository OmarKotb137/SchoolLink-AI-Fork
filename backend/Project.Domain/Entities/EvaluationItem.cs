using SchoolLink.Domain.Enums;

namespace SchoolLink.Domain.Entities
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

        // Navigation Properties
        public EvaluationTemplate Template { get; set; } = null!;
    }
}
