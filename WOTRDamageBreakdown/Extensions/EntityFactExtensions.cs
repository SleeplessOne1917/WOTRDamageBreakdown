using Kingmaker.EntitySystem;
using Kingmaker.UnitLogic.Buffs;

namespace WOTRDamageBreakdown.Extensions
{
    public static class EntityFactExtensions
    {
        public static string GetName(this EntityFact fact)
        {
            if (fact is Buff buff)
            {
                return buff.Name; ;
            }

            var pascalCase = fact.Blueprint?.name ?? fact.GetType().Name;
            pascalCase = pascalCase.Remove("Feature").Remove("Buff").Remove("Effect").Remove("Feat").Remove("Enchantment");
            var returnString = pascalCase.SpaceSeparatePascalCase();

            return returnString.Replace(" Of ", " of ").Replace(" The ", " the ");
        }
    }
}
