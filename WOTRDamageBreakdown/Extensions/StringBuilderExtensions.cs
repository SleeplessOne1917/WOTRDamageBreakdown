using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace WOTRDamageBreakdown.Extensions
{
    public static class StringBuilderExtensions
    {
        public static void AppendDamageModifiersBreakdown(this StringBuilder sb, RuleDealDamage rule, List<Modifier> modifiers)
        {
            var weapon = rule.DamageBundle.Weapon;
            var damageBonusStat = rule.AttackRoll?.WeaponStats.DamageBonusStat;

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
    }
}
