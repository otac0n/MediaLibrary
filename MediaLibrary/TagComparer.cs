// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Linq;
    using TaggingLibrary;

    public class TagComparer : IComparer<string>, IComparer<IList<string>>, IComparer<ISet<string>>
    {
        private readonly Dictionary<string, Color?> colorCache = new Dictionary<string, Color?>();
        private readonly StringComparer nameComparer;
        private readonly Dictionary<string, double> orderCache = new Dictionary<string, double>();
        private readonly TagRuleEngine tagEngine;

        public TagComparer(TagRuleEngine tagEngine, StringComparer nameComparer = null)
        {
            this.tagEngine = tagEngine;
            this.nameComparer = nameComparer ?? StringComparer.CurrentCultureIgnoreCase;
        }

        public int Compare(string a, string b)
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
            var aOrder = this.GetTagOrder(a);
            var bOrder = this.GetTagOrder(b);
            if ((comp = aOrder.CompareTo(bOrder)) != 0)
            {
                return comp;
            }

            var aColor = this.GetTagColor(a);
            var bColor = this.GetTagColor(b);

            if (aColor.HasValue && bColor.HasValue)
            {
                if ((comp = aColor.Value.R.CompareTo(bColor.Value.R)) != 0 ||
                    (comp = aColor.Value.G.CompareTo(bColor.Value.G)) != 0 ||
                    (comp = aColor.Value.B.CompareTo(bColor.Value.B)) != 0)
                {
                    return comp;
                }
            }
            else if (aColor.HasValue && !bColor.HasValue)
            {
                return -1;
            }
            else if (bColor.HasValue && !aColor.HasValue)
            {
                return 1;
            }

            var aDescendants = this.tagEngine.GetTagDescendants(a).Count;
            var bDescendants = this.tagEngine.GetTagDescendants(b).Count;
            if ((comp = bDescendants - aDescendants) != 0)
            {
                return comp;
            }

            return this.nameComparer.Compare(a, b);
        }

        public int Compare(IList<string> a, IList<string> b)
        {
            var comp = b.Count.CompareTo(a.Count);
            if (comp != 0)
            {
                return comp;
            }

            var aSorted = a.OrderBy(t => t, this);
            var bSorted = b.OrderBy(t => t, this);
            return aSorted.Zip(bSorted, this.Compare).Where(c => c != 0).FirstOrDefault();
        }

        public int Compare(ISet<string> a, ISet<string> b)
        {
            IEnumerable<IGrouping<double, string>> Group(ISet<string> set) =>
                from t in set
                group t by this.GetTagOrder(t) into g
                orderby g.Key
                select g;

            var aGrouped = Group(a).ToList();
            var bGrouped = Group(b).ToList();
            var comp = aGrouped.Zip(bGrouped, (gA, gB) =>
            {
                var gComp = gA.Key.CompareTo(gB.Key);
                if (gComp != 0)
                {
                    return gComp;
                }

                return this.Compare(gA.ToList(), gB.ToList());
            }).Where(c => c != 0).FirstOrDefault();
            if (comp != 0)
            {
                return comp;
            }

            return b.Count.CompareTo(a.Count);
        }

        public Color? GetTagColor(string tag)
        {
            if (!this.colorCache.TryGetValue(tag, out var color))
            {
                this.colorCache[tag] = color = this.tagEngine.GetTagColor(tag);
            }

            return color;
        }

        public double GetTagOrder(string tag)
        {
            if (!this.orderCache.TryGetValue(tag, out var order))
            {
                this.orderCache[tag] = order = this.tagEngine.GetTagOrder(tag);
            }

            return order;
        }
    }
}
