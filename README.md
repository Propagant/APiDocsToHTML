# APiDocsToHTML
Simple convertor from custom APi documentation to the readable HTML documentation.\
Convert your code to the readable APi documentation in a markup HTML style.\
Written by Matej Vanco 2022, [https://matejvanco.com](https://matejvanco.com) (MIT License)

**Programming Language:** C#\
**Framework:** .NET 4.7.2\
**Platform:** Windows, OSX, Linux
**Version:** First release (19/01/2023)\
Not properly tested, used for my own purposes.\
Please reach me out if you face any problems or issues.

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

Once your project has an access to the APiDocsToHTML namespace, you can generate an existing APiDocsToHTML document to the HTML documentation. See the example below. The following example runs in a regular C# Console Application.

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
# APi Document
It's required to know how the APi documents work and how to write them. The APiDocsToHTML converts your code in a following order:

**1. Create an APi document with your code that you'd like to export\
2. Load the document to the APiBase class\
3. Load HTML+CSS templates\
4. Export**

Creating APi documents is an easy process. Supported file format is a well known .txt format. Create a new txt document and now you can start writing your custom "documentation" that will be later converted to the markup HTML format.

See the following example
```text
	> DOCUMENT DATA <

|>Introduction|Space<|

|>Intro|Introduction
|#Title=This is a regular Title
|#Text=This is a regular Text element... You can create as many elements as you want.

You can also create spaces!	And tabs!

<b>You can also use html macros!</b> <i>Yeah!</i>
|#

|#Code=And this is code...

int number = 5;
string lmao = "hello world";
```
It might look complicated at a first sight, but believe me, it's easy! Check the official syntax for APi documents.

## Syntax

- The official conversion starts below `> DOCUMENT DATA <` macro
- Categories, that will contain certain elements use symbol `|>`
- Readonly categories (without any elements) use symbol `|>` and must end with symbol `<|`
- Category-Elements use symbol `|#`
- You can end a specific element content with empty element symbol `|#`
- Everything below certain category belongs to the specific category
- Everything that doesn't start with the category symbol or element symbol will belong to the currently selected element as a CONTENT
- Element syntax is as follows: **|#STYLECLASS=CONTENT** (use regular tabs/line breaks to create a much organized content)
- The only available style classes are as follow: **Title, Text, Code**
- It's fine to use blank spaces for better organisation
- There are some styling options for custom code-formation:
    - Code-type: `<ct></c>`
    - Code-comment: `<cc></c>`
    - Code-keyword: `<ck></c>`
    - Code-string content: `<cs></c>`
- NOTICE that all the styling hashes end with `</c>`. It's just a little shortcut
- In case you would like to automate the code-formatting, you can try to use my custom regex pattern (especially written for C#).\
Class: **APiHTMLExport.CodeHTMLStyles**

## Category Attributes
All categories are allowed to use certain attributes or category root (more below). The current version contains just one built-in attribute **'Space'** which makes a little space above the drawn category.\
This might help you to make the navigation panel more readable and organized with spaces. See the example below.

```text
> DOCUMENT DATA <

|>Hello World|Space
|#Title=Hello World! My category has a little space above...
|>Bye World|Space
|#Title=This category has a little space above too...
|>Well Bye
|#Title=This has not!
```
It's possible to use just one attribute per-category. Both readonly categories and regular (with elements) categories are allowed to use one attribute.

## Category Sorting
It's possible to sort categories and create much clearer navigation panel. As mentioned above, categories might contain specific attributes (in the current version it's just one built-in attribute).
Instead of attributes, categories might also contain a root category that the specific category will belong to. The best practice for this is to use a readonly category as a root category. See the example below.

```text
> DOCUMENT DATA <

|>0Introduction<|
|>1Advanced|Space<|

|>Startup|Introduction
|#Title=Startup
|#Text=Something about startup... This belongs to the Introduction readonly category
|>Installation|Advanced
|#Title=Installation
|#Text=Something about installation... This belongs to the Installation readonly category
```

Readonly categories are allowed to use a certain 'sorting number' which will tell the compiler which category should be rendered first and which last. It's possible to sort categories from 0(first) to 9(last).\
Regular categories (with elements) are not allowed to use the 'sorting number'. That's one of the reasons why to use read-only categories for nested sorting.\
If a readonly category does not contain a sorting number, its default priority is always 9 (the latest).\
If a regular category has an attribute to join to one of the regular categories, its sorting number will be based on its first letter (a-first, z-last).

# Templates
APiDocsToHTML uses a custom html and css styles that I wrote for my purpose. If you would like to modify these templates, you can go to the *project solution/SourceFiles/Templates*. Both templates must contain certain macros that are defined in the APiBase classes & APiHTMLExport. Please see these rules/macros properly in the source file. However you are very free to edit/modify these templates as mentioned!
