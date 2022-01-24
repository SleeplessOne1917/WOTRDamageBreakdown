using Kingmaker.EntitySystem;
using Kingmaker.Enums;
using Kingmaker.RuleSystem.Rules;
using Kingmaker.RuleSystem.Rules.Damage;
using Kingmaker.UI.Common;
using System.Reflection;
using System.Text;
using UnityModManagerNet;

namespace WOTRDamageBreakdown;

public class Main
{
    static bool Load(UnityModManager.ModEntry modEntry)
    {
        var harmony = new HarmonyLib.Harmony(modEntry.Info.Id);
        harmony.PatchAll(Assembly.GetExecutingAssembly());

        return true;
    }
}

public static class DamageModifiersBreakdown
{
    public static void AddDamageRule(RuleDealDamage rule)
    {
        StatModifiersBreakdown.AddBonusSources(rule.AllBonuses);
        StatModifiersBreakdown.AddStatModifiers(rule.Initiator.Stats.AdditionalDamage);
    }

    private static int CompareModifiers(Modifier x, Modifier y)
    {
        return ModifierDescriptorComparer.Instance.Compare(x.Descriptor, y.Descriptor);
    }

    public static void AppendDamageModifiersBreakdown(this StringBuilder sb, List<Modifier> modifiers)
    {
        if (modifiers.Count() <= 0)
            return;

        modifiers.Sort(new Comparison<Modifier>(CompareModifiers));

        foreach (var modifier in modifiers)
        {
            if (modifier.Value != 0)
            {
                var source = modifier.Fact.GetName();
                sb.AppendBonus(modifier.Value, source, modifier.Descriptor);
            }
        }
    }

    public static string GetName(this EntityFact entityFact) => entityFact.Blueprint != null ? entityFact.Blueprint.name : entityFact.GetType().Name;
}

