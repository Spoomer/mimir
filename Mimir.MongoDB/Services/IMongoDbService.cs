using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Nekoyume.TableData;

namespace Mimir.MongoDB.Services;

public interface IMongoDbService
{
    IMongoCollection<T> GetCollection<T>(string collectionName);
    IMongoDatabase GetDatabase();
    GridFSBucket GetGridFs();
    Task<byte[]> RetrieveFromGridFs(ObjectId fileId);
    Task<T?> GetSheetAsync<T>(CancellationToken cancellationToken = default) where T : ISheet, new();
    IMongoCollection<BsonDocument> GetCollection<T>();
    IMongoCollection<BsonDocument> GetCollection(string collectionName);
}
