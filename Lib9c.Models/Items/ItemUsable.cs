using System.Text.Json.Serialization;
using Bencodex.Types;
using Lib9c.Models.Exceptions;
using Lib9c.Models.Extensions;
using Lib9c.Models.Factories;
using Lib9c.Models.Skills;
using Lib9c.Models.Stats;
using MongoDB.Bson.Serialization.Attributes;
using ValueKind = Bencodex.Types.ValueKind;

namespace Lib9c.Models.Items;

/// <summary>
/// <see cref="Nekoyume.Model.Item.ItemUsable"/>
/// </summary>
[BsonIgnoreExtraElements]
public record ItemUsable : ItemBase
{
    public Guid ItemId { get; init; }

    public StatMap StatsMap { get; init; }

    public List<Skill> Skills { get; init; }

    public List<Skill> BuffSkills { get; init; }
    public long RequiredBlockIndex { get; init; }

    [BsonIgnore, GraphQLIgnore, JsonIgnore]
    public override IValue Bencoded => ((Dictionary)base.Bencoded)
        .Add("itemId", ItemId.Serialize())
        .Add("statsMap", StatsMap.Bencoded)
        .Add("skills", new List(Skills
            .OrderByDescending(i => i.Chance)
            .ThenByDescending(i => i.Power)
            .Select(s => s.Bencoded)))
        .Add("buffSkills", new List(BuffSkills
            .OrderByDescending(i => i.Chance)
            .ThenByDescending(i => i.Power)
            .Select(s => s.Bencoded)))
        .Add("requiredBlockIndex", RequiredBlockIndex.Serialize());

    public ItemUsable()
    {
    }

    public ItemUsable(IValue bencoded) : base(bencoded)
    {
        try
        {
            var itemUsable = (Nekoyume.Model.Item.ItemUsable)Nekoyume.Model.Item.ItemFactory.Deserialize(bencoded);
            ItemId = itemUsable.ItemId;
            StatsMap = new StatMap(itemUsable.StatsMap.Serialize());
            Skills = itemUsable.Skills.Select(s => new Skill(s.Serialize())).ToList();
            BuffSkills = itemUsable.BuffSkills.Select(s => new Skill(s.Serialize())).ToList();
            RequiredBlockIndex = itemUsable.RequiredBlockIndex;
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
