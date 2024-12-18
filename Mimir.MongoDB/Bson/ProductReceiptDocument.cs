using Lib9c.Models.Market;
using Libplanet.Crypto;

namespace Mimir.MongoDB.Bson;

public record ProductReceiptDocument : ProductDocument
{
    public Address BuyerAddress { get; set; }
    public ProductReceiptDocument(long storedBlockIndex, Address address, Address avatarAddress, Address buyerAddress, Address productsStateAddress, Product product) 
        : base(storedBlockIndex, address, avatarAddress, productsStateAddress, product)
    {
        BuyerAddress = buyerAddress;
    }

    public ProductReceiptDocument(long storedBlockIndex, Address address, Address avatarAddress, Address buyerAddress, Address productsStateAddress, Product product, decimal unitPrice, int? combatPoint, int? crystal, int? crystalPerPrice) 
        : base(storedBlockIndex, address, avatarAddress, productsStateAddress, product, unitPrice, combatPoint, crystal, crystalPerPrice)
    {
        BuyerAddress = buyerAddress;
    }
}