// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Search.Terms
{
    public class SavedSearchTerm : Term
    {
        public SavedSearchTerm(string searchName)
        {
            this.SearchName = searchName;
        }

        public string SearchName { get; }

        public override string ToString() => $"{{{this.SearchName.Replace("}", "}}")}}}";
    }
}
