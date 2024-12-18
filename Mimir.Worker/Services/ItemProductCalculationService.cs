using Bencodex.Types;
using Lib9c.Models.Extensions;
using Lib9c.Models.Items;
using Lib9c.Models.Market;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.TableData.Crystal;
using ILogger = Serilog.ILogger;

namespace Mimir.Worker.Services;

public class ItemProductCalculationService : IItemProductCalculationService
{
    private readonly MongoDbService _store;

    public ItemProductCalculationService(MongoDbService store)
    {
        _store = store;
    }

    public async Task<int?> CalculateCombatPointAsync(ItemProduct itemProduct)
    {
        var costumeStatSheet = await _store.GetSheetAsync<CostumeStatSheet>();

        if (costumeStatSheet != null)
        {
            int? cp = itemProduct.TradableItem switch
            {
                ItemUsable itemUsable
                    => CPHelper.GetCP(
                        (Nekoyume.Model.Item.ItemUsable)
                        Nekoyume.Model.Item.ItemFactory.Deserialize(
                            (Dictionary)itemUsable.Bencoded
                        )
                    ),
                Costume costume => CPHelper.GetCP(new Nekoyume.Model.Item.Costume((Dictionary)costume.Bencoded), costumeStatSheet),
                _ => null
            };
            return cp;
        }
        
        return null;
    }

    public async Task<(int? crystal, int? crystalPerPrice)> CalculateCrystalMetricsAsync(ItemProduct itemProduct)
    {
        var crystalEquipmentGrindingSheet =
            await _store.GetSheetAsync<CrystalEquipmentGrindingSheet>();
        var crystalMonsterCollectionMultiplierSheet =
            await _store.GetSheetAsync<CrystalMonsterCollectionMultiplierSheet>();

        if (
            crystalEquipmentGrindingSheet != null
            && crystalMonsterCollectionMultiplierSheet != null
            && itemProduct.TradableItem is Equipment equipment
        )
        {
            var rawCrystal = CrystalCalculator.CalculateCrystal(
                [equipment.ToNekoyumeEquipment()],
                false,
                crystalEquipmentGrindingSheet,
                crystalMonsterCollectionMultiplierSheet,
                0
            );

            int crystal = (int)rawCrystal.MajorUnit;
            int crystalPerPrice = (int)
                rawCrystal.DivRem(itemProduct.Price.MajorUnit).Quotient.MajorUnit;

            return (crystal, crystalPerPrice);
        }

        return (null, null);
    }
}