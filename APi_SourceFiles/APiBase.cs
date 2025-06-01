using System.Collections.Generic;
using System.IO;

namespace APiDocsToHtml
{
    /// <summary>
    /// Main base class for the APi-documentation conversion to HTML documents.
    /// Use the class constructor for proper initialization. Please learn more on github about syntax and its use https://github.com/Propagant/APiDocsToHTML/blob/main/README.md
    /// Version 1.1.0 - May 2023.
    /// Originally written in August 2022, by Matej Vanco, https://matejvanco.com
    /// </summary>
    public sealed class APiBase : APiData
    {
        /// <summary>
        /// Default initialized path for the APi-documents
        /// </summary>
        public string DefaultPath { get => _defaultPath; private set => _defaultPath = value; }
        private string _defaultPath;

        private readonly bool initialized = false;

        /// <summary>
        /// Base setup constructor for the APi document
        /// </summary>
        /// <param name="defaultDirectoryPath">Default path for the APi documents database (must be an existing directory)</param>
        /// <param name="success">Was the setup successful?</param>
        public APiBase(string defaultDirectoryPath, out bool success)
        {
            DefaultPath = "";
            initialized = success = false;
            if (!Directory.Exists(defaultDirectoryPath))
                return;
            DefaultPath = defaultDirectoryPath;
            initialized = success = true;
        }

        /// <summary>
        /// Currently loaded/existing APi-categories
        /// </summary>
        public List<APiCategory> CurrentDocumentApiCategories { get; private set; }

        public const string STREAM_DefaultExtension = ".txt";
        public static readonly char STREAM_Separator = Path.DirectorySeparatorChar;

        // Streaming macros for custom APiDocs database convertor
        private const string STREAM_DOCS_DATA = "> DOCUMENT DATA <";
        private const string STREAM_MACRO_CATEGORY_START = "|>";
        private const string STREAM_MACRO_CATEGORY_READONLY_END = "<|";
        private const string STREAM_MACRO_ELEMENT_START = "|#";
        private const string STREAM_MACRO_IGNORE_LINES_START = "|NEW_LINE_IGNORE_START";
        private const string STREAM_MACRO_IGNORE_LINES_END = "|NEW_LINE_IGNORE_END";

        /// <summary>
        /// Load specific document from the database (See documentation for syntax rules)
        /// </summary>
        /// <param name="documentNameWithoutExtension">Existing document name without extension or a directory name</param>
        /// <param name="exception">Exception message of the result</param>
        /// <returns>Returns true if the operation was succcessful</returns>
        public bool ApiLoadDocument(string documentNameWithoutExtension, out string exception)
        {
            CurrentDocumentApiCategories = ApiLoadDocumentData(documentNameWithoutExtension, out exception);
            return CurrentDocumentApiCategories != null;
        }

        /// <summary>
        /// Load specific document database. Can be single file or stripped multiple files of .txt type. Please see syntax rules for more info
        /// </summary>
        /// <param name="documentNameWithoutExtension">Existing document name without extension or a directory name</param>
        /// <param name="exception">Exception message of the result</param>
        /// <returns>Returns a complete list of categories and elements</returns>
        private List<APiCategory> ApiLoadDocumentData(string documentNameWithoutExtension, out string exception)
        {
            exception = "Load successful.";

            // Return if not initialized
            if (!initialized)
            {
                exception = "Load unsuccessful. Base system is not initialized.";
                return null;
            }

            string path = $"{DefaultPath}{STREAM_Separator}{documentNameWithoutExtension}";
            bool isDirectory = Directory.Exists(path);
            if (!isDirectory) path += STREAM_DefaultExtension;
            bool isFile = File.Exists(path);
            if (!isDirectory && !isFile)
            {
                exception = $"Load unsuccessful. The entered document name doesn't exist in the given path (File?:{isFile} or Directory?:{isDirectory})\nEntered path:{path}.";
                return null;
            }

            List<APiCategory> apiCat = new List<APiCategory>();

            // Read from directory files if possible (not needed to have all the categories in one file)
            if(isDirectory)
            {
                string[] dirs = Directory.GetFiles(path);
                if (dirs == null || (dirs != null && dirs.Length == 0))
                {
                    exception = "The document type is Directory and the directory has no data inside.";
                    return null;
                }
                foreach (string d in dirs)
                    ApiLoadDataFromFile(d, ref apiCat);
            }
            else ApiLoadDataFromFile(path, ref apiCat);

            if (apiCat.Count == 0)
                exception = "Load successful. However there is no data to read.";
            return apiCat;
        }

