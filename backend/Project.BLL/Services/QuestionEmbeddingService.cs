using Common.Results;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Project.BLL.Embedding;
using Project.DAL.Interfaces;
using Project.DAL.MongoDb;

namespace Project.BLL.Services;

public interface IQuestionEmbeddingService
{
    /// <summary>إضافة أسئلة من بنك الأسئلة (SQL) إلى MongoDB embedding</summary>
    Task<OperationResult<int>> EmbedQuestionBankItemsAsync(List<int> questionBankIds, CancellationToken ct = default);

    /// <summary>بحث دلالي مع فلترة حسب الصف والمادة</summary>
    Task<OperationResult<List<SemanticSearchResult>>> SemanticSearchAsync(SemanticSearchRequest request, CancellationToken ct = default);

    /// <summary>تضمين كل أسئلة بنك الأسئلة غير المضمنة</summary>
    Task<OperationResult<int>> EmbedAllUnembeddedAsync(CancellationToken ct = default);
}

public class SemanticSearchRequest
{
    public string Query { get; set; } = string.Empty;
    public int? GradeLevelId { get; set; }
    public int? SubjectId { get; set; }
    public int Limit { get; set; } = 10;
    public double MinScore { get; set; } = 0.5;
}

public class SemanticSearchResult
{
    public int QuestionBankId { get; set; }
    public int SubjectId { get; set; }
    public int GradeLevelId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string? CorrectAnswer { get; set; }
    public string? OptionsJson { get; set; }
    public double Score { get; set; }
}

