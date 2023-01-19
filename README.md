# APiDocsToHTML
Simple convertor from custom APi documentation to the readable HTML documentation.\
Convert your code to the readable APi documentation in a markup HTML style.

**Programming Language:** C#\
**Framework:** .NET 4.7.2\
**Platform:** Windows, OSX, Linux

# Why?
All programmers know how painful it is to document a code/APi in a readable format for all human beings. This simple tool allows you to create custom APi databases that can be later converted to the HTML pages. Personally I needed to save some time and I wanted to document my code much effectively.
For example, I've made a full documentation for my asset on the Unity Asset Store just with this tool. Check the documentation [here](https://struct9.com/matejvanco-assets/md-package/Introduction).

# Installation (Source)
Follow the steps below for proper installation of the source project
1. Pull the project through the git
2. Open the solution in any IDE *(Visual Studio .NET 4.7.2 recommended)*
3. See the **APi_SourceFiles** directory for source files

# Usage
You can use the source project to build a dll file for your project (to use the APiDocsToHTML as a reference) or you can simply go to the APiDocsToHTML directory/bin and attach the generated dll file to your project.

Once your project has an access to the APiDocsToHTML namespace, you can generate an existing APiDocsToHTML document to the HTML documentation. See the example below. The following example runs with a regular C# Console Application.

```C#
APiBase apib = new APiBase(@"E:\APiDocsToHTML_Test", out bool success);
if (!success)
{
    Console.WriteLine("Initialization unsuccessful");
    return;
}

if (!apib.ApiLoadDocument("TestingDocument", out string e))
{
    Console.WriteLine("Error while loading the docs data:" + e);
    return;
}

APiBase.RequiredTemplates reqTemps = new APiBase.RequiredTemplates();
reqTemps.htmlTemplatePath = apib.DefaultPath+"/html.html";
reqTemps.styleTemplatePath = apib.DefaultPath + "/style.css";

if (!apib.ApiExportDocument("Testing", apib.DefaultPath+"/Export", reqTemps, out e))
{
    Console.WriteLine(e);
    return;
}

Console.WriteLine("Export successful!");
```
First, you need to **create an instance** of the **APiBase class**. The constructor automatically initializes the convertor with the main directory where all the APi documents are stored. **The included directory must exists**.

It's highly recommended to check if the APiBase was successfully initialized.
After successful initialization, we can **load a specific APi document** or we can **directly export the document**.

```C#
APiBase apib = new APiBase(@"E:\APiDocsToHTML_Test", out bool success);
if (!success)
{
    Console.WriteLine("Initialization unsuccessful");
    return;
}

APiBase.RequiredTemplates reqTemps = new APiBase.RequiredTemplates();
reqTemps.htmlTemplatePath = apib.DefaultPath+"/html.html";
reqTemps.styleTemplatePath = apib.DefaultPath + "/style.css";

if (!apib.ApiExportDocument("TestingDocument", "Testing", apib.DefaultPath+"/Export", reqTemps, out string e))
{
    Console.WriteLine(e);
    return;
}

Console.WriteLine("Export successful!");
```

Why can we load the document and then export the document? You can load a specific APi document in case you would like to receive an information about the loaded categories and their elements. See the example below.

```C#
APiBase apib = new APiBase(@"E:\APiDocsToHTML_Test", out bool success);
if (!success)
{
    Console.WriteLine("Initialization unsuccessful");
    return;
}

if (!apib.ApiLoadDocument("TestingDocument", out string e))
{
    Console.WriteLine("Error while loading the docs data:" + e);
    return;
}
// Receiving the actual category information about its style-class, content(name), attribute and elements (you can also go through all the elements per category)
foreach(var category in apib.CurrentDocumentApiCategories)
{
    Console.WriteLine($"Category class '{category.idClass}' has a text content '{category.idText}'; has an attribute '{category.parentAttribute}' and elements count {category.pageElements.Count}");
}

APiBase.RequiredTemplates reqTemps = new APiBase.RequiredTemplates();
reqTemps.htmlTemplatePath = apib.DefaultPath+"/html.html";
reqTemps.styleTemplatePath = apib.DefaultPath + "/style.css";

if (!apib.ApiExportDocument("TestingDocument", "Testing", apib.DefaultPath+"/Export", reqTemps, out e))
{
    Console.WriteLine(e);
    return;
}

Console.WriteLine("Export successful!");
```

