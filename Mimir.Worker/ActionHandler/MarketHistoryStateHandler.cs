using Bencodex.Types;
using Lib9c.Models.Market;
using Libplanet.Crypto;
using Mimir.MongoDB.Bson;
using Mimir.Worker.Client;
using Mimir.Worker.Exceptions;
using Mimir.Worker.Initializer.Manager;
using Mimir.Worker.Services;
using MongoDB.Bson;
using MongoDB.Driver;
using Nekoyume.Action;
using Nekoyume.Model.State;
using Serilog;

namespace Mimir.Worker.ActionHandler;

public class MarketHistoryStateHandler(
    IStateService stateService,
    MongoDbService store,
    IHeadlessGQLClient headlessGqlClient,
    IInitializerManager initializerManager
)
    : BaseActionHandler<ProductReceiptDocument>(
        stateService,
        store,
        headlessGqlClient,
        initializerManager,
        "^buy_product[0-9]*$",
        Log.ForContext<ProductStateHandler>()
    )
{
    protected override async Task<IEnumerable<WriteModel<BsonDocument>>> HandleActionAsync(long blockIndex, Address signer, IValue actionPlainValue, string actionType, IValue? actionPlainValueInternal, IClientSessionHandle? session = null, CancellationToken stoppingToken = default)
    {
        if (actionPlainValueInternal is not Dictionary actionValues)
        {
            throw new InvalidTypeOfActionPlainValueInternalException(
                [ValueKind.Dictionary],
                actionPlainValueInternal?.Kind
            );
        }

        var productInfos = GetProductInfos(actionValues);
        List<(Address avatarAddress, Guid productId)> avatarAddressesWithProductId = GetAvatarAddressesWithProductId(productInfos);
        var buyerAddress = GetBuyerAddress(actionValues);
        var ops = new List<WriteModel<BsonDocument>>();

        foreach (var (avatarAddress, productId) in avatarAddressesWithProductId)
        {
            var productsStateAddress = Nekoyume.Model.Market.ProductsState.DeriveAddress(
                avatarAddress
            );
            var product = await StateGetter.GetProductState(productId, stoppingToken, blockIndex);

            var productReceiptDocument = CreateProductReceiptDocumentAsync(blockIndex, avatarAddress, buyerAddress, productsStateAddress, product);
            ops.Add(productReceiptDocument.ToUpdateOneModel());
        }


        return ops;
    }

    private ProductDocument CreateProductReceiptDocumentAsync(
        long blockIndex,
        Address avatarAddress,
        Address buyerAddress,
        Address productsStateAddress,
        Product product
    )
    {
        var productAddress = Nekoyume.Model.Market.Product.DeriveAddress(product.ProductId);
        switch (product)
        {
            case ItemProduct itemProduct:
            {
                var unitPrice = CalculateUnitPrice(itemProduct);
                // var combatPoint = await CalculateCombatPointAsync(itemProduct);
                // var (crystal, crystalPerPrice) = await CalculateCrystalMetricsAsync(itemProduct);

                // return new ProductDocument(
                //     productAddress,
                //     avatarAddress,
                //     productsStateAddress,
                //     product,
                //     unitPrice,
                //     combatPoint,
                //     crystal,
                //     crystalPerPrice
                // );
                return new ProductReceiptDocument(
                    blockIndex,
                    productAddress,
                    avatarAddress,
                    buyerAddress,
                    productsStateAddress,
                    product,
                    unitPrice,
                    null,
                    null,
                    null
                );
            }
            case FavProduct favProduct:
            {
                var unitPrice = CalculateUnitPrice(favProduct);

                return new ProductReceiptDocument(
                    blockIndex,
                    productAddress,
                    avatarAddress,
                    buyerAddress,
                    productsStateAddress,
                    product,
                    unitPrice,
                    null,
                    null,
                    null
                );
            }
            default:
                return new ProductReceiptDocument(
                    blockIndex,
                    productAddress,
                    avatarAddress,
                    buyerAddress,
                    productsStateAddress,
                    product
                );
        }
    }

    private static Address GetBuyerAddress(Dictionary actionValues)
    {
        return actionValues["a"].ToAddress();
    }

    private static List<IProductInfo> GetProductInfos(Dictionary actionValues)
    {
        var serialized = (List)actionValues["p"];
        return serialized
            .Cast<List>()
            .Select(Nekoyume.Model.Market.ProductFactory.DeserializeProductInfo)
            .ToList();
    }

    private static List<(Address avatarAddress, Guid productId)> GetAvatarAddressesWithProductId(List<IProductInfo> productInfos)
    {
        var avatarAddresses = new List<(Address avatarAddress, Guid productId)>();

        foreach (var productInfo in productInfos)
        {
            avatarAddresses.Add((productInfo.AvatarAddress, productInfo.ProductId));
        }

        return avatarAddresses;
    }

    private static decimal CalculateUnitPrice(ItemProduct itemProduct)
    {
        return decimal.Parse(itemProduct.Price.GetQuantityString()) / itemProduct.ItemCount;
    }

    private static decimal CalculateUnitPrice(FavProduct favProduct)
    {
        return decimal.Parse(favProduct.Price.GetQuantityString())
               / decimal.Parse(favProduct.Asset.GetQuantityString());
    }
}