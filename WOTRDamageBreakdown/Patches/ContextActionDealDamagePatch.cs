using HarmonyLib;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Mechanics.Actions;
using System;
using System.Linq;

namespace WOTRDamageBreakdown.Patches
{
    [HarmonyPatch(typeof(ContextActionDealDamage),
         nameof(ContextActionDealDamage.GetDamageRule),
         new Type[] { typeof(ContextActionDealDamage.DamageInfo), typeof(int) },
         new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out })]
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
}
