using System.Collections.Generic;

namespace APiDocsToHtml
{
    /// <summary>
    /// Class for a single category.
    /// This should refer to one complete category that will contain a list of elements of type APiElement.
    /// Written by Matej Vanco 2022. https://matejvanco.com
    /// </summary>
    public sealed class APiCategory : APiIdentity
    {
        public APiCategory(string idtext, string idclass, bool readOnly)
        {
            idText = idtext.GetCategoryNameOnly(out bool hasAtt);
            if (hasAtt)
                parentAttribute = idtext.GetCategoryAttributes();
            idClass = idclass;
            ReadonlyCategory = readOnly;
            pageElements = new List<APiElement>();
        }

        /// <summary>
        /// Category custom attribute = |EXISTING CATEGORY NAME (This will create a subcategory) or |Space (makes a space above)
        /// </summary>
        public string parentAttribute;
        public bool HasAttribute()
        {
            return !string.IsNullOrEmpty(parentAttribute);
        }
        /// <summary>
        /// Readonly categories can't have any content and will indicate to a readonly text in the navbar
        /// </summary>
        public bool ReadonlyCategory { get; private set; }

        public List<APiElement> pageElements;
    }
}
