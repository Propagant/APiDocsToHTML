namespace APiDocsToHtml
{
    /// <summary>
    /// Class for category-elements.
    /// This should refer to one object in a specific category.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public sealed class APiElement : APiIdentity
    {
        public APiElement(string idclass, string idtext)
        {
            idText = idtext;
            idClass = idclass;
        }
    }
}
