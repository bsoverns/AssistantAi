using System.Globalization;

namespace AssistantAi.Helpers
{
    public static class TextFormatting
    {
        /// <summary>Title-cases a string using the current culture ("minor" -> "Minor").</summary>
        public static string ToProperCase(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            TextInfo textInfo = CultureInfo.CurrentCulture.TextInfo;
            return textInfo.ToTitleCase(input.ToLower());
        }
    }
}