        /// <summary>
        /// Load data from a specific path - refers to list of category
        /// </summary>
        private void ApiLoadDataFromFile(string path, ref List<APiCategory> apiCat)
        {
            bool readingCategories = false;
            APiElement readingElement = null;
            APiCategory readingCategory = null;
            bool ignoreNewLine = false;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (readingCategories == false)
                {
                    if(line.Trim() == STREAM_DOCS_DATA)
                        readingCategories = true;

                    continue;
                }

                if (line.Trim().StartsWith(STREAM_MACRO_CATEGORY_START))
                {
                    CheckForCurrentCategory(apiCat, readingCategory, readingElement);
                    readingElement = null;
                    readingCategory = new APiCategory(ReturnPlainText(line, STREAM_MACRO_CATEGORY_START), "Category", line.TrimEnd().EndsWith(STREAM_MACRO_CATEGORY_READONLY_END));
                    if (readingCategory.ReadonlyCategory)
                    {
                        // If the category is marked as ReadOnly, it will only appear as a text in the side-bar without any content
                        apiCat.Add(readingCategory);
                        readingCategory = null;
                    }
                    continue;
                }

                // Reading the actual category elements
                if (readingCategory != null)
                {
                    bool sw = line.TrimStart().StartsWith(STREAM_MACRO_ELEMENT_START);
                    if (sw)
                    {
                        if (readingElement != null && sw)
                        {
                            readingCategory.pageElements.Add(readingElement);
                            readingElement = null;
                            ignoreNewLine = false;
                        }

                        if (ReturnElement(line, out string idclass, out string idcontent))
                            readingElement = new APiElement(idclass.Remove(0, STREAM_MACRO_ELEMENT_START.Length), idcontent);
                        else if (readingElement != null)
                            readingElement = null;
                    }
                    else if (readingElement != null)
                    {
                        if(ignoreNewLine == false && line.Trim().StartsWith(STREAM_MACRO_IGNORE_LINES_START))
                            ignoreNewLine = true;
                        else if (line.Trim().StartsWith(STREAM_MACRO_IGNORE_LINES_END))
                            ignoreNewLine = false;
                        else
                        {
                            if (ignoreNewLine == false)
                                readingElement.idText += "\n";
                            readingElement.idText += line;
                        }
                    }
                }
            }

            CheckForCurrentCategory(apiCat, readingCategory, readingElement);
        }

        private void CheckForCurrentCategory(List<APiCategory> catBase, APiCategory cat, APiElement el)
        {
            if (cat == null)
                return;
            CheckForCurrentElement(el, cat);
            catBase.Add(cat);
        }

        private void CheckForCurrentElement(APiElement el, APiCategory cat)
        {
            if (el != null)
                cat.pageElements.Add(el);
        }

        public struct RequiredTemplates
        {
            public string htmlTemplatePath;
            public string styleTemplatePath;
        }

