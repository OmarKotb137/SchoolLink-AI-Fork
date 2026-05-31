namespace SchoolLink.Domain.Entities
{
    public class AIGenerationLog : BaseEntity
    {
        public int UserId { get; set; }
        public string OperationType { get; set; } = string.Empty;
        public string? InputSummary { get; set; }
        public bool IsSuccess { get; set; }
        public int? TokensUsed { get; set; }
        public int? LatencyMs { get; set; }

        // Navigation Properties
        public User User { get; set; } = null!;
    }
}