public class QuestionEmbeddingService : IQuestionEmbeddingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMongoDbContext _mongoDb;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger<QuestionEmbeddingService> _logger;

    public QuestionEmbeddingService(
        IUnitOfWork unitOfWork,
        IMongoDbContext mongoDb,
        IEmbeddingService embeddingService,
        ILogger<QuestionEmbeddingService> logger)
    {
        _unitOfWork = unitOfWork;
        _mongoDb = mongoDb;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    private IMongoCollection<QuestionEmbeddingDocument> Collection
        => _mongoDb.GetCollection<QuestionEmbeddingDocument>("questions");

    public async Task<OperationResult<int>> EmbedQuestionBankItemsAsync(List<int> questionBankIds, CancellationToken ct = default)
    {
        if (questionBankIds.Count == 0)
            return OperationResult<int>.Success(0);

        var qbItems = new List<Domain.Entities.QuestionBank>();
        foreach (var id in questionBankIds)
        {
            var item = await _unitOfWork.QuestionBank.GetByIdAsync(id);
            if (item != null && !item.IsDeleted)
                qbItems.Add(item);
        }

        if (qbItems.Count == 0)
            return OperationResult<int>.Failure("لم يتم العثور على أسئلة في بنك الأسئلة", 404);

        // Remove old embeddings
        await Collection.DeleteManyAsync(
            Builders<QuestionEmbeddingDocument>.Filter.In(x => x.QuestionBankId, questionBankIds),
            cancellationToken: ct);

        // Build texts
        var embedTexts = qbItems.Select(q => BuildQuestionTextFromQb(q)).ToArray();
        var displayTexts = qbItems.Select(q => BuildDisplayTextFromQb(q)).ToArray();

        float[][] embeddings;
        try
        {
            embeddings = await _embeddingService.GenerateEmbeddingsBatchAsync(embedTexts, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل توليد embeddings لبنك الأسئلة");
            return OperationResult<int>.Failure($"فشل توليد embeddings: {ex.Message}");
        }

        if (embeddings.Length != qbItems.Count)
            return OperationResult<int>.Failure("عدد embeddings لا يطابق عدد الأسئلة");

        var docs = new List<QuestionEmbeddingDocument>();
        for (int i = 0; i < qbItems.Count; i++)
        {
            docs.Add(new QuestionEmbeddingDocument
            {
                QuestionBankId = qbItems[i].Id,
                SubjectId = qbItems[i].SubjectId,
                GradeLevelId = qbItems[i].GradeLevelId,
                QuestionText = displayTexts[i],
                EmbeddingText = embedTexts[i],
                Embedding = embeddings[i],
                CreatedAt = DateTime.UtcNow
            });
        }

        await Collection.InsertManyAsync(docs, cancellationToken: ct);
        return OperationResult<int>.Success(docs.Count, $"تم تضمين {docs.Count} سؤال بنجاح");
    }

    public async Task<OperationResult<List<SemanticSearchResult>>> SemanticSearchAsync(
        SemanticSearchRequest request, CancellationToken ct = default)
    {
        float[] queryEmbedding;
        try
        {
            queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "فشل توليد embedding للبحث");
            return OperationResult<List<SemanticSearchResult>>.Failure($"فشل البحث: {ex.Message}");
        }

        if (queryEmbedding.Length == 0)
            return OperationResult<List<SemanticSearchResult>>.Success([], "لا توجد نتائج");

        // Build filter
        var filterBuilder = Builders<QuestionEmbeddingDocument>.Filter;
        var filters = new List<FilterDefinition<QuestionEmbeddingDocument>>();

        if (request.GradeLevelId.HasValue)
            filters.Add(filterBuilder.Eq(x => x.GradeLevelId, request.GradeLevelId.Value));
        if (request.SubjectId.HasValue)
            filters.Add(filterBuilder.Eq(x => x.SubjectId, request.SubjectId.Value));

        var filter = filters.Count > 0 ? filterBuilder.And(filters) : filterBuilder.Empty;

        // Fetch all matching docs (for small/medium datasets)
        var allDocs = await Collection.Find(filter)
            .SortByDescending(x => x.CreatedAt)
            .ToListAsync(ct);

        if (allDocs.Count == 0)
            return OperationResult<List<SemanticSearchResult>>.Success([], "لا توجد نتائج");

        // Compute cosine similarity
        var scored = new List<(QuestionEmbeddingDocument Doc, double Score)>(allDocs.Count);
        foreach (var doc in allDocs)
        {
            double sim = CosineSimilarity(queryEmbedding, doc.Embedding);
            scored.Add((doc, sim));
        }

        var results = scored
            .Where(s => s.Score >= request.MinScore)
            .OrderByDescending(s => s.Score)
            .Take(request.Limit)
            .Select(async s =>
            {
                // Fetch full question data from SQL
                var qb = await _unitOfWork.QuestionBank.GetByIdAsync(s.Doc.QuestionBankId);
                return new SemanticSearchResult
                {
                    QuestionBankId = s.Doc.QuestionBankId,
                    SubjectId = s.Doc.SubjectId,
                    GradeLevelId = s.Doc.GradeLevelId,
                    QuestionText = s.Doc.QuestionText,
                    CorrectAnswer = qb?.CorrectAnswer,
                    OptionsJson = qb?.OptionsJson,
                    Score = Math.Round(s.Score, 4)
                };
            })
            .ToList();

        // Wait for all the SQL lookups
        var finalResults = await Task.WhenAll(results);

        return OperationResult<List<SemanticSearchResult>>.Success(
            finalResults.OrderByDescending(r => r.Score).ToList(),
            $"تم العثور على {finalResults.Length} نتيجة");
    }

    public async Task<OperationResult<int>> EmbedAllUnembeddedAsync(CancellationToken ct = default)
    {
        // Get all QuestionBank IDs from SQL
        var allQbIds = new List<int>();
        // We need a method to get all QB IDs. Let's use FindAsync
        var allItems = await _unitOfWork.QuestionBank.FindAsync(q => !q.IsDeleted);
        var allIds = allItems.Select(q => q.Id).ToHashSet();

        // Get already embedded IDs from MongoDB
        var embeddedIds = await Collection
            .Find(Builders<QuestionEmbeddingDocument>.Filter.Empty)
            .Project(x => x.QuestionBankId)
            .ToListAsync(ct);

        var embeddedSet = new HashSet<int>(embeddedIds);
        var unembedded = allIds.Where(id => !embeddedSet.Contains(id)).ToList();

        if (unembedded.Count == 0)
            return OperationResult<int>.Success(0, "كل الأسئلة مضمنة بالفعل");

        return await EmbedQuestionBankItemsAsync(unembedded, ct);
    }

    #region Helpers

    /// <summary>نص الـ embedding: فقط نص السؤال + الاختيارات (بدون IDs ولا metadata)</summary>
    private static string BuildQuestionTextFromQb(Domain.Entities.QuestionBank qb)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(qb.QuestionText);

        if (!string.IsNullOrWhiteSpace(qb.OptionsJson))
        {
            try
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<List<QbOptionParse>>(qb.OptionsJson);
                if (options is not null && options.Count > 0)
                {
                    sb.AppendLine("الاختيارات:");
                    foreach (var opt in options.OrderBy(o => o.DisplayOrder))
                        sb.AppendLine($"- {opt.Text}");
                }
            }
            catch { }
        }

        return sb.ToString().Trim();
    }

    /// <summary>نص العرض: نفس المحتوى لكن للعرض للطالب</summary>
    private static string BuildDisplayTextFromQb(Domain.Entities.QuestionBank qb)
    {
        return BuildQuestionTextFromQb(qb);
    }

    private class QbOptionParse
    {
        public string Text { get; set; } = "";
        public bool IsCorrect { get; set; }
        public int DisplayOrder { get; set; }
    }

    private static double CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length) return 0;
        double dot = 0;
        for (int i = 0; i < a.Length; i++)
            dot += a[i] * b[i];
        return dot;
    }

    #endregion
}
