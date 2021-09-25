// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MediaLibrary.Storage;

    public class PersonComparer : IComparer<Person>, IComparer<IList<Person>>
    {
        public int Compare(Person a, Person b)
        {
            if (a == null)
            {
                return b == null ? 0 : -1;
            }
            else if (b == null)
            {
                return 1;
            }

            int comp;
            if ((comp = StringComparer.CurrentCultureIgnoreCase.Compare(a.Name, b.Name)) != 0)
            {
                return comp;
            }

            return a.PersonId.CompareTo(b.PersonId);
        }

        public int Compare(IList<Person> a, IList<Person> b)
        {
            var comp = a.Count.CompareTo(b.Count);
            if (comp != 0)
            {
                return comp;
            }

            var aSorted = a.OrderBy(p => p, this);
            var bSorted = b.OrderBy(p => p, this);
            return aSorted.Zip(bSorted, this.Compare).Where(c => c != 0).FirstOrDefault();
        }
    }
}
