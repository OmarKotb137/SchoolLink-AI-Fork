using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Project.BLL.Embedding;

public class QuestionEmbeddingDocument
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string Id { get; set; } = ObjectId.GenerateNewId().ToString();

    public int QuestionBankId { get; set; }
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string EmbeddingText { get; set; } = string.Empty;

    [BsonRepresentation(BsonType.Double)]
    public float[] Embedding { get; set; } = Array.Empty<float>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
