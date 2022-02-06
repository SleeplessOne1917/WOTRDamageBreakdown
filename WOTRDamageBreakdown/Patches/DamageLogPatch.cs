using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.Utility;
using System.Linq;
using System.Text;
using WOTRDamageBreakdown.Extensions;

namespace WOTRDamageBreakdown.Patches
{
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
            var dice = firstDamage.Dice;
            var isZeroDice = dice.Dice == Kingmaker.RuleSystem.DiceType.Zero || dice.Rolls == 0;

            if (trueTotal != 0 && !isZeroDice)
            {
                sb.AppendLine();
                sb.AppendLine($"<b>Damage bonus: {UIConsts.GetValueWithSign(trueTotal)}</b>");
                sb.AppendDamageModifiersBreakdown(rule, totalBonus, trueTotal);
            }
        }
    }
}
