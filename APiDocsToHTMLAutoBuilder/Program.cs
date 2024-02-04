// Very simple auto-builder of the APiDocsToHTML project.
// Written by Matej Vanco, November 2023 as a shortcut for building an APi html documentations.
// Updated in February 2024

using APiDocsToHtml;

bool process = true;
string msg;

while (process)
{
    // Intro
    Console.WriteLine("Welcome to the APiDocsToHTML auto builder!\n" +
        "This simple console program will help you to build the APi documentation to html with ease.\n\n" +
        "Please Follow the instructions below...");
    Console.WriteLine("\n\n");

Again:
    Console.WriteLine("\nDo you have any export template of your APiDocsToHTML project? Write n for no, write h for 'what is the export template?' or write the exact path of your export template file:");
    string? exportTemplate = Console.ReadLine();

    // Export template
    if(exportTemplate != null)
    {
        if(exportTemplate == "h")
        {
            Console.WriteLine("Export template is a shortcut & time-saving file that helps you to skip all the steps that this console application contains for proper export of your APiDocsToHTML project.\n" +
                "The export template must be a .txt file. See the line-by-line hierarchy of how such 'export template' should look like:\n\n" +
                "1st: APiDocsToHTML root project exact directory path\n" +
                "2nd: APiDocsToHTML project directory name (in this local path)\n" +
                "3rd: html template file name + extension (in this local path)\n" +
                "4th: css template file name + extension (in this local path)\n" +
                "5th: export document title name\n" +
                "6th: export directory name (in this local path)\n" +
                "7th (optional): custom html code styles. Write 'CUSTOM CODE STYLES' first to begin writing custom code styles. Then use the following format:\n" +
                "styleClassName|styleShortcutStartingMacro|styleShortcutEndingMacro(NEWLINE)listOfCatchupPatterns(NEWLINE + CODE STYLE END to continue writing patterns for this entity)\n\n"+
                "Currently available code styles: CodeKeyword|<ck>|</c>, CodeType|<ct>|</c>, CodeString|<cs>|</c>");
            msg = "";
            goto RepeatQuestion;
        }
        else if(exportTemplate != "n")
        {
            if (exportTemplate == "auto")
            {
                exportTemplate = AppDomain.CurrentDomain.BaseDirectory + "/autoPath.txt";
                if(!File.Exists(exportTemplate))
                {
                    msg = "The entered auto path doesn't exist! Please create a file with 'autoPath.txt' in the current directory containing the target project src directory!";
                    goto RepeatQuestion;
                }
                exportTemplate = File.ReadAllText(exportTemplate);
            }
            if(!File.Exists(exportTemplate))
            {
                msg = "The entered export template file doesn't exist!";
                goto RepeatQuestion;
            }

            string[] lines = File.ReadAllLines(exportTemplate);
            if(lines.Length < 6)
            {
                msg = "Export template file has less than 6 lines! It must have exactly 6 or more... Type 'h' to see 'export-template' guidelines next time";
                goto RepeatQuestion;

            }

            APiBase api = new APiBase(lines[0], out bool succ);
            if (succ)
            {
                if (api.ApiLoadDocument(lines[1], out string ex))
                {
                    APiBase.RequiredTemplates templates = new APiBase.RequiredTemplates();
                    templates.htmlTemplatePath = api.DefaultPath + "/" + lines[2];
                    templates.styleTemplatePath = api.DefaultPath + "/" + lines[3];
                    List<APiHTMLExport.CodeHTMLStyles> codeStyles = new List<APiHTMLExport.CodeHTMLStyles>();

                    if (lines.Length > 6 && lines[6].Trim().ToUpper() == "CUSTOM CODE STYLES")
                    {
                        bool readingPatterns = false;
                        APiHTMLExport.CodeHTMLStyles currentStyle = new APiHTMLExport.CodeHTMLStyles();
                        List<string> currentPatterns = new List<string>();
                        for (int i = 7; i < lines.Length; i++)
                        {
                            if(!readingPatterns)
                            {
                                if (string.IsNullOrWhiteSpace(lines[i]))
                                    continue;

                                string[] content = lines[i].Split('|');
                                if(content.Length != 3)
                                {
                                    msg = "Invalid syntax while creating a custom code html style! CodeHTMLStyle contains just 3 params!";
                                    goto RepeatQuestion;
                                }
                                currentStyle.styleClassName = content[0];
                                currentStyle.styleShortcutStartingMacro = content[1];
                                currentStyle.styleShortcutEndingMacro = content[2];
                                readingPatterns = true;
                                Console.WriteLine("*Detected code style: " + lines[i]);
                            }
                            else
                            {
                                if (lines[i].Trim().ToUpper() == "CODE STYLE END")
                                {
                                    currentStyle.catchupPatterns = currentPatterns.ToArray();
                                    codeStyles.Add(currentStyle);
                                    Console.WriteLine("*Custom code style entity added: " + lines[i]);
                                    currentStyle = new APiHTMLExport.CodeHTMLStyles();
                                    currentPatterns.Clear();
                                    readingPatterns = false;
                                    continue;
                                }
                                Console.WriteLine("-----> Detected code pattern for this style: " + lines[i]);
                                currentPatterns.Add(lines[i]);
                            }
                        }
                    }

                    if (api.ApiExportDocument(lines[4], api.DefaultPath + "/" + lines[5], templates, out ex, true, codeStyles.ToArray()))
                    {
                        msg = "Export successful!";
                        goto RepeatQuestion;
                    }
                    else
                    {
                        msg = "Couldn't export the loaded APiDocsToHTML project. Exception: " + ex;
                        goto RepeatQuestion;
                    }
                }
                else
                {
                    msg = "Couldn't export the loaded APiDocsToHTML project. Exception: " + ex;
                    goto RepeatQuestion;
                }
            }
        }
    }

    // Main project path
    Console.WriteLine("\nPlease enter the exact directory path of your APiDocsToHTML project:");
    string? dirPath = Console.ReadLine();

    if (dirPath == null)
    {
        msg = "You have entered an invalid project directory path";
        goto RepeatQuestion;
    }
    if (!Directory.Exists(dirPath))
    {
        msg = "The entered project directory path doesn't exist";
        goto RepeatQuestion;
    }

    // APiBase initialization
    APiBase apiBase = new APiBase(dirPath, out bool success);
    if(!success)
    {
        msg = "Couldn't load the APiBase class. The entered project directory path doesn't exist or may be invalid";
        goto RepeatQuestion;
    }

    // Project name
    Console.WriteLine("Please enter the folder name in your APiDocsToHTML project you would like to export to html:");
    string? projPath = Console.ReadLine();

    if (!apiBase.ApiLoadDocument(projPath, out string e))
    {
        msg = "Couldn't load the project directory. Exception: " + e;
        goto RepeatQuestion;
    }

    // Local/ custom path
    Console.WriteLine("Would you like to use the current project path to load html templates and export the project document?\n" +
        "Currently loaded project path is: " + apiBase.DefaultPath + "\n... Write y for yes, write n for nope (If no, you will have to enter the exact file path for both html templates and export folder)");
    string? useProjP = Console.ReadLine();
    bool useProjectPath = useProjP != null && useProjP == "y";

    // Templates units
    Console.WriteLine("Now the auto-builder will require .html page and .css style templates. These templates can be pulled from the official APiDocsToHTML repository...");
    APiBase.RequiredTemplates reqTemps = new APiBase.RequiredTemplates();

    if (!useProjectPath)
    {
        Console.WriteLine("Enter the exact file path for .html page template (with file extension):");
        reqTemps.htmlTemplatePath = Console.ReadLine();
    }
    else
    {
        Console.WriteLine("Enter the file name only for .html page template (without file extension):");
        reqTemps.htmlTemplatePath = apiBase.DefaultPath + "/" + Console.ReadLine() + ".html";
    }

    if (!useProjectPath)
    {
        Console.WriteLine("Enter the exact file path for .css style template (with file extension):");
        reqTemps.styleTemplatePath = Console.ReadLine();
    }
    else
    {
        Console.WriteLine("Enter the file name only for .css style template (without file extension):");
        reqTemps.styleTemplatePath = apiBase.DefaultPath + "/" + Console.ReadLine() + ".css";
    }

    // Document Title
    Console.WriteLine("Enter the exported document title:");
    string? documentTitle = Console.ReadLine();

    string? exportFolder;
    // Export folder
    if (!useProjectPath)
    {
        Console.WriteLine("Enter the exact (existing) directory path for export folder:");
        exportFolder = Console.ReadLine();
    }
    else
    {
        Console.WriteLine("Enter the directory name only for export folder (The directory doesn't have to exist):");
        exportFolder = apiBase.DefaultPath + "/" + Console.ReadLine();
        if (exportFolder != null && !Directory.Exists(exportFolder))
            Directory.CreateDirectory(exportFolder);
    }

    if (exportFolder == null)
    {
        msg = "You have entered an empty export folder path/name";
        goto RepeatQuestion;
    }

    // Export
    if (!apiBase.ApiExportDocument(documentTitle, exportFolder, reqTemps, out e))
    {
        msg = "Couldn't export the loaded APiDocsToHTML project. Exception: " + e;
        goto RepeatQuestion;
    }

    msg = "Export successful!";

    // Repeat?
RepeatQuestion:
    Console.WriteLine("\n\n--- " + msg + " ---\n\n");
    Console.WriteLine("*Would you like to repeat the auto build process? write y for yes, write n for nope");
    string? yesNo = Console.ReadLine();
    process = yesNo != null && yesNo == "y";
    if (process)
        goto Again;
}