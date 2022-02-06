using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WOTRDamageBreakdown.Extensions
{
    public static class StringBuilderExtensions
    {
        public static void AppendDamageModifiersBreakdown(this StringBuilder sb, RuleDealDamage rule, int totalBonus, int trueTotal)
        {
            var modifiers = rule.ResultList.First().Source.Modifiers;
            var weapon = rule.DamageBundle.Weapon;
            var damageBonusStat = rule.AttackRoll?.WeaponStats.DamageBonusStat;
            DamageType damageType = rule.ResultList.First().Source.Type;

            var additionalDamageModifiers = rule.Initiator.Stats.AdditionalDamage.Modifiers;
            if ((!rule.SourceAbility?.IsSpell ?? true) && (!rule.SourceAbility?.IsCantrip ?? true) && additionalDamageModifiers.Count() > 0)
            {
                var stackableModifiers = additionalDamageModifiers
                    .Where(m => m.Stacks)
                    .Select(MapModifier);
                var nonStackableModifiers = additionalDamageModifiers
                    .Where(m => !m.Stacks)
                    .GroupBy(m => m.ModDescriptor)
                    .Select(g => MapModifier(g.MaxBy(m => m.ModValue)));

                modifiers.AddRange(stackableModifiers);
                modifiers.AddRange(nonStackableModifiers);

                totalBonus += stackableModifiers.Sum(m => m.Value) + nonStackableModifiers.Sum(m => m.Value);
            }

            if (totalBonus != trueTotal)
            {
                var unitPartWeaponTraining = rule.Initiator.Descriptor.Get<UnitPartWeaponTraining>();
                var weaponTrainingRank = unitPartWeaponTraining?.GetWeaponRank(weapon);
                if (weaponTrainingRank.HasValue && weaponTrainingRank.Value > 0)
                {
                    modifiers.Add(new Modifier(weaponTrainingRank.Value, unitPartWeaponTraining.WeaponTrainings.First(), ModifierDescriptor.None));
                    totalBonus += weaponTrainingRank.Value;
                }
            }

            if (totalBonus != trueTotal)
                modifiers.Add(new Modifier(trueTotal - totalBonus, ModifierDescriptor.UntypedStackable));

            if (modifiers.Count <= 0)
                return;

            modifiers.Sort(new Comparison<Modifier>(CompareModifiers));

            for (var i = 0; i < modifiers.Count; ++i)
            {
                if (modifiers[i].Value != 0)
                {
                    string source;
                    var fact = modifiers[i].Fact;

                    if (i == 0 && damageBonusStat.HasValue && modifiers[i].Descriptor == ModifierDescriptor.None && (!fact?.GetName()?.Contains("Weapon Training") ?? true))
                    {
                        source = damageBonusStat.Value.ToString();
                    }
                    else if (modifiers[i].Value == Bolstered.BolsteredValue && fact == null)
                    {
                        source = "Bolster Metamagic";
                    }
                    else if (modifiers[i].Descriptor == ModifierDescriptor.Enhancement)
                    {
                        const string plusPattern = @"\s+\+\d+";
                        var regex = new Regex(plusPattern);
                        source = regex.Replace(weapon.Blueprint.Name, string.Empty);
                    }
                    else if (fact?.GetName()?.Contains("Weapon Specialization") ?? false)
                    {
                        var typeName = weapon.Blueprint.Type.TypeName.ToString().Remove("Composite ");
                        source = $"{fact.GetName()} ({typeName})";
                    }
                    else if (fact?.GetName()?.Contains("Weapon Training")  ?? false)
                    {
                        var parts = fact.GetName().Split(' ').ToList();
                        var last = parts.Last();
                        if (last == "Double" || last == "Thrown")
                        {
                            parts.Add("Weapons");
                        }

                        if (last == "Hammers")
                            source = $"{string.Join(" ", parts.Take(2))} (Hammers, Maces, and Flails)";
                        else
                            source = $"{string.Join(" ", parts.Take(2))} ({string.Join(" ", parts.Skip(2))})";
                    }
                    else
                    {
                        source = fact?.GetName();
                    }

                    sb.AppendBonus(modifiers[i].Value, source, modifiers[i].Descriptor);
                }
            }
        }

        private static int CompareModifiers(Modifier x, Modifier y)
        {
            return ModifierDescriptorComparer.Instance.Compare(x.Descriptor, y.Descriptor);
        }

        private static Modifier MapModifier(ModifiableValue.Modifier mod)
        {
            return new Modifier(mod.ModValue, mod.Source, mod.ModDescriptor);
        }
    }
}
