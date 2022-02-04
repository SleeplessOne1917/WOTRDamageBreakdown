using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.Items;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
using Kingmaker.UnitLogic.Buffs;
using Kingmaker.UnitLogic.Mechanics.Actions;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityModManagerNet;

namespace WOTRDamageBreakdown
{

    public class Main
    {
        static bool Load(UnityModManager.ModEntry modEntry)
        {
            var harmony = new Harmony(modEntry.Info.Id);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            

            return true;
        }
    }

    public static class Bolstered
    {
        public static int BolsteredValue;
    }

    public static class DamageModifiersBreakdown
    {
        private static int CompareModifiers(Modifier x, Modifier y)
        {
            return ModifierDescriptorComparer.Instance.Compare(x.Descriptor, y.Descriptor);
        }

        public static void AppendDamageModifiersBreakdown(this StringBuilder sb, RuleDealDamage rule, int totalBonus, int trueTotal)
        {
            var modifiers = rule.ResultList.SelectMany(r => r.Source.Modifiers).ToList();
            var weapon = rule.DamageBundle.Weapon;
            var damageBonusStat = rule.AttackRoll?.WeaponStats.DamageBonusStat;
            
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

            ItemEntity ringOfPyromania;
            if (totalBonus != trueTotal && trueTotal - totalBonus >= 5 && IsWearingRingWithName(rule.Initiator, "Ring of Pyromania", out ringOfPyromania) && rule.ResultList.First().Source.Type == DamageType.Energy)
            {
                modifiers.Add(new Modifier(5, ringOfPyromania.Facts.List.First(), ModifierDescriptor.UntypedStackable));
                totalBonus += 5;
            }

            if (totalBonus != trueTotal && trueTotal - totalBonus >= 2 && rule.Initiator.Buffs.Enumerable.Any(b => b.Name == "Ring of Summons"))
            {
                var buff = rule.Initiator.Buffs.Enumerable.First(b => b.Name == "Ring of Summons");
                modifiers.Add(new Modifier(2, buff, ModifierDescriptor.UntypedStackable));
                totalBonus += 2;
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

                    if (i == 0 && damageBonusStat.HasValue)
                    {
                        source = damageBonusStat.Value.ToString();
                    }
                    else if (modifiers[i].Value == Bolstered.BolsteredValue && fact == null)
                    {
                        source = "Bolster Metamagic";
                    }
                    else if (modifiers[i].Descriptor == ModifierDescriptor.Enhancement && weapon != null)
                    {
                        const string plusPattern = @"\s+\+\d+";
                        var regex = new Regex(plusPattern);
                        source = regex.Replace(weapon.Blueprint.Name, string.Empty);
                    }
                    else if (fact != null && fact.GetName().Contains("Weapon Specialization"))
                    {
                        var typeName = weapon.Blueprint.Type.TypeName.ToString().Remove("Composite ");
                        source = $"{fact.GetName()} ({typeName})";
                    }
                    else if (fact != null && fact.GetName().Contains("Weapon Training"))
                    {
                        var parts = fact.GetName().Split(' ');
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

        private static bool IsWearingRingWithName(UnitEntityData initiator, string name, out ItemEntity ring)
        {
            ring = null;

            var ring1HasName = initiator.Body.Ring1.HasItem && initiator.Body.Ring1.Item.Name == name;
            if (ring1HasName)
                ring = initiator.Body.Ring1.Item;

            var ring2HasName = initiator.Body.Ring2.HasItem && initiator.Body.Ring2.Item.Name == name;
            if (ring2HasName)
                ring = initiator.Body.Ring2.Item;

            return ring1HasName || ring2HasName;
        }

        public static string GetName(this EntityFact fact) {
            if (fact is Buff buff)
            {
                return buff.Name.Remove("Enchantment"); ;
            }

            var pascalCase = fact.Blueprint?.name ?? fact.GetType().Name;
            pascalCase = pascalCase.Remove("Feature").Remove("Buff").Remove("Effect").Remove("Feat").Remove("Enchantment");
            var returnString = pascalCase.SpaceSeparatePascalCase();

            return returnString.Replace(" Of ", " of ").Replace(" The ", " the ");
        }

        public static bool IsUpperCase(this char character)
        {
            return character <= 90 && character >= 65;
        }

        public static string Remove(this string str, string strToRemove)
        {
            return str.Replace(strToRemove, string.Empty);
        }

        public static string SpaceSeparatePascalCase(this string pascalCase)
        {
            var returnString = pascalCase[0].ToString();

            for (var i = 1; i < pascalCase.Length; ++i)
            {
                if (pascalCase[i].IsUpperCase())
                    returnString += " ";

                returnString += pascalCase[i];
            }

            return returnString;
        }
    }

    [HarmonyPatch(typeof(DamageLogMessage), nameof(DamageLogMessage.AppendDamageDetails))]
    class DamageLogPatch
    {
        static void Postfix(StringBuilder sb, RuleDealDamage rule)
        {
            var totalBonus = rule.ResultList.Sum(damageValue => damageValue.Source.Modifiers.Sum(m => m.Value));
            int trueTotal = rule.ResultList.Sum(dv => dv.Source.TotalBonus);
            var isZeroDice = rule.ResultList.Any(r => r.Source.Dice.Dice == Kingmaker.RuleSystem.DiceType.Zero || r.Source.Dice.Rolls == 0);
            if (rule != null && trueTotal != 0 && !isZeroDice)
            {
                sb.Append('\n');
                sb.Append($"<b>Damage bonus: {UIConsts.GetValueWithSign(trueTotal)}</b>\n");
                sb.AppendDamageModifiersBreakdown(rule, totalBonus, trueTotal);
            }
        }
    }

    [HarmonyPatch(typeof(ContextActionDealDamage),
        nameof(ContextActionDealDamage.GetDamageRule),
        new Type[] { typeof(ContextActionDealDamage.DamageInfo), typeof(int)},
        new ArgumentType[] {ArgumentType.Normal, ArgumentType.Out})]
    class ContextActionDealDamageGetDamageRulePatch
    {
        static void Postfix(ref int bolsteredBonus, ref RuleDealDamage __result)
        {
            Bolstered.BolsteredValue = bolsteredBonus;

            if (bolsteredBonus > 0)
            {
                var baseDamage = __result.DamageBundle.First();
                baseDamage.AddModifier(new Modifier(bolsteredBonus, ModifierDescriptor.UntypedStackable));
                baseDamage.Bonus -= bolsteredBonus;
            }
        }
    }

    [HarmonyPatch(typeof(RuleDealDamage),
    MethodType.Constructor,
    new Type[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(DamageBundle) })]
    class RuleDealDamagePatch1
    {
        static void Postfix()
        {
            Bolstered.BolsteredValue = 0;
        }
    }

    [HarmonyPatch(typeof(RuleDealDamage),
   MethodType.Constructor,
   new Type[] { typeof(UnitEntityData), typeof(UnitEntityData), typeof(BaseDamage) })]
    class RuleDealDamagePatch2
    {
        static void Postfix()
        {
            Bolstered.BolsteredValue = 0;
        }
    }
}