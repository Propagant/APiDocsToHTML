using System.Collections.Generic;
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
        private const string HTMLRepMacro_Head = "|PAGE_HEAD|";
        private const string HTMLRepMacro_Title = "|DOCS_TITLE|";
        private const string HTMLMacro_Categories = "<!--CATEGORIES-->";
        private const string HTMLMacro_Container = "<!--CONTAINER-->";

        /// <summary>
        /// Class that represents a specific code-styles that the base html style contain. Each element has a shortcut macro for internal use and a regex-matchup pattern
        /// </summary>
        public class CodeHTMLStyles
        {
            public string styleClassName;
            public string styleShortcutStartingMacro;
            public string styleShortcutEndingMacro;
            public string[] catchupPatterns;
            public bool isCommentType;
        }

        // All the expressions have been made just for my personal purposes [C#] (might be updated in the future with more expressions if needed)
        private static readonly CodeHTMLStyles[] RegisteredCodeHTMLStyles = new CodeHTMLStyles[]
        {
            // Code-comments
            new CodeHTMLStyles()
            {
                 styleClassName="CodeComment",
                 styleShortcutStartingMacro="<cc>",
                 styleShortcutEndingMacro="</c>",
                 isCommentType = true
            },

            // Code-types
            new CodeHTMLStyles()
            {
                 styleClassName="CodeType",
                 styleShortcutStartingMacro="<ct>",
                 styleShortcutEndingMacro="</c>",
                 catchupPatterns = new string[]
                 {
                    // Attributes (Require component & Space)
                    @"(RequireComponent|CustomEditor|Range(?=(\(|\s+?\()))|(?<=\[)(Space|CanEditMultipleObjects|SerializeField)(?=\])",
                    // Regular classes
                    @"(?<!\w)(Camera|MonoBehaviour|MeshFilter|List|Mathf|Object|Collider|GameObject|Renderer|Mesh|AudioClip)(?!\w)",
                    // Input
                    @"(?<!\w)(Input|KeyCode)(?!\w)",
                    // Physics
                    @"(?<!\w)(Ray|Physics|RaycastHit)(?!\w)",
                    // Other
                    @"(?<!\w)(Debug|Time|Transform)(?!\w)"
                 }
            },

            // Code-keywords
            new CodeHTMLStyles()
            {
                 styleClassName="CodeKeyword",
                 styleShortcutStartingMacro="<ck>",
                 styleShortcutEndingMacro="</c>",
                 catchupPatterns = new string[]
                 {
                    // Essential keywords
                    @"(?<!\w)(using|get|async|virtual|set|public|private|sealed|static|abstract|protected|override|base|new|void|class|return|out|in|typeof|if|while|else|for|foreach|continue|null)(?!\w)",
                    // Datatypes
                    @"(?<!\w)(Vector3|Vector2|Quaternion|true|false|bool|int|float|string|var)(?!\w)"
                 }
            },

            // Code-string contents & numerics (floats, doubles, ints etc)
            new CodeHTMLStyles()
            {
                 styleClassName="CodeString",
                 styleShortcutStartingMacro="<cs>",
                 styleShortcutEndingMacro="</c>",
                 catchupPatterns = new string[]
                 {
                     // Strings and numerics
                    "(\"((\\[^\n]|[^\"\"\n])*)\")|((?<!\\w)([-+]?[0-9]*\\.?[0-9]+)f?)"
                 }
            }
        };
        private static readonly string RegisteredCodeComment = "//"; // Lines that start with this string will be ignored and marked as a comment style

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
        private static string ConvertFromShortcutToCompleteStyle(this string str)
        {
            foreach(CodeHTMLStyles dhtmls in RegisteredCodeHTMLStyles)
                str = str.Replace(dhtmls.styleShortcutStartingMacro, $"<span class={dhtmls.styleClassName}>").Replace(dhtmls.styleShortcutEndingMacro,"</span>");
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
        public static bool ApiExportToHtml(List<APiCategory> apiCategories, string documentTitle, string targetDirectory, string htmlTemplatePath, string cssTemplatePath, out string exception, bool autoFormatCodeStyle = true, CodeHTMLStyles[] customCodeHTMLStyles = null)
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
                if (headIndex == -1 && l.Contains(HTMLRepMacro_Head))
                    headIndex = i;
                if (titleIndex == -1 && l.Contains(HTMLRepMacro_Title))
                    titleIndex = i;
                if (categoryIndex == -1 && l == HTMLMacro_Categories)
                    categoryIndex = i + 1;
                if (containerIndex == -1 && l == HTMLMacro_Container)
                    containerIndex = i + 1;
            }

            // The html template file must contain category and container macros for proper conversion
            if(categoryIndex == -1 || containerIndex == -1 || headIndex == -1 || titleIndex == -1)
            {
                exception = $"Target html template file '{htmlTemplatePath}' doesn't contain a " +
                    $"'{HTMLMacro_Categories}' macro for insertion of categories or " +
                    $"'{HTMLMacro_Container}' macro for insertion of container or " +
                    $"'{HTMLRepMacro_Head}' for head title or " +
                    $"'{HTMLRepMacro_Title}' for docs title";
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
                if (att != "Space")
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

            // Sort attributes from top to bottom
            sortedAttributes.Sort((a, b) => a.Item2.CompareTo(b.Item2));

            // Populate available attributes again if possible
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
                        if (!sortedCategories.Contains(cat) && (cat.idText == attbase || !cat.HasAttribute() || cat.parentAttribute == "Space"))
                        {
                            bool isABase = false;
                            foreach (APiCategory cat2 in apiCategories)
                            {
                                // Check if this is the base element for inheritance
                                if(cat2.parentAttribute == cat.idText && cat.idText != attbase && cat.parentAttribute == "Space")
                                {
                                    isABase = true;
                                    break;
                                }
                            }
                            if (isABase) continue;

                            sortedCategories.Add(cat);
                        }
                    }

                    if (attbase == "Space")
                        continue;

                    // Add other unsorted categories
                    foreach (APiCategory cat in apiCategories)
                    {
                        if (cat.parentAttribute == attbase)
                            sortedCategories.Add(cat);
                    }
                }
            }
            else sortedCategories.AddRange(apiCategories);

            // Reverse list
            sortedCategories.Reverse();

            // Inserting the categories and their content to the html content
            for (int i = 0; i < apiCategories.Count; i++)
            {
                // Creating a copy of the html template for the particular category file
                List<string> copyHtmlContent = new List<string>(htmlContent);
                var apic = apiCategories[i];
                // Replace the head title with the category title
                copyHtmlContent[headIndex] = copyHtmlContent[headIndex].Replace(HTMLRepMacro_Head, apic.idText);
                // Replace the docs title with the custom title
                copyHtmlContent[titleIndex] = copyHtmlContent[titleIndex].Replace(HTMLRepMacro_Title, documentTitle);

                // Writing all the categories to the side-bar navigator
                int ac = 0;
                foreach(APiCategory sorted in sortedCategories)
                {
                    string isBreakAttribute = sorted.parentAttribute == "Space" ? "<br>" : "";
                    string isSub = sorted.HasAttribute() && sorted.parentAttribute != "Space" ? "&emsp;" : "";

                    string isReadOnly = sorted.ReadonlyCategory ? isSub : "<li>" + isSub + "<a href=\"" + sorted.idText.RemoveSpacesAndTrim() + ".html\">";
                    string isReadOnlyEnd = sorted.ReadonlyCategory ? "" : "</a></li>";
                    copyHtmlContent.Insert(categoryIndex, isBreakAttribute + isReadOnly + sorted.idText + isReadOnlyEnd);
                    ac++;
                }

                // Readonly categories do not have elements
                if (apiCategories[i].ReadonlyCategory)
                    continue;

                // Writing the page content
                for (int d = 0; d < apic.pageElements.Count; d++)
                {
                    var el = apic.pageElements[d];
                    int indx = containerIndex + ac + d;
                    if(indx >= copyHtmlContent.Count)
                    {
                        exception = $"Index of element insertion in {apic.idText} category is higher than expected {indx}vs{copyHtmlContent.Count}. Please fix your source!";
                        return false;
                    }
                    string idText = el.idText;
                    if (el.idClass == "Code" && autoFormatCodeStyle)
                        idText = ApiConvertToHtmlCodeFormat(idText, customCodeHTMLStyles);
                    copyHtmlContent.Insert(indx, $"<div class=\"{el.idClass}\">{idText.ConvertFromShortcutToCompleteStyle().MakeHTMLFriendly()}</div>");
                }

                // Writing the actual file
                string htmlPath = $"{targetDirectory}{APiBase.STREAM_Separator}{apic.idText.RemoveSpacesAndTrim()}.html";
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
                // Exporting the css style template
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
        private static string ApiConvertToHtmlCodeFormat(string content, CodeHTMLStyles[] customCodeHTMLStyles = null)
        {            
            string[] newTextLines = content.Split('\n');
            int i = 0;
            string currLine;
            Regex regex;
            MatchCollection matches;
            string commentPart;

            foreach (string line in newTextLines)
            {
                commentPart = "";
                currLine = line;

                // Make < and > friendly for HTML
                currLine = currLine.Replace("<", "&lt;").Replace(">", "&gt;");

                // Check for comments (specified for C#)
                if (currLine.Contains(RegisteredCodeComment))
                {
                    var commentType = GetCommentCodeHtmlStyle(RegisteredCodeHTMLStyles);
                    int cmntindx = currLine.IndexOf(RegisteredCodeComment);
                    if(cmntindx == 0)
                    {
                        currLine = currLine.Replace(currLine, commentType.styleShortcutStartingMacro + currLine + commentType.styleShortcutEndingMacro);
                        goto cont;
                    }
                    else
                    {
                        commentPart = commentType.styleShortcutStartingMacro + currLine.Substring(cmntindx, currLine.Length - cmntindx) + commentType.styleShortcutEndingMacro;
                        currLine = currLine.Substring(0, cmntindx);
                    }
                }

                // Go through the all registered code html styles
                ListThroughTheCode(RegisteredCodeHTMLStyles);
                // Check custom code
                if(customCodeHTMLStyles != null)
                    ListThroughTheCode(customCodeHTMLStyles);

                cont:
                newTextLines[i] = currLine + commentPart;
                i++;
            }

            // Create a single string
            StringBuilder sb = new StringBuilder();
            for (int x = 0; x < newTextLines.Length; x++)
                sb.Append(newTextLines[x] + System.Environment.NewLine);

            return sb.ToString();




            // Go through the specific CodeHtmlStyles array and do matches
            void ListThroughTheCode(CodeHTMLStyles[] listChtmls)
            {
                foreach (CodeHTMLStyles dhtmls in listChtmls)
                {
                    if (dhtmls.catchupPatterns != null)
                        foreach (string p in dhtmls.catchupPatterns)
                            DoMatches(p, dhtmls.styleShortcutStartingMacro, dhtmls.styleShortcutEndingMacro);
                }
            }

            // Process specific match iterations
            void DoMatches(string matchPattern, string codeStartingMacro, string codeEndingMacro)
            {
                regex = new Regex(matchPattern);
                matches = regex.Matches(currLine);
                if (matches.Count > 0)
                    foreach (Match m in matches)
                    {
                        if (m.Success)
                            currLine = currLine.Replace(m.Value, codeStartingMacro + m.Value + codeEndingMacro);
                    }
            }
        }
    }
}