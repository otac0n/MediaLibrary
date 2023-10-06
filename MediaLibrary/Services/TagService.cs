// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Services
{
    using System;
    using System.Drawing;
    using System.Linq;
    using TaggingLibrary;

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

        public static TagComparer GetTagComparer(this TagRuleEngine tagEngine, StringComparer nameComparer = null) => new TagComparer(tagEngine, nameComparer);

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
