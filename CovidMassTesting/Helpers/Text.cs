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
            var stringFormD = Text.Normalize(System.Text.NormalizationForm.FormD);
            var retVal = new System.Text.StringBuilder();
            for (var index = 0; index < stringFormD.Length; index++)
            {
                if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(stringFormD[index]) != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    retVal.Append(stringFormD[index]);
                }
            }
            return retVal.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }
        /// <summary>
        /// Formats the code.. removes slashes and trims
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        public static string FormatBarCode(this string code)
        {
            if (string.IsNullOrEmpty(code)) return code;
            return code
                .ToUpper()
                .Replace("‐", "")//utf slash?
                .Replace("-", "")
                .Replace(" ", "")
                .Trim();
        }
        /// <summary>
        /// Format passport, covid pass and other documents to the standardized form
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static string FormatDocument(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return input?
                .ToUpper()
                .Replace("‐", "")//utf slash?
                .Replace("-", "")
                .Replace(" ", "")
                .Replace("/", "")
                .Trim();
        }
    }
}
