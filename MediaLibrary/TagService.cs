// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Drawing;
    using System.Linq;
    using MediaLibrary.Tagging;

    public static class TagService
    {
        public static string FindProperty(this TagRuleEngine tagEngine, string tag, string prefix) =>
            tagEngine.FindProperty(tag, p => p.StartsWith(prefix, StringComparison.Ordinal));

        public static string FindProperty(this TagRuleEngine tagEngine, string tag, Func<string, bool> predicate) =>
            tagEngine.GetAllTagProperties(tag).Where(predicate).FirstOrDefault();

        public static string GetPropertyValue(this TagRuleEngine tagEngine, string tag, string propertyName)
        {
            var value = tagEngine.FindProperty(tag, propertyName + "=");
            return value == null ? value : value.Substring(propertyName.Length + 1);
        }

        public static Color? GetTagColor(this TagRuleEngine tagEngine, string tag) =>
            ColorService.ParseColor(tagEngine.GetPropertyValue(tag, "color"));

        public static Comparison<string> GetTagComparison(this TagRuleEngine tagEngine) => (a, b) =>
        {
            var comp = 0;

            var aOrder = tagEngine.GetTagOrder(a);
            var bOrder = tagEngine.GetTagOrder(b);
            if ((comp = aOrder.CompareTo(bOrder)) != 0)
            {
                return comp;
            }

            var aColor = tagEngine.GetTagColor(a);
            var bColor = tagEngine.GetTagColor(b);

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

            var aDescendants = tagEngine.GetTagDescendants(a).Count;
            var bDescendants = tagEngine.GetTagDescendants(b).Count;
            if ((comp = bDescendants - aDescendants) != 0)
            {
                return comp;
            }

            return StringComparer.CurrentCultureIgnoreCase.Compare(a, b);
        };

        public static double GetTagOrder(this TagRuleEngine tagEngine, string tag)
        {
            var value = tagEngine.GetPropertyValue(tag, "order")?.Trim();
            if (string.IsNullOrEmpty(value) || !double.TryParse(value, out var order) || double.IsNaN(order))
            {
                return 100;
            }

            return order;
        }
    }
}
