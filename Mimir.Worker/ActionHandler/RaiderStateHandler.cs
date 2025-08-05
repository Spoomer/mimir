using Bencodex.Types;
using Lib9c.Models.Exceptions;
using Lib9c.Models.Extensions;
using Libplanet.Crypto;
using Mimir.MongoDB.Bson;
using Mimir.MongoDB.Services;
using Mimir.Shared.Client;
using Mimir.Shared.Constants;
using Mimir.Shared.Services;
using Mimir.Worker.Initializer.Manager;
using MongoDB.Bson;
using MongoDB.Driver;
using Nekoyume;
using Nekoyume.Extensions;
using Nekoyume.TableData;
using Serilog;

namespace Mimir.Worker.ActionHandler;

public class RaiderStateHandler(
    IStateService stateService,
    IMongoDbService store,
    IHeadlessGQLClient headlessGqlClient,
    IInitializerManager initializerManager,
    IStateGetterService stateGetterService,
    PlanetType planetType
)
    : BaseActionHandler<RaiderStateDocument>(
        stateService,
        store,
        headlessGqlClient,
        initializerManager,
        "^raid[0-9]*$",
        Log.ForContext<RaiderStateHandler>(),
        stateGetterService
    )
{
    protected override async Task<IEnumerable<WriteModel<BsonDocument>>> HandleActionAsync(
        long blockIndex,
        Address signer,
        IValue actionPlainValue,
        string actionType,
        IValue? actionPlainValueInternal,
        IClientSessionHandle? session = null,
        CancellationToken stoppingToken = default
    )
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
                actionPlainValueInternal.Kind
            );
        }

        var avatarAddress = d["a"].ToAddress();
        var worldBossListSheet = await Store.GetSheetAsync<WorldBossListSheet>(stoppingToken);
        if (worldBossListSheet is null)
        {
            throw new InvalidOperationException(
                $"{nameof(WorldBossKillRewardRecordStateHandler)} requires ${nameof(WorldBossListSheet)}"
            );
        }

        var row = worldBossListSheet.FindRowByBlockIndex(blockIndex);
        var raidId = row.Id;
        var raiderAddress = Addresses.GetRaiderAddress(avatarAddress, raidId);
        var raiderState = await StateGetter.GetRaiderStateAsync(raiderAddress, stoppingToken);
        return [new RaiderStateDocument(blockIndex, raiderAddress, raiderState).ToUpdateOneModel()];
    }
}
