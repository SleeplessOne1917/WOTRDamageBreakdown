using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Stats;
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
using static UnityModManagerNet.UnityModManager.ModEntry;

namespace WOTRDamageBreakdown
{

    public class Main
    {
        public static ModLogger logger;

        static bool Load(UnityModManager.ModEntry modEntry)
        {
            logger = modEntry.Logger;
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

        private static bool IsSpell(ItemEntityWeapon weapon)
        {
            return weapon == null || weapon.Blueprint.Name == "Ray";
        }

        private static Modifier MapModifier(ModifiableValue.Modifier mod)
        {
            return new Modifier(mod.ModValue, mod.Source, mod.ModDescriptor);
        }

        public static void AppendDamageModifiersBreakdown(this StringBuilder sb, RuleDealDamage rule, int totalBonus, int trueTotal)
        {
            var modifiers = rule.ResultList.First().Source.Modifiers;
            var weapon = rule.DamageBundle.Weapon;
            var damageBonusStat = rule.AttackRoll?.WeaponStats.DamageBonusStat;
            var dice = rule.ResultList.First().Source.Dice;
            DamageType damageType = rule.ResultList.First().Source.Type;

            var additionalDamageModifiers = rule.Initiator.Stats.AdditionalDamage.Modifiers;
            if (!IsSpell(weapon) && additionalDamageModifiers.Count() > 0)
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

                    if (i == 0 && damageBonusStat.HasValue)
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

        public static string GetName(this EntityFact fact) {
            if (fact is Buff buff)
            {
                return buff.Name; ;
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
            if (rule == null)
                return;

            var firstDamage = rule.ResultList.Select(damageValue => damageValue.Source).First();
            var totalBonus = firstDamage.Modifiers.Sum(m => m.Value);
            int trueTotal = firstDamage.TotalBonus;
            var dice = rule.ResultList.Select(r => r.Source.Dice).First();
            var isZeroDice = dice.Dice == Kingmaker.RuleSystem.DiceType.Zero || dice.Rolls == 0;

            if (trueTotal != 0 && !isZeroDice)
            {
                sb.AppendLine();
                sb.AppendLine($"<b>Damage bonus: {UIConsts.GetValueWithSign(trueTotal)}</b>");
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

    [HarmonyPatch(typeof(ContextActionDealDamage), nameof(ContextActionDealDamage.GetDamageInfo))]
    class ContextActionDealDamageGetDamageInfoPatch
    {
        static void Postfix(ContextActionDealDamage __instance)
        {
            Main.logger.Log($"Bonus value type: {__instance.Value.BonusValue.ValueType}");
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