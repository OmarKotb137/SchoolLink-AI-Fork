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

    /// <summary>إضافة كل أسئلة بنك الأسئلة غير المضمنة</summary>
    Task<OperationResult<int>> EmbedAllUnembeddedAsync(CancellationToken ct = default);

    /// <summary>حذف embedding من MongoDB عند حذف سؤال من بنك الأسئلة</summary>
    Task<OperationResult> DeleteByQuestionBankIdAsync(int questionBankId, CancellationToken ct = default);

    /// <summary>إعادة إنشاء embedding بعد تعديل سؤال في بنك الأسئلة</summary>
    Task<OperationResult> ReEmbedQuestionAsync(int questionBankId, CancellationToken ct = default);
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

        // Check which IDs already have embeddings in MongoDB
        var existingIds = await Collection
            .Find(Builders<QuestionEmbeddingDocument>.Filter.In(x => x.QuestionBankId, qbItems.Select(q => q.Id)))
            .Project(x => x.QuestionBankId)
            .ToListAsync(ct);

        var existingSet = new HashSet<int>(existingIds);

        var newItems = qbItems.Where(q => !existingSet.Contains(q.Id)).ToList();

        if (newItems.Count == 0)
        {
            return OperationResult<int>.Success(qbItems.Count, "الأسئلة موجودة مسبقاً في البحث الذكي");
        }

        _logger.LogInformation("إضافة {Count} سؤال جديد (تخطي {Skipped} موجود مسبقاً)", newItems.Count, existingSet.Count);

        // Build texts for new items only
        var embedTexts = newItems.Select(q => BuildQuestionTextFromQb(q)).ToArray();
        var displayTexts = newItems.Select(q => BuildDisplayTextFromQb(q)).ToArray();

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

        if (embeddings.Length != newItems.Count)
            return OperationResult<int>.Failure("عدد embeddings لا يطابق عدد الأسئلة");

        var docs = new List<QuestionEmbeddingDocument>();
        for (int i = 0; i < newItems.Count; i++)
        {
            docs.Add(new QuestionEmbeddingDocument
            {
                QuestionBankId = newItems[i].Id,
                SubjectId = newItems[i].SubjectId,
                GradeLevelId = newItems[i].GradeLevelId,
                QuestionText = displayTexts[i],
                EmbeddingText = embedTexts[i],
                Embedding = embeddings[i],
                CreatedAt = DateTime.UtcNow
            });
        }

        await Collection.InsertManyAsync(docs, cancellationToken: ct);

        var totalMsg = existingSet.Count > 0
            ? $"تم إضافة {docs.Count} سؤال جديد (+{existingSet.Count} موجود مسبقاً)"
            : $"تم إضافة {docs.Count} سؤال بنجاح";

        return OperationResult<int>.Success(docs.Count, totalMsg);
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

        var topResults = scored
            .Where(s => s.Score >= request.MinScore)
            .OrderByDescending(s => s.Score)
            .Take(request.Limit)
            .ToList();

        if (topResults.Count == 0)
            return OperationResult<List<SemanticSearchResult>>.Success([], "لا توجد نتائج");

        // Batch-fetch all QuestionBank items from SQL (single query, no concurrency)
        var qbIds = topResults.Select(s => s.Doc.QuestionBankId).Distinct().ToList();
        var qbMap = new Dictionary<int, Domain.Entities.QuestionBank>();
        foreach (var qid in qbIds)
        {
            var item = await _unitOfWork.QuestionBank.GetByIdAsync(qid);
            if (item != null)
                qbMap[qid] = item;
        }

        var finalResults = topResults.Select(s =>
        {
            qbMap.TryGetValue(s.Doc.QuestionBankId, out var found);
            return new SemanticSearchResult
            {
                QuestionBankId = s.Doc.QuestionBankId,
                SubjectId = s.Doc.SubjectId,
                GradeLevelId = s.Doc.GradeLevelId,
                QuestionText = s.Doc.QuestionText,
                CorrectAnswer = found?.CorrectAnswer,
                OptionsJson = found?.OptionsJson,
                Score = Math.Round(s.Score, 4)
            };
        }).ToList();

        return OperationResult<List<SemanticSearchResult>>.Success(
            finalResults.OrderByDescending(r => r.Score).ToList(),
            $"تم العثور على {finalResults.Count} نتيجة");
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

    public async Task<OperationResult> DeleteByQuestionBankIdAsync(int questionBankId, CancellationToken ct = default)
    {
        var result = await Collection.DeleteOneAsync(
            Builders<QuestionEmbeddingDocument>.Filter.Eq(x => x.QuestionBankId, questionBankId),
            ct);

        if (result.DeletedCount > 0)
            _logger.LogInformation("تم حذف embedding من MongoDB لسؤال بنك الأسئلة {QuestionBankId}", questionBankId);
        else
            _logger.LogWarning("لم يتم العثور على embedding في MongoDB لسؤال بنك الأسئلة {QuestionBankId}", questionBankId);

        return OperationResult.Success("تم حذف الـ embedding من MongoDB بنجاح");
    }

    public async Task<OperationResult> ReEmbedQuestionAsync(int questionBankId, CancellationToken ct = default)
    {
        // 1. احذف القديم من MongoDB
        await DeleteByQuestionBankIdAsync(questionBankId, ct);

        // 2. أضف الجديد
        var embedResult = await EmbedQuestionBankItemsAsync([questionBankId], ct);
        if (!embedResult.IsSuccess)
            return OperationResult.Failure(embedResult.Message);

        return OperationResult.Success("تم تحديث الـ embedding بنجاح");
    }

    #region Helpers

    private static readonly System.Text.Json.JsonSerializerOptions _jsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>نص الـ embedding: نص السؤال + الاختيارات (للبحث الدلالي)</summary>
    private static string BuildQuestionTextFromQb(Domain.Entities.QuestionBank qb)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(qb.QuestionText);

        if (!string.IsNullOrWhiteSpace(qb.OptionsJson))
        {
            try
            {
                var options = System.Text.Json.JsonSerializer.Deserialize<List<QbOptionParse>>(qb.OptionsJson, _jsonOpts);
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

    /// <summary>نص العرض: نص السؤال فقط (بدون اختيارات) للعرض النظيف</summary>
    private static string BuildDisplayTextFromQb(Domain.Entities.QuestionBank qb)
    {
        return qb.QuestionText.Trim();
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
