using MongoDB.Driver;

namespace Project.DAL.MongoDb;

public interface IMongoDbContext
{
    IMongoCollection<T> GetCollection<T>(string name);
    IMongoDatabase Database { get; }
}
