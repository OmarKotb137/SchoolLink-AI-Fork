namespace Project.Domain.Entities
{
    public class ExamQuestionBankItem : BaseEntity
    {
        public int ExamId { get; set; }
        public int QuestionBankId { get; set; }
        public decimal Points { get; set; }
        public int DisplayOrder { get; set; }

        // Navigation Properties
        public Exam Exam { get; set; } = null!;
        public QuestionBank QuestionBank { get; set; } = null!;
    }
}
