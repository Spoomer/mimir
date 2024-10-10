using System.Text.RegularExpressions;
using Bencodex.Types;
using Lib9c.Models.Extensions;
using Lib9c.Models.States;
using Libplanet.Crypto;
using Mimir.MongoDB;
using Mimir.MongoDB.Bson;
using Mimir.Worker.Exceptions;
using Mimir.Worker.Services;
using MongoDB.Driver;
using Serilog;

namespace Mimir.Worker.ActionHandler;

public class PetStateHandler(IStateService stateService, MongoDbService store)
    : BaseActionHandler(
        stateService,
        store,
        "^pet_enhancement[0-9]*$|^combination_equipment[0-9]*$|^rapid_combination[0-9]*$",
        Log.ForContext<PetStateHandler>())
{
    protected override async Task<bool> TryHandleAction(
        long blockIndex,
        string actionType,
        IValue? actionPlainValueInternal,
        IClientSessionHandle? session = null,
        CancellationToken stoppingToken = default)
    {
        if (actionPlainValueInternal is not Dictionary actionValues)
        {
            throw new InvalidTypeOfActionPlainValueInternalException(
                [ValueKind.Dictionary],
                actionPlainValueInternal?.Kind);
        }

        Address avatarAddress;
        int petId;

        if (Regex.IsMatch(actionType, "^pet_enhancement[0-9]*$"))
        {
            avatarAddress = actionValues["a"].ToAddress();
            petId = actionValues["p"].ToInteger();
        }
        else if (Regex.IsMatch(actionType, "^combination_equipment[0-9]*$"))
        {
            avatarAddress = actionValues["a"].ToAddress();
            var pid = actionValues["pid"].ToNullableInteger();
            if (pid is null)
            {
                return false;
            }

            petId = pid.Value;
        }
        else if (Regex.IsMatch(actionType, "^rapid_combination[0-9]*$"))
        {
            avatarAddress = actionValues["avatarAddress"].ToAddress();
            var slotIndex = actionValues["slotIndex"].ToInteger();
            AllCombinationSlotState allCombinationSlotState;
            try
            {
                allCombinationSlotState = await StateGetter.GetAllCombinationSlotStateAsync(
                    avatarAddress,
                    stoppingToken);    
            }
            catch (Exception e)
            {
                Logger.Fatal(e, "Failed to get AllCombinationSlotState for avatar: {AvatarAddress}", avatarAddress);
                return false;
            }

            if (!allCombinationSlotState.CombinationSlots.TryGetValue(slotIndex, out var combinationSlotState))
            {
                Logger.Fatal("CombinationSlotState not found for slotIndex: {SlotIndex}", slotIndex);
                return false;
            }

            if (combinationSlotState.PetId is null)
            {
                // ignore
                return true;
            }

            petId = combinationSlotState.PetId.Value;
        }
        else
        {
            throw new ArgumentException($"Unknown actionType: {actionType}");
        }

        Logger.Information("Handle pet_state, avatar: {AvatarAddress} ", avatarAddress);

        var petStateAddress = Nekoyume.Model.State.PetState.DeriveAddress(avatarAddress, petId);
        var petState = await StateGetter.GetPetState(petStateAddress);

        await Store.UpsertStateDataManyAsync(
            CollectionNames.GetCollectionName<PetStateDocument>(),
            [new PetStateDocument(petStateAddress, petState)],
            session,
            stoppingToken
        );

        return true;
    }
}
