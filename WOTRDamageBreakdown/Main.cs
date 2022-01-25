using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
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

    public static class DamageModifiersBreakdown
    {
        private static int CompareModifiers(Modifier x, Modifier y)
        {
            return ModifierDescriptorComparer.Instance.Compare(x.Descriptor, y.Descriptor);
        }

        public static void AppendDamageModifiersBreakdown(this StringBuilder sb, RuleDealDamage rule)
        {
            var modifiers = rule.ResultList.SelectMany(r => r.Source.Modifiers).ToList();
            var weapon = rule.DamageBundle.Weapon;
            var damageBonusStat = rule.AttackRoll?.WeaponStats.DamageBonusStat;
            
            if (modifiers.Count <= 0)
                return;

            modifiers.Sort(new Comparison<Modifier>(CompareModifiers));

            for(var i = 0; i < modifiers.Count; ++i)
            {
                if (modifiers[i].Value != 0)
                {
                    string source;

                    if (i == 0 && damageBonusStat.HasValue)
                    {
                        source = damageBonusStat.Value.ToString();
                    }
                    else if (modifiers[i].Descriptor == ModifierDescriptor.Enhancement && weapon != null)
                    {
                        const string plusPattern = @"\s+\+\d+";
                        var regex = new Regex(plusPattern);
                        source = regex.Replace(weapon.Blueprint.Name, "");
                    }
                    else
                    {
                        source = modifiers[i].Fact?.GetName();
                    }


                    sb.AppendBonus(modifiers[i].Value, source, modifiers[i].Descriptor);
                }
            }
        }

        public static string GetName(this EntityFact fact) {
            var pascalCase = fact?.Blueprint?.name ?? fact?.GetType().Name ?? "Other";
            pascalCase = pascalCase.Replace("Feature", "").Replace("Buff", "");
            var returnString = pascalCase[0].ToString();

            for (var i = 1; i < pascalCase.Length; ++i)
            {
                if (pascalCase[i].IsUpperCase())
                    returnString += " ";

                returnString += pascalCase[i];
            }

            return returnString;
        }

        public static bool IsUpperCase(this char character)
        {
            return character <= 90 && character >= 65;
        }
    }

    [HarmonyPatch(typeof(DamageLogMessage), nameof(DamageLogMessage.AppendDamageDetails))]
    class DamageLogPatch
    {
        static void Postfix(StringBuilder sb, RuleDealDamage rule)
        {
            int totalBonus = rule.ResultList.Sum(damageValue => damageValue.Source.Modifiers.Sum(m => m.Value));
            if (rule != null && totalBonus > 0)
            {
                sb.Append('\n');
                sb.Append($"<b>Damage bonus: {UIConsts.GetValueWithSign(totalBonus)}</b>\n");
                sb.AppendDamageModifiersBreakdown(rule);
            }
        }
    }
}