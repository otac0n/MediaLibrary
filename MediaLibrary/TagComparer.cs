// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using TaggingLibrary;

    public class TagComparer : IComparer<string>
    {
        private readonly Dictionary<string, Color?> colorCache = new Dictionary<string, Color?>();
        private readonly Dictionary<string, double> orderCache = new Dictionary<string, double>();
        private readonly TagRuleEngine tagEngine;

        public TagComparer(TagRuleEngine tagEngine)
        {
            this.tagEngine = tagEngine;
        }

        public int Compare(string a, string b)
        {
            var comp = 0;

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

            return StringComparer.CurrentCultureIgnoreCase.Compare(a, b);
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
