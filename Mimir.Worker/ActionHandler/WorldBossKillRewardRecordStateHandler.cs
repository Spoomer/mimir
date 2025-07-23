using Bencodex.Types;
using Lib9c.Models.Exceptions;
using Lib9c.Models.Extensions;
using Libplanet.Crypto;
using Microsoft.Extensions.Options;
using Mimir.MongoDB.Bson;
using Mimir.Worker.Client;
using Mimir.Worker.Initializer.Manager;
using Mimir.Worker.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Nekoyume;
using Nekoyume.Extensions;
using Nekoyume.TableData;
using Serilog;

namespace Mimir.Worker.ActionHandler;

public class WorldBossKillRewardRecordStateHandler(
    IStateService stateService, 
    MongoDbService store, 
    IHeadlessGQLClient headlessGqlClient, 
    IInitializerManager initializerManager,
    IOptions<Configuration> configuration)
    : BaseActionHandler<WorldBossKillRewardRecordDocument>(
        stateService, store, headlessGqlClient, initializerManager, "^raid[0-9]*$", Log.ForContext<WorldBossKillRewardRecordStateHandler>(), configuration)
{
    protected override async Task<IEnumerable<WriteModel<BsonDocument>>> HandleActionAsync(
        long blockIndex,
        Address signer,
        IValue actionPlainValue,
        string actionType,
        IValue? actionPlainValueInternal,
        IClientSessionHandle? session = null,
        CancellationToken stoppingToken = default)
    {
        if (actionPlainValueInternal is null)
        {
            throw new ArgumentNullException(nameof(actionPlainValueInternal));
        }

        if (actionPlainValueInternal is not Dictionary d)
        {
            throw new UnsupportedArgumentTypeException<ValueKind>(
                nameof(actionPlainValueInternal),
                [ValueKind.Dictionary],
                actionPlainValueInternal.Kind);
        }

        var avatarAddress = d["a"].ToAddress();
        var worldBossListSheet = await Store.GetSheetAsync<WorldBossListSheet>(stoppingToken);
        if (worldBossListSheet is null)
        {
            throw new InvalidOperationException($"{nameof(WorldBossKillRewardRecordStateHandler)} requires {nameof(WorldBossListSheet)}");
        }

        var row = worldBossListSheet.FindRowByBlockIndex(blockIndex);
        var raidId = row.Id;

        var worldBossKillRewardRecordAddress = Addresses.GetWorldBossKillRewardRecordAddress(
            avatarAddress,
            raidId);
        var worldBossKillRewardRecordState = await StateGetter.GetWorldBossKillRewardRecordStateAsync(
            worldBossKillRewardRecordAddress,
            stoppingToken);
        return
        [
            new WorldBossKillRewardRecordDocument(
                blockIndex,
                worldBossKillRewardRecordAddress,
                avatarAddress,
                worldBossKillRewardRecordState).ToUpdateOneModel()
        ];
    }
}
