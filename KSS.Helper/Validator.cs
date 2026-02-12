using System.Globalization;

namespace KSS.Helper
{
    public static class Validator
    {
        public static bool IsValidPersianDate(string inputDate)
        {
            string[] formats = { "yyyy/MM/dd", "yyyy/M/d", "yy/MM/dd", "yy/M/d" };

            if (DateTime.TryParseExact(inputDate, formats, new CultureInfo("fa-IR"), DateTimeStyles.None, out _))
                return true;

            return false;
        }
    }
}