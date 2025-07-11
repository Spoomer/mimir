using System.Text.Json.Serialization;
using Bencodex.Types;
using Lib9c.Models.Exceptions;
using Lib9c.Models.Extensions;
using Lib9c.Models.Stats;
using MongoDB.Bson.Serialization.Attributes;
using ValueKind = Bencodex.Types.ValueKind;

namespace Lib9c.Models.Items;

/// <summary>
/// <see cref="Nekoyume.Model.Item.Consumable"/>
/// </summary>
[BsonIgnoreExtraElements]
public record Consumable : ItemUsable
{
    public List<DecimalStat> Stats { get; init; }

    [BsonIgnore, GraphQLIgnore, JsonIgnore]
    public override IValue Bencoded => ((Dictionary)base.Bencoded)
        .Add("stats", new List(Stats
            .OrderBy(i => i.StatType)
            .ThenByDescending(i => i.BaseValue)
            .Select(s => s.BencodedWithoutAdditionalValue)));

    public Consumable()
    {
    }

    public Consumable(IValue bencoded) : base(bencoded)
    {
        try
        {
            var consumable = (Nekoyume.Model.Item.Consumable)Nekoyume.Model.Item.ItemFactory.Deserialize(bencoded);
            Stats = consumable.Stats.Select(s => new DecimalStat(s.Serialize())).ToList();
        }
        catch (ArgumentException)
        {
            throw new UnsupportedArgumentTypeException<ValueKind>(
                nameof(bencoded),
                new[] { ValueKind.Dictionary, ValueKind.List },
                bencoded.Kind);
        }
    }
}
