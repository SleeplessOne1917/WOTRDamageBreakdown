using HarmonyLib;
using Kingmaker.Blueprints.Root.Strings.GameLog;
using Kingmaker.EntitySystem.Stats;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UnitLogic.Parts;
using Kingmaker.Utility;
using System;
using System.Collections.Generic;
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
            int totalBonus = firstDamage.TotalBonus;
            var dice = firstDamage.Dice;
            var isZeroDice = dice.Dice == Kingmaker.RuleSystem.DiceType.Zero || dice.Rolls == 0;
            var modifiers = GetModifiers(rule, totalBonus, firstDamage).ToList();

            if (modifiers.Count > 0 && !isZeroDice)
            {
                sb.AppendLine();
                sb.AppendLine($"<b>Damage bonus: {UIConsts.GetValueWithSign(totalBonus)}</b>");
                sb.AppendDamageModifiersBreakdown(rule, modifiers);
            }
        }

        private static IEnumerable<Modifier> GetModifiers(RuleDealDamage rule, int trueTotal, BaseDamage damage)
        {
            var modifiers = damage.Modifiers;
            var totalBonus = modifiers.Sum(m => m.Value);

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
                var weapon = rule.DamageBundle.Weapon;
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

            modifiers.Sort(new Comparison<Modifier>(CompareModifiers));

            return modifiers;
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
