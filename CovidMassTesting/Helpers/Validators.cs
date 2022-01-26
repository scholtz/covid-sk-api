using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// Validator helpers
    /// </summary>
    public static class Validators
    {
        /// <summary>
        /// Checks if email is valid
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public static bool IsValidEmail(this string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Format phone to slovak standard
        /// 
        /// 0800 123 456 convers to +421800123456
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static string FormatPhone(this string number)
        {
            if (number == null) number = "";
            number = number.Replace(" ", "");
            number = number.Replace("\t", "");
            if (number.StartsWith("00", true, CultureInfo.InvariantCulture)) number = "+" + number[2..];
            if (number.StartsWith("0", true, CultureInfo.InvariantCulture)) number = "+421" + number[1..];
            return number;
        }/// <summary>
         /// Validates the phone number +421800123456
         /// </summary>
         /// <param name="number"></param>
         /// <returns></returns>
        public static bool IsValidPhoneNumber(this string number)
        {
            return Regex.Match(number, @"^(\+[0-9]{11}[0-9]?)$").Success;
        }
    }
}
