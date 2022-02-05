namespace WOTRDamageBreakdown.Extensions
{
    public static class StringExtensions
    {
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
}
