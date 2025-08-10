using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.Extensions.Options;
using Mimir.MongoDB.Bson;
using Mimir.MongoDB.Bson.Serialization;
using Mimir.Options;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Nekoyume;
using Nekoyume.TableData;
using Serilog;

namespace Mimir.MongoDB.Services;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoDatabase _database;
    private readonly GridFSBucket _gridFs;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IMongoCollection<BsonDocument>> _stateCollectionMappings;


    public MongoDbService(IOptions<DatabaseOption> databaseOption)
    {
        _logger = Log.ForContext<IMongoDbService>();

        SerializationRegistry.Register();
        var settings = MongoClientSettings.FromUrl(
            new MongoUrl(databaseOption.Value.ConnectionString)
        );

        if (databaseOption.Value.CAFile is not null)
        {
            X509Store localTrustStore = new X509Store(StoreName.Root);
            X509Certificate2Collection certificateCollection = new X509Certificate2Collection();
            certificateCollection.Import(databaseOption.Value.CAFile);
            try
            {
                localTrustStore.Open(OpenFlags.ReadWrite);
                localTrustStore.AddRange(certificateCollection);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Root certificate import failed: " + ex.Message);
                throw;
            }
            finally
            {
                localTrustStore.Close();
            }
        }

        var client = new MongoClient(settings);
        _database = client.GetDatabase(databaseOption.Value.Database);
        _gridFs = new GridFSBucket(_database);
        _stateCollectionMappings = InitStateCollections();

    }
    private Dictionary<string, IMongoCollection<BsonDocument>> InitStateCollections()
    {
        var mappings = new Dictionary<string, IMongoCollection<BsonDocument>>();
        var collectionNames = CollectionNames.CollectionAndStateTypeMappings.Values
            .Concat(CollectionNames.CollectionAndAddressMappings.Values);
        foreach (var collectionName in collectionNames)
        {
            var collection = _database.GetCollection<BsonDocument>(collectionName);
            mappings[collectionName] = collection;

            if (collection.CountDocuments(Builders<BsonDocument>.Filter.Empty) > 0)
            {
                continue;
            }

            try
            {
                _database.CreateCollection(collectionName);
            }
            catch (MongoCommandException e)
            {
                // ignore already exists
                _logger.Debug(
                    e,
                    "Collection already exists. {CollectionName}",
                    collectionName);
            }
        }

        return mappings;
    }

    public IMongoCollection<T> GetCollection<T>(string collectionName)
    {
        var database = GetDatabase();
        return database.GetCollection<T>(collectionName);
    }

    public IMongoDatabase GetDatabase()
    {
        return _database;
    }

    public GridFSBucket GetGridFs()
    {
        return _gridFs;
    }

    public async Task<byte[]> RetrieveFromGridFs(ObjectId fileId)
    {
        return await RetrieveFromGridFs(_gridFs, fileId);
    }

    public static async Task<byte[]> RetrieveFromGridFs(GridFSBucket gridFs, ObjectId fileId)
    {
        var fileBytes = await gridFs.DownloadAsBytesAsync(fileId);
        return fileBytes;
    }
    
    private static async Task<string> RetrieveStringFromGridFs(GridFSBucket gridFs, ObjectId fileId)
    {
        return Encoding.UTF8.GetString(await RetrieveFromGridFs(gridFs, fileId));
    }
    
    public IMongoCollection<BsonDocument> GetCollection<T>()
    {
        var collectionName = CollectionNames.GetCollectionName<T>();
        return GetCollection(collectionName);
    }
    
    public IMongoCollection<BsonDocument> GetCollection(string collectionName)
    {
        if (_stateCollectionMappings.TryGetValue(collectionName, out var collection))
        {
            return collection;
        }

        throw new InvalidOperationException($"No collection mapping found for name: {collectionName}");
    }
    
    public async Task<T?> GetSheetAsync<T>(CancellationToken cancellationToken = default)
        where T : ISheet, new()
    {
        var address = Addresses.GetSheetAddress<T>();
        var filter = Builders<BsonDocument>.Filter.Eq("_id", address.ToHex());
        var document = await GetCollection<SheetDocument>()
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return default;
        }

        var csv = await RetrieveStringFromGridFs(_gridFs, document["RawStateFileId"].AsObjectId);
        var sheet = new T();
        sheet.Set(csv);
        return sheet;
    }

}
