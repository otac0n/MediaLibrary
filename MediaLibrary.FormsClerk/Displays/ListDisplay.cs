// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Displays
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq;
    using System.Windows.Forms;
    using static Controls;

    public class ListDisplay : Display
    {
        private static HashSet<Type> Types = new HashSet<Type>
        {
            typeof(List<>),
            typeof(IList<>),
            typeof(IReadOnlyList<>),
            typeof(IReadOnlyCollection<>),
            typeof(ImmutableArray<>),
            typeof(ImmutableList<>),
            typeof(IImmutableList<>),
            typeof(ImmutableStack<>),
            typeof(IImmutableStack<>),
        };

        private ListDisplay()
        {
        }

        public static ListDisplay Instance { get; } = new ListDisplay();

        public override bool CanDisplay(Scope scope, Type type, object value)
        {
            if (type.IsArray)
            {
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var genericType = type.GetGenericTypeDefinition();
                return Types.Contains(genericType);
            }

            return false;
        }

        protected override Control Update(Control control, Scope scope, Type type, object value, IReadOnlyList<Display> displays)
        {
            var elementType = type.IsArray
                ? type.GetElementType()
                : type.GetGenericArguments().Single();
            var values = ((IEnumerable)value).Cast<object>().ToList();

            if (control is FlowLayoutPanel flowPanel && flowPanel.Tag == this)
            {
                flowPanel.SuspendLayout();
                for (var i = flowPanel.Controls.Count - 1; i >= values.Count; i--)
                {
                    var oldControl = flowPanel.Controls[i];
                    flowPanel.Controls.RemoveAt(i);
                    oldControl.Dispose();
                }
            }
            else
            {
                flowPanel = MakeFlowPanel(tag: this);
                flowPanel.SuspendLayout();
            }

            for (var i = 0; i < values.Count; i++)
            {
                var item = values[i];
                var itemName = $"[{i}]";

                Display.FindAndUpdate(
                    flowPanel.Controls.Count > i ? flowPanel.Controls[i] : null,
                    scope.Extend(itemName, item, new Dictionary<string, object> { [Scope.SharedProperties.Key] = i }),
                    elementType,
                    item,
                    displays,
                    (oldControl, newControl) =>
                    {
                        if (oldControl != null)
                        {
                            flowPanel.Controls.Remove(oldControl);
                            oldControl.Dispose();
                        }

                        if (newControl != null)
                        {
                            flowPanel.Controls.Add(newControl);
                            flowPanel.Controls.SetChildIndex(newControl, i);
                        }
                    });
            }

            flowPanel.ResumeLayout();

            return flowPanel;
        }
    }
}
