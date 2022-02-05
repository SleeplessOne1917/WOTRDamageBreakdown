using HarmonyLib;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.RuleSystem.Rules.Damage;
using System;

namespace WOTRDamageBreakdown.Patches
{
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
