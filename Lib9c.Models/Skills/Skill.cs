using System.Text.Json.Serialization;
using Bencodex;
using Bencodex.Types;
using Lib9c.Models.Exceptions;
using Lib9c.Models.Extensions;
using MongoDB.Bson.Serialization.Attributes;
using Nekoyume.Model.Skill;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using ValueKind = Bencodex.Types.ValueKind;

namespace Lib9c.Models.Skills;

/// <summary>
/// <see cref="Nekoyume.Model.Skill.Skill"/>
/// </summary>
[BsonIgnoreExtraElements]
public record Skill : IBencodable
{
    public SkillSheet.Row SkillRow { get; init; }
    public long Power { get; init; }
    public int Chance { get; init; }
    public int StatPowerRatio { get; init; }
    public StatType ReferencedStatType { get; init; }

    private readonly bool _skillRowHasCombo = false;

    [BsonIgnore, GraphQLIgnore, JsonIgnore]
    public IValue Bencoded
    {
        get
        {

            var list = List.Empty
                .Add(SkillRow.Serialize())
                .Add(Power.Serialize())
                .Add(Chance.Serialize());

            if (StatPowerRatio != default && ReferencedStatType != StatType.NONE)
            {
                list = list
                    .Add(StatPowerRatio.Serialize())
                    .Add(ReferencedStatType.Serialize());
            }

            return list;
        }
    }

    public Skill()
    {
    }

    public Skill(IValue bencoded)
    {
        try
        {
            var skill = SkillFactory.Deserialize(bencoded);
            SkillRow = skill.SkillRow;
            _skillRowHasCombo = SkillRow.Combo;
            Power = skill.Power;
            Chance = skill.Chance;
            StatPowerRatio = skill.StatPowerRatio;
            ReferencedStatType = skill.ReferencedStatType;
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
