namespace APiDocsToHtml
{
    /// <summary>
    /// Base class for api element identity - for both categories and category-elements.
    /// Both share the same html params - style class and text content.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public abstract class APiIdentity : APiData
    {
        public string idText;
        public string idClass { get; protected set; }
    }
}
