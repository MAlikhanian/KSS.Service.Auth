using System.Globalization;
using System.Numerics;

namespace KSS.Helper
{
    public static class Extension
    {
        public static DateTime ToGregorian(this string date)
        {
            var splitedParts = date.Split('/');

            return new DateTime(int.Parse(splitedParts[0]), int.Parse(splitedParts[1]), int.Parse(splitedParts[2]), new PersianCalendar());
        }
        public static string ToPersian(this DateTime value, bool withTime = false)
        {
            return PersianDate(value, withTime);
        }
        public static string ToPersian(this DateTime? value, bool withTime = false, bool emptyMassage = false)
        {
            if (value.HasValue)
            {
                return PersianDate(value.Value, withTime);
            }
            else
                return emptyMassage ? "فاقد اطلاعات" : null;
        }
        public static string PersianDate(DateTime value, bool withTime)
        {
            PersianCalendar persianCalendar = new();

            int year = persianCalendar.GetYear(value);
            int month = persianCalendar.GetMonth(value);
            int day = persianCalendar.GetDayOfMonth(value);
            int hour = persianCalendar.GetHour(value);
            int minute = persianCalendar.GetMinute(value);
            int second = persianCalendar.GetSecond(value);

            if (!withTime)
                return $"{year:0000}/{month:00}/{day:00}";
            else
                return $"{year:0000}/{month:00}/{day:00}-{hour:00}:{minute:00}:{second:00}";

        }
        public static string SafePersianString(this string value)
        {
            return string.IsNullOrEmpty(value) ? value : value.Replace("ی", "ي");
        }
        public static string GetPersianDayOfWeekName_Number(string dayName, bool name)
        {
            if (name)
            {
                return dayName switch
                {
                    "Saturday" => "شنبه",
                    "Sunday" => "یکشنبه",
                    "Monday" => "دوشنبه",
                    "Tuesday" => "سه شنبه",
                    "Wednesday" => "چهار شنبه",
                    "Thursday" => "پنجشنبه",
                    "Friday" => "جمعه",
                    _ => "",
                };
            }
            else
            {
                return dayName switch
                {
                    "Saturday" => "1",
                    "Sunday" => "2",
                    "Monday" => "3",
                    "Tuesday" => "4",
                    "Wednesday" => "5",
                    "Thursday" => "6",
                    "Friday" => "7",
                    _ => "",
                };
            }
        }
        public static string GetPersianMonthName(this byte pMonth)
        {
            return pMonth switch
            {
                1 => "فروردین",
                2 => "اردیبهشت",
                3 => "خرداد",
                4 => "تیر",
                5 => "مرداد",
                6 => "شهریور",
                7 => "مهر",
                8 => "آبان",
                9 => "آذر",
                10 => "دی",
                11 => "بهمن",
                12 => "اسفند",
                _ => "",
            };
        }
        public static bool TimeInterferenceCheck(short startTimeA, short endTimeA, short startTimeB, short endTimeB)
        {
            bool check = false;

            if (startTimeB > startTimeA && startTimeB < endTimeA) check = true;
            if (startTimeB < startTimeA && endTimeB > startTimeA) check = true;
            if (startTimeA == startTimeB || endTimeA == endTimeB) check = true;

            return check;
        }
        public static DateTime YearStart(this short value)
        {
            string yearStartPDate = $"{value}/01/01";

            return yearStartPDate.ToGregorian();
        }
        public static DateTime YearEnd(this short value)
        {
            PersianCalendar pc = new();

            string yearEndPDate = $"{value}/12/{pc.GetDaysInMonth(value, 12)}";

            return yearEndPDate.ToGregorian();
        }
        public static string AddSeparatorToNumber(this decimal number)
        {
            return number.ToString("#,0");
        }
        public static string ConvertNumberLetter(this decimal number)
        {
            string finalText = string.Empty;

            int length = 0;

            if (Convert.ToInt64(number) < 10) length = 1;
            else if (Convert.ToInt64(number) < 100) length = 2;
            else if (Convert.ToInt64(number) < 1000) length = 3;
            else if (Convert.ToInt64(number) < 10000) length = 4;
            else if (Convert.ToInt64(number) < 100000) length = 5;
            else if (Convert.ToInt64(number) < 1000000) length = 6;
            else if (Convert.ToInt64(number) < 10000000) length = 7;
            else if (Convert.ToInt64(number) < 100000000) length = 8;
            else if (Convert.ToInt64(number) < 1000000000) length = 9;
            else if (Convert.ToInt64(number) < 10000000000) length = 10;
            else if (Convert.ToInt64(number) < 100000000000) length = 11;
            else if (Convert.ToInt64(number) <= 999999999999) length = 12;

            string strNumber = Convert.ToInt64(number).ToString();

            string section1 = string.Empty, section2 = string.Empty, section3 = string.Empty, section4 = string.Empty;

            switch (length)
            {

                case 1: section1 = strNumber[..1]; break;

                case 2: section1 = strNumber[..2]; break;

                case 3: section1 = strNumber[..3]; break;

                case 4:

                    section1 = strNumber[..1];
                    section2 = strNumber.Substring(1, 3);

                    break;

                case 5:

                    section1 = strNumber[..2];
                    section2 = strNumber.Substring(2, 3);

                    break;

                case 6:

                    section1 = strNumber[..3];
                    section2 = strNumber.Substring(3, 3);

                    break;

                case 7:

                    section1 = strNumber[..1];
                    section2 = strNumber.Substring(1, 3);
                    section3 = strNumber.Substring(4, 3);

                    break;

                case 8:

                    section1 = strNumber[..2];
                    section2 = strNumber.Substring(2, 3);
                    section3 = strNumber.Substring(5, 3);

                    break;

                case 9:

                    section1 = strNumber[..3];
                    section2 = strNumber.Substring(3, 3);
                    section3 = strNumber.Substring(6, 3);

                    break;

                case 10:

                    section1 = strNumber[..1];
                    section2 = strNumber.Substring(1, 3);
                    section3 = strNumber.Substring(4, 3);
                    section4 = strNumber.Substring(7, 3);

                    break;

                case 11:

                    section1 = strNumber[..2];
                    section2 = strNumber.Substring(2, 3);
                    section3 = strNumber.Substring(5, 3);
                    section4 = strNumber.Substring(8, 3);

                    break;

                case 12:

                    section1 = strNumber[..3];
                    section2 = strNumber.Substring(3, 3);
                    section3 = strNumber.Substring(6, 3);
                    section4 = strNumber.Substring(9, 3);

                    break;
            }

            //1 to 3 digit
            if (length <= 3)
                finalText = ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section1));
            //4 to 6 digit
            else if (length <= 6)
            {
                finalText = ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section1));

                finalText += " هزار ";

                if (section2 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section2));
                }
            }
            //7 to 9 digit
            else if (length <= 9)
            {
                finalText = ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section1));

                finalText += " میلیون ";

                if (section2 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section2));

                    finalText += " هزار ";
                }

                if (section3 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section3));
                }
            }
            //10 to 12 digit
            else if (length <= 12)
            {
                finalText = ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section1));

                finalText += " میلیارد ";

                if (section2 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section2));

                    finalText += " میلیون ";
                }

                if (section3 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section3));

                    finalText += " هزار ";
                }

                if (section4 != "000")
                {
                    finalText += " و ";

                    finalText += ConvertUpThreeDigitNumberToLetter(Convert.ToInt32(section4));
                }
            }

            return finalText;
        }
        public static string ConvertUpThreeDigitNumberToLetter(int number)
        {
            string finalText = string.Empty;

            string strNumber = Convert.ToInt64(number).ToString();

            if (strNumber.Length == 1) strNumber = "00" + strNumber;
            if (strNumber.Length == 2) strNumber = "0" + strNumber;

            string first = "0", second = "0", third = "0";

            first = strNumber.Substring(0, 1);
            second = strNumber.Substring(1, 1);
            third = strNumber.Substring(2, 1);

            switch (first)
            {
                case "1": finalText += "یکصد"; break;
                case "2": finalText += "دویست"; break;
                case "3": finalText += "سیصد"; break;
                case "4": finalText += "چهارصد"; break;
                case "5": finalText += "پانصد"; break;
                case "6": finalText += "ششصد"; break;
                case "7": finalText += "هفتصد"; break;
                case "8": finalText += "هشتصد"; break;
                case "9": finalText += "نهصد"; break;
            }

            if (first != "0" && (second != "0" || third != "0")) finalText += " و ";

            if (second == "1")
            {
                switch (third)
                {
                    case "0": finalText += "ده"; break;
                    case "1": finalText += "یازده"; break;
                    case "2": finalText += "دوازده"; break;
                    case "3": finalText += "سیزده"; break;
                    case "4": finalText += "چهارده"; break;
                    case "5": finalText += "پانزده"; break;
                    case "6": finalText += "شانزده"; break;
                    case "7": finalText += "هفده"; break;
                    case "8": finalText += "هجده"; break;
                    case "9": finalText += "نوزده"; break;
                }
            }
            else
            {
                switch (second)
                {
                    case "2": finalText += "بیست"; break;
                    case "3": finalText += "سی"; break;
                    case "4": finalText += "چهل"; break;
                    case "5": finalText += "پنجاه"; break;
                    case "6": finalText += "شصت"; break;
                    case "7": finalText += "هفتاد"; break;
                    case "8": finalText += "هشتاد"; break;
                    case "9": finalText += "نود"; break;
                }

                if (third != "0" && second != "0") finalText += " و ";

                switch (third)
                {
                    case "1": finalText += "یک"; break;
                    case "2": finalText += "دو"; break;
                    case "3": finalText += "سه"; break;
                    case "4": finalText += "چهار"; break;
                    case "5": finalText += "پنج"; break;
                    case "6": finalText += "شش"; break;
                    case "7": finalText += "هفت"; break;
                    case "8": finalText += "هشت"; break;
                    case "9": finalText += "نه"; break;
                }
            }

            return finalText;
        }
        public static string GetHourFromTotalMinute(this int totalMinute)
        {
            bool negative = false;

            if (totalMinute < 0) { negative = true; totalMinute *= -1; }

            int intH = totalMinute / 60; string strH = intH.ToString(); if (strH.Length == 1) { strH = "0" + strH; }
            int intM = totalMinute % 60; string strM = intM.ToString(); if (strM.Length == 1) { strM = "0" + strM; }

            string hourFromTotalMinute = strH + ":" + strM;

            if (negative) { hourFromTotalMinute += "-"; }

            return hourFromTotalMinute;
        }

        public static int GetTotalMinuteFromHour(this string hour)
        {
            int totalMinutes = 0;

            if (!string.IsNullOrEmpty(hour))
            {
                string[] parts = hour.Split(':');
                int hours = Convert.ToInt32(parts[0]);
                int minutes = Convert.ToInt32(parts[1]);

                totalMinutes = hours * 60 + minutes;
            }

            return totalMinutes;
        }
    }
}
