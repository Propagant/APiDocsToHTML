using System;

namespace APiDocsToHtml
{
    /// <summary>
    /// Custom extensions for internal purpose.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public static class APiExtensions
    {
        /// <summary>
        /// Makes the entry string HTML friendly! Replaces tabs, new lines etc to the html styles
        /// </summary>
        public static string MakeHTMLFriendly(this string str)
        {
            return str.Replace(Environment.NewLine, "<br>").Replace("\n", "<br>").Replace("\t", "&emsp;").Replace("    ","&emsp;");
        }

        /// <summary>
        /// Replace spaces in a current string with underscore(_) and trim in both sides
        /// </summary>
        public static string RemoveSpacesAndTrim(this string str)
        {
            return str.Replace(" ", "_").Trim();
        }

        /// <summary>
        /// Returns the category name only without any attributes
        /// </summary>
        public static string GetCategoryNameOnly(this string str, out bool hasAttributes)
        {
            hasAttributes = false;
            if (!str.Contains("|"))
                return str;
            hasAttributes = true;
            return str.Substring(0, str.IndexOf('|')).Trim();
        }

        /// <summary>
        /// Returns a complete string of category attributes
        /// </summary>
        public static string GetCategoryAttributes(this string categoryUncompiledName)
        {
            string catName = categoryUncompiledName.GetCategoryNameOnly(out bool hasAtts);

            if (!hasAtts) return string.Empty;
            else return categoryUncompiledName.Replace(catName, "").Replace("|", "").Trim();
        }

        /// <summary>
        /// Convert current string to integer
        /// </summary>
        public static int ConvertToInt(this string str, out bool success)
        {
            success = CanConvertToInt(str, out int t);
            return t;
        }

        /// <summary>
        /// Can the input string be converted to integer?
        /// </summary>
        public static bool CanConvertToInt(this string str, out int result)
        {
            bool res = int.TryParse(str, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int t);
            result = t;
            return res;
        }
    }

    /// <summary>
    /// Custom nested data for internal purpose.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public abstract class APiData
    {
        /// <summary>
        /// Color structure in range of 0-255
        /// </summary>
        public struct Color
        {
            public int r;
            public int g;
            public int b;
            public Color(int r, int g, int b)
            {
                this.r = Math.Max(0, Math.Min(255, r));
                this.g = Math.Max(0, Math.Min(255, g));
                this.b = Math.Max(0, Math.Min(255, b));
            }
        }
    }
}
