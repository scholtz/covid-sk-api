using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CovidMassTesting.Helpers
{
    /// <summary>
    /// Text helper
    /// </summary>
    public static class Text
    {
        /// <summary>
        /// https://www.dotnetportal.cz/blogy/4/Tomas-Jecha/663/NET-Tip-6-Ciste-odstraneni-diakritiky
        /// </summary>
        /// <param name="Text"></param>
        /// <returns></returns>
        public static string RemoveDiacritism(string Text)
        {
            string stringFormD = Text.Normalize(System.Text.NormalizationForm.FormD);
            System.Text.StringBuilder retVal = new System.Text.StringBuilder();
            for (int index = 0; index < stringFormD.Length; index++)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stringFormD[index]) != System.Globalization.UnicodeCategory.NonSpacingMark)
                    retVal.Append(stringFormD[index]);
            }
            return retVal.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }


    }
}