        /// <summary>
        /// Export a specific document to the complete APi-documentation html format with all the required files
        /// This is more-memory consuming operation.
        /// </summary>
        /// <param name="srcDocumentNameWithoutExtension">Existing document name without extension (File or Directory)</param>
        /// <param name="documentTitle">Document title - header for the final HTML document</param>
        /// <param name="specificExportPath">Specific export path for the document (Directory path)</param>
        /// <param name="requiredTemplates">Required templates (specific path for html template and css/style template)</param>
        /// <param name="exportException">Exception message of the result</param>
        /// <param name="autoFormatCodeStyle">if enabled, the built-in & custom regex patterns will be applied to the code-format styles</param>
        /// <param name="customCodeHTMLStyles">Additional custom html code-styles (optional)</param>
        /// <returns>Returns true if the operation was succcessful</returns>
        public bool ApiExportDocument(string srcDocumentNameWithoutExtension, string documentTitle, string specificExportPath, RequiredTemplates requiredTemplates, out string exportException, bool autoFormatCodeStyle = true, APiHTMLExport.CodeHTMLStyles[] customCodeHTMLStyles = null)
        {
            // Return if not initialized
            if (!initialized)
            {
                exportException = "Export unsuccessful. Base system is not initialized!";
                return false;
            }
            
            // Load specifi categories from the document
            List<APiCategory> apiCat = ApiLoadDocumentData(srcDocumentNameWithoutExtension, out exportException);
            if (apiCat == null)
            {
                exportException = "Export unsuccessful. Source document from the load-process additionally says: " + exportException;
                return false;
            }

            return APiHTMLExport.ApiExportToHtml(apiCat, documentTitle,
                specificExportPath,
                requiredTemplates.htmlTemplatePath,
                requiredTemplates.styleTemplatePath, 
                out exportException, autoFormatCodeStyle, customCodeHTMLStyles);
        }

        /// <summary>
        /// Export currently-opened document to the complete APi-documentation html format with all the required files
        /// This is less-memory consuming operation.
        /// </summary>
        /// <param name="documentTitle">Document title - header for the final HTML document</param>
        /// <param name="specificExportPath">Specific export path for the document (Directory path)</param>
        /// <param name="requiredTemplates">Required templates (specific path for html template and css/style template)</param>
        /// <param name="exportException">Exception message of the result</param>
        /// <param name="autoFormatCodeStyle">if enabled, the built-in & custom regex patterns will be applied to the code-format styles</param>
        /// <param name="customCodeHTMLStyles">Additional custom html code-styles (optional)</param>
        /// <returns>Returns true if the operation was succcessful</returns>
        public bool ApiExportDocument(string documentTitle, string specificExportPath, RequiredTemplates requiredTemplates, out string exportException, bool autoFormatCodeStyle = true, APiHTMLExport.CodeHTMLStyles[] customCodeHTMLStyles = null)
        {
            // Return if not initialized
            if (!initialized)
            {
                exportException = "Export unsuccessful. Base system is not initialized!";
                return false;
            }

            if (CurrentDocumentApiCategories == null || CurrentDocumentApiCategories.Count == 0)
            {
                exportException = "Export unsuccessful. There is no data to be exported!";
                return false;
            }

            return APiHTMLExport.ApiExportToHtml(CurrentDocumentApiCategories, documentTitle,
                specificExportPath,
                requiredTemplates.htmlTemplatePath,
                requiredTemplates.styleTemplatePath,
                out exportException, autoFormatCodeStyle, customCodeHTMLStyles);
        }

        /// <summary>
        /// Get all documents in the currently initialized solution (Returns existing files and directories)
        /// </summary>
        /// <returns>Returns an array of all documents in the root folder</returns>
        public string[] ApiGetAllDocuments(out bool success, out string exception)
        {
            success = false;
            // Return if not initialized
            if (!initialized)
            {
                exception = "Load of all documents unsuccessful. Base system is not initialized!";
                return null;
            }

            success = true;
            exception = "Load of all documents successful.";
            List<string> datas = new List<string>();
            datas.AddRange(Directory.GetFiles(DefaultPath));
            datas.AddRange(Directory.GetDirectories(DefaultPath));
            return datas.ToArray();
        }

        #region Private Helpers

        private string ReturnPlainText(string fullEntry, string removeMacro)
            => fullEntry.Replace(removeMacro, "").Replace(STREAM_MACRO_CATEGORY_READONLY_END,"").TrimEnd();
        
        private bool ReturnElement(string fullEntry, out string idClass, out string idContent)
        {
            idClass = "NULL";
            idContent = "NULL";
            if (!fullEntry.Contains("="))
                return false;
            idClass = fullEntry.Substring(0, fullEntry.IndexOf('='));
            if (idClass.Length == 0)
                return false;
            idContent = fullEntry.Substring(idClass.Length + 1, fullEntry.Length - idClass.Length - 1);
            return true;
        }

        #endregion
    }
}
