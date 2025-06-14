﻿using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace APiDocsToHtml
{
    /// <summary>
    /// Final html export - HTML conversion - here the "magic" happens.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public static class APiHTMLExport
    {
        // The params below must exist in the template HTML document
        private const string HTML_MACRO_HEAD = "|PAGE_HEAD|";
        private const string HTML_MACRO_TITLE = "|DOCS_TITLE|";
        private const string HTML_MACRO_CATEGORIES = "<!--CATEGORIES-->";
        private const string HTML_MACRO_BODYCONTAINER = "<!--CONTAINER-->";

        // Exporter attributes (Currently only one - space)
        private const string ATTRIBUTE_MACRO_SPACE = "Space";

        // Exporter common macros
        private const string COMMON_MACRO_SHORTCUT_END = "</c>";

        /// <summary>
        /// The class represents a specific code style that the base HTML style contains. Each element has a shortcut macro for internal use and a regex match-up pattern
        /// </summary>
        public sealed class CodeHTMLStyles
        {
            public string styleClassName;
            public string styleShortcutMacroStart;
            public string[] catchupPatterns;
            public bool isCommentType;
        }

        // All the expressions have been made just for my personal purposes [C#] (might be updated in the future with more expressions if needed)
        private static readonly CodeHTMLStyles[] registeredCodeHTMLStyles = new CodeHTMLStyles[]
        {
            // Code-comments
            new CodeHTMLStyles()
            {
                 styleClassName="CodeComment",
                 styleShortcutMacroStart="<cc>",
                 isCommentType = true
            },

            // Code-types
            new CodeHTMLStyles()
            {
                 styleClassName="CodeType",
                 styleShortcutMacroStart="<ct>",
                 catchupPatterns = new string[]
                 {
                    // Attributes (Require component & Space)
                    @"(RequireComponent|CustomEditor|Range(?=(\(|\s+?\()))|(?<=\[)(Space|CanEditMultipleObjects|SerializeField)(?=\])",
                    // Regular classes
                    @"(?<!\w)(Camera|MonoBehaviour|Material|MeshFilter|List|Mathf|Light|Color|Object|Collider|GameObject|Renderer|Mesh|AudioClip)(?!\w)",
                    // Input
                    @"(?<!\w)(Input|KeyCode)(?!\w)",
                    // Physics
                    @"(?<!\w)(Ray|Physics|RaycastHit)(?!\w)",
                    // Other
                    @"(?<!\w)(Debug|Time|Transform|Texture2D|IReadOnlyList)(?!\w)"
                 }
            },

            // Code-keywords
            new CodeHTMLStyles()
            {
                 styleClassName="CodeKeyword",
                 styleShortcutMacroStart="<ck>",
                 catchupPatterns = new string[]
                 {
                    // Essential keywords
                    @"(?<!\w)(using|get|async|virtual|set|public|private|sealed|const|static|abstract|protected|override|base|new|void|class|return|out|in|typeof|if|while|else|for|foreach|continue|null)(?!\w)",
                    // Datatypes
                    @"(?<!\w)(Vector3|Vector2|Quaternion|true|false|bool|int|float|string|var)(?!\w)",
                    // Shading language
                    @"(?<!\w)(float2|float3|float4|half|half2|half3|half4|float2x2|float3x3|float4x4|half2x2|half3x3|half4x4)(?!\w)"
                 }
            },

            // Code-string contents & numerics (floats, doubles, ints etc)
            new CodeHTMLStyles()
            {
                 styleClassName="CodeString",
                 styleShortcutMacroStart="<cs>",
                 catchupPatterns = new string[]
                 {
                    // Strings and numerics
                    "(\"((\\[^\n]|[^\"\"\n])*)\")|((?<!\\w)([-+]?[0-9]*\\.?[0-9]+)f?)"
                 }
            }
        };
        private const string RegisteredCodeComment = "//"; // Lines that start with this string will be ignored and marked as a comment style

        public static IReadOnlyCollection<CodeHTMLStyles> RegisteredCodeHTMLStylesReadonly => registeredCodeHTMLStyles;

        /// <summary>
        /// Get a comment element from the registered html-code-styles
        /// </summary>
        /// <returns>Returns an element from the list that is marked as IsCommentType (Might return null if not found)</returns>
        private static CodeHTMLStyles GetCommentCodeHtmlStyle(this CodeHTMLStyles[] ctlms)
        {
            foreach (var t in ctlms)
                if (t.isCommentType)
                    return t;
            return null;
        }

        /// <summary>
        /// Convert a shortcut macro to the html-friendly existing css class
        /// </summary>
        /// <returns>Returns a converted css style (if exists)</returns>
        private static string ConvertFromShortcutToCompleteStyle(this string str, CodeHTMLStyles[] customStyles = null)
        {
            foreach(CodeHTMLStyles dhtmls in registeredCodeHTMLStyles)
                str = str.Replace(dhtmls.styleShortcutMacroStart, $"<span class={dhtmls.styleClassName}>").Replace(COMMON_MACRO_SHORTCUT_END, "</span>");
            if (customStyles != null && customStyles.Length > 0)
                foreach (CodeHTMLStyles dhtmls in customStyles)
                    str = str.Replace(dhtmls.styleShortcutMacroStart, $"<span class={dhtmls.styleClassName}>").Replace(COMMON_MACRO_SHORTCUT_END, "</span>");
            return str;
        }

        /// <summary>
        /// Export a specific list of categories to the complete HTML structure
        /// </summary>
        /// <param name="apiCategories">Input list of modifier APi categories with all its elements & content</param>
        /// <param name="documentTitle">Document title - will appear as a header in the HTML</param>
        /// <param name="targetDirectory">Target directory where the export will happen (must exists)</param>
        /// <param name="htmlTemplatePath">HTML template path</param>
        /// <param name="cssTemplatePath">CSS style template path</param>
        /// <param name="exception">Exception message if the export is unsuccessful</param>
        /// <param name="customCodeHTMLStyles">Custom code in HTML styles with regex patterns (leave it null if unused)</param>
        /// <returns>Returns true if the export was successful. Otherwise listen to the exception output</returns>
        public static bool ApiExportToHtml(in List<APiCategory> apiCategories, in string documentTitle, in string targetDirectory,
            in string htmlTemplatePath, in string cssTemplatePath, out string exception, in bool autoFormatCodeStyle = true,
            in CodeHTMLStyles[] customCodeHTMLStyles = null)
        {
            exception = "";
            if (!Directory.Exists(targetDirectory))
            {
                exception = $"Target directory '{targetDirectory}' doesn't exist";
                return false;
            }
            if (!File.Exists(htmlTemplatePath))
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't exist";
                return false;
            }
            if (!File.Exists(cssTemplatePath))
            {
                exception = $"Target css-style template file '{cssTemplatePath}' doesn't exist";
                return false;
            }

            List<string> htmlContent = new List<string>(File.ReadAllLines(htmlTemplatePath));
            apiCategories.Reverse();

            // Looking for certain macros in the HTML template file
            int headIndex = -1;
            int titleIndex = -1;
            int categoryIndex = -1;
            int containerIndex = -1;

            for (int i = 0; i < htmlContent.Count; i++)
            {
                string l = htmlContent[i].Trim();
                if (headIndex == -1 && l.Contains(HTML_MACRO_HEAD))
                    headIndex = i;
                if (titleIndex == -1 && l.Contains(HTML_MACRO_TITLE))
                    titleIndex = i;
                if (categoryIndex == -1 && l == HTML_MACRO_CATEGORIES)
                    categoryIndex = i + 1;
                if (containerIndex == -1 && l == HTML_MACRO_BODYCONTAINER)
                    containerIndex = i + 1;
            }

            // The html template file must contain category and container macros for proper conversion
            if (categoryIndex == -1)
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't contain a " +
                    $"'{HTML_MACRO_CATEGORIES}' macro for insertion of categories";
                return false;
            }
            if (headIndex == -1)
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't contain a " +
                    $"'{HTML_MACRO_HEAD}' for head title";
                return false;
            }
            if (titleIndex == -1)
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't contain a " +
                    $"'{HTML_MACRO_TITLE}' for docs title";
                return false;
            }
            if (containerIndex == -1)
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't contain a " +
                    $"'{HTML_MACRO_BODYCONTAINER}' macro for insertion of container";
                return false;
            }

            List<APiCategory> sortedCategories = new List<APiCategory>();

            // Getting all the available attributes from the category list
            List<string> availableAttributes = new List<string>();
            foreach(APiCategory cat in apiCategories)
            {
                if (cat.HasAttribute() && !availableAttributes.Contains(cat.parentAttribute))
                    availableAttributes.Add(cat.parentAttribute);
            }

            // Declare a sorted attribute-list
            List<(string, int)> sortedAttributes = new List<(string, int)>();
            foreach (string att in availableAttributes)
            {
                bool foundBase = false;

                // If its just a built-in attribute, ignore the rest
                if (!att.Equals(ATTRIBUTE_MACRO_SPACE))
                {
                    foreach (APiCategory cat in apiCategories)
                    {
                        if (cat.idText.Length <= 1)
                            continue;
                        // All the categories that will indicate to the root category for other categories have to contain a sorting number (from 0 to 9)
                        string catPrior = cat.idText.Substring(0, 1);
                        if (catPrior.CanConvertToInt(out int v))
                        {
                            // Remove priority number (if its a base category for the attribute) and continue
                            string catName = cat.idText.Substring(1, cat.idText.Length - 1);
                            if (catName == att)
                            {
                                cat.idText = cat.idText.Substring(1, cat.idText.Length - 1);
                                sortedAttributes.Add((att, v));
                                foundBase = true;
                                break;
                            }
                        }
                    }
                }
                // Add default attribute without a sorting number (always the top 9)
                if(!foundBase)
                    sortedAttributes.Add((att, 9));
            }

            sortedAttributes.Sort((a, b) => a.Item2.CompareTo(b.Item2));

            if (sortedAttributes.Count > 0)
            {
                availableAttributes.Clear();
                for (int i = 0; i < sortedAttributes.Count; i++)
                    availableAttributes.Add(sortedAttributes[i].Item1);
            }

            // Adjust finally sorted list of categories
            if (availableAttributes.Count > 0)
            {
                // Search through the available attributes
                foreach (string attbase in availableAttributes)
                {
                    // Search through all the categories
                    foreach (APiCategory cat in apiCategories)
                    {
                        // Check conditions (Space is the only attribute available, other attributes will indicate to the nested inheritance in navbar)
                        if (!sortedCategories.Contains(cat) && (cat.idText == attbase || !cat.HasAttribute() || cat.parentAttribute.Equals(ATTRIBUTE_MACRO_SPACE)))
                        {
                            bool isABase = false;
                            foreach (APiCategory cat2 in apiCategories)
                            {
                                // Check if this is the base element for inheritance
                                if(cat2.parentAttribute == cat.idText && cat.idText != attbase && cat.parentAttribute.Equals(ATTRIBUTE_MACRO_SPACE))
                                {
                                    isABase = true;
                                    break;
                                }
                            }
                            if (isABase)
                                continue;

                            sortedCategories.Add(cat);
                        }
                    }

                    if (attbase.Equals(ATTRIBUTE_MACRO_SPACE))
                        continue;

                    // Add other unsorted categories
                    foreach (APiCategory cat in apiCategories)
                    {
                        if (cat.parentAttribute == attbase)
                            sortedCategories.Add(cat);
                    }
                }
            }
            else
                sortedCategories.AddRange(apiCategories);

            sortedCategories.Reverse();

            // Inserting the categories and their content to the html content
            for (int i = 0; i < apiCategories.Count; i++)
            {
                // Creating a copy of the html template for the particular category file
                List<string> copyHtmlContent = new List<string>(htmlContent);
                APiCategory apiCategory = apiCategories[i];
                // Replace the head title with the category title
                copyHtmlContent[headIndex] = copyHtmlContent[headIndex].Replace(HTML_MACRO_HEAD, apiCategory.idText);
                // Replace the docs title with the custom title
                copyHtmlContent[titleIndex] = copyHtmlContent[titleIndex].Replace(HTML_MACRO_TITLE, documentTitle);

                // Writing all the categories to the side-bar navigator
                int categoryIndexOffset = 0;
                foreach(APiCategory sorted in sortedCategories)
                {
                    string isBreakAttribute = sorted.parentAttribute == ATTRIBUTE_MACRO_SPACE ? "<br>" : "";
                    string isSub = sorted.HasAttribute() && sorted.parentAttribute != ATTRIBUTE_MACRO_SPACE ? "&emsp;" : "";

                    string isReadOnly = sorted.ReadonlyCategory ? isSub : "<li>" + isSub + "<a href=\"" + sorted.idText.RemoveSpacesAndTrim() + ".html\">";
                    string isReadOnlyEnd = sorted.ReadonlyCategory ? "" : "</a></li>";
                    copyHtmlContent.Insert(categoryIndex, isBreakAttribute + isReadOnly + sorted.idText + isReadOnlyEnd);
                    categoryIndexOffset++;
                }

                // Readonly categories do not have elements
                if (apiCategories[i].ReadonlyCategory)
                    continue;

                // Writing the page content
                for (int x = 0; x < apiCategory.pageElements.Count; x++)
                {
                    var element = apiCategory.pageElements[x];
                    int elementIndexInsertion = containerIndex + categoryIndexOffset + x;
                    if(elementIndexInsertion >= copyHtmlContent.Count)
                    {
                        exception = $"Index of element insertion in {apiCategory.idText} category is higher than expected {elementIndexInsertion}vs{copyHtmlContent.Count}. Please fix your source!";
                        return false;
                    }
                    string idText = element.idText;
                    if (element.idClass == "Code" && autoFormatCodeStyle)
                        idText = ApiConvertToHtmlCodeFormat(idText, customCodeHTMLStyles);
                    copyHtmlContent.Insert(elementIndexInsertion, $"<div class=\"{element.idClass}\">{idText.ConvertFromShortcutToCompleteStyle(customCodeHTMLStyles).MakeHTMLFriendly()}</div>");
                }

                string htmlPath = $"{targetDirectory}{APiBase.STREAM_Separator}{apiCategory.idText.RemoveSpacesAndTrim()}.html";

                try
                {
                    File.Create(htmlPath).Dispose();
                    File.WriteAllLines(htmlPath, copyHtmlContent);
                }
                catch(IOException ioe)
                {
                    exception = ioe.Message;
                    return false;
                }
            }

            try
            {
                File.Create(targetDirectory + APiBase.STREAM_Separator + "style.css").Dispose();
                File.WriteAllText(targetDirectory + APiBase.STREAM_Separator + "style.css", File.ReadAllText(cssTemplatePath));
            }
            catch (IOException ioe)
            {
                exception = ioe.Message;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Conversion from a plain text to the specific APiDocs html-code-format. Not a perfect solution, but works for my purpose. Great time saver!
        /// </summary>
        /// <param name="content">Text content for the expected conversion</param>
        /// <param name="customCodeHTMLStyles">Custom code-styles, leave empty if not used</param>
        /// <returns>Returns converted text content to the html-code style</returns>
        private static string ApiConvertToHtmlCodeFormat(in string content, in CodeHTMLStyles[] customCodeHTMLStyles = null)
        {            
            string[] newTextLines = content.Split('\n');
            int i = 0;
            string currLine;
            string commentPart;

            foreach (string line in newTextLines)
            {
                commentPart = "";
                currLine = line;

                currLine = currLine.Replace("<", "&lt;").Replace(">", "&gt;");

                // Check for comments (specified for C#)
                if (currLine.Contains(RegisteredCodeComment))
                {
                    var commentType = GetCommentCodeHtmlStyle(registeredCodeHTMLStyles);
                    int cmntindx = currLine.IndexOf(RegisteredCodeComment);
                    if(cmntindx == 0)
                    {
                        currLine = currLine.Replace(currLine, commentType.styleShortcutMacroStart + currLine + COMMON_MACRO_SHORTCUT_END);
                        goto cont;
                    }
                    else
                    {
                        commentPart = commentType.styleShortcutMacroStart + currLine.Substring(cmntindx, currLine.Length - cmntindx) + COMMON_MACRO_SHORTCUT_END;
                        currLine = currLine.Substring(0, cmntindx);
                    }
                }

                ListThroughTheCode(registeredCodeHTMLStyles);

                if(customCodeHTMLStyles != null && customCodeHTMLStyles.Length > 0)
                    ListThroughTheCode(customCodeHTMLStyles);

                cont:
                newTextLines[i] = currLine + commentPart;
                i++;
            }

            StringBuilder sb = new StringBuilder();

            for (int x = 0; x < newTextLines.Length; x++)
                sb.Append(newTextLines[x] + System.Environment.NewLine);

            return sb.ToString();

            void ListThroughTheCode(CodeHTMLStyles[] listChtmls)
            {
                foreach (CodeHTMLStyles dhtmls in listChtmls)
                {
                    if (dhtmls.catchupPatterns != null)
                        foreach (string p in dhtmls.catchupPatterns)
                            DoMatches(p, dhtmls.styleShortcutMacroStart, COMMON_MACRO_SHORTCUT_END);
                }
            }

            void DoMatches(string matchPattern, string codeStartingMacro, string codeEndingMacro)
                => currLine = Regex.Replace(currLine, matchPattern, match => codeStartingMacro + match.Value + codeEndingMacro);
        }
    }
}