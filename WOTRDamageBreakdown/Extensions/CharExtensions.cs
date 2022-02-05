namespace WOTRDamageBreakdown.Extensions
{
    public static class CharExtensions
    {
        public static bool IsUpperCase(this char character)
        {
            return character <= 90 && character >= 65;
        }
    }
}
