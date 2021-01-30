// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Displays
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using static Controls;

    public class ObjectGraphDisplay : Display
    {
        private static readonly ConcurrentDictionary<Type, MemberInfo[]> ReadableMemberCache = new ConcurrentDictionary<Type, MemberInfo[]>();

        private ObjectGraphDisplay()
        {
        }

        public static ObjectGraphDisplay Instance { get; } = new ObjectGraphDisplay();

        public override bool CanDisplay(Scope scope, Type type, object value) => value is object;

        protected override Control Update(Control control, Scope scope, Type type, object value, IReadOnlyList<Display> displays)
        {
            var readableMembers = GetReadableMembers(type).ToLookup(GetMemberIsStatic);
            var staticMembers = readableMembers[true].ToList();
            var instanceMembers = readableMembers[false].ToList();
            var rowCount = Math.Max(1, instanceMembers.Count);

            if (control is TableLayoutPanel propertiesTable && propertiesTable.Tag == this && propertiesTable.ColumnCount == 2)
            {
                propertiesTable.SuspendLayout();

                for (var i = rowCount; i < propertiesTable.RowCount; i++)
                {
                    var keyControl = propertiesTable.GetControlFromPosition(0, i);
                    if (keyControl != null)
                    {
                        propertiesTable.Controls.Remove(keyControl);
                        keyControl.Dispose();
                    }

                    var valueControl = propertiesTable.GetControlFromPosition(1, i);
                    if (valueControl != null)
                    {
                        propertiesTable.Controls.Remove(valueControl);
                        valueControl.Dispose();
                    }
                }

                propertiesTable.RowCount = rowCount;
            }
            else
            {
                propertiesTable = MakeTablePanel(rowCount, 2, tag: this);
                propertiesTable.SuspendLayout();
            }

            for (var i = 0; i < instanceMembers.Count; i++)
            {
                var p = i; // Closure variable.
                var member = instanceMembers[p];

                var oldLabel = propertiesTable.GetControlFromPosition(0, p);
                if (oldLabel is Label label && label.Tag == this)
                {
                    label.Text = member.Name;
                }
                else
                {
                    propertiesTable.Controls.Add(MakeLabel(member.Name, tag: this), 0, p);
                    oldLabel?.Dispose();
                }

                MemberDisplay.FindAndUpdate(
                    propertiesTable.GetControlFromPosition(1, p),
                    scope,
                    member,
                    value,
                    displays,
                    (oldControl, newControl) =>
                    {
                        if (oldControl != null)
                        {
                            propertiesTable.Controls.Remove(oldControl);
                            oldControl.Dispose();
                        }

                        if (newControl != null)
                        {
                            propertiesTable.Controls.Add(newControl, 1, p);
                        }
                    });
            }

            propertiesTable.ResumeLayout();

            return propertiesTable;
        }

        private static bool GetMemberIsStatic(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.IsStatic;

                case PropertyInfo property:
                    return property.GetAccessors(true)[0].IsStatic;
            }

            throw new NotImplementedException();
        }

        private static object GetMemberValue(MemberInfo member, object value)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.GetValue(field.IsStatic ? null : value);

                case PropertyInfo property:
                    return property.GetValue(property.GetMethod.IsStatic ? null : value);
            }

            throw new NotImplementedException();
        }

        private static Type GetMemberValueType(MemberInfo member)
        {
            switch (member)
            {
                case FieldInfo field:
                    return field.FieldType;

                case PropertyInfo property:
                    return property.PropertyType;
            }

            throw new NotImplementedException();
        }

        private static MemberInfo[] GetReadableMembers(Type type)
        {
            return ReadableMemberCache.GetOrAdd(type, t =>
            {
                IEnumerable<Type> baseTypes;
                if (t.IsInterface)
                {
                    var seen = new HashSet<Type>();

                    var queue = new Queue<Type>();
                    queue.Enqueue(t);

                    while (queue.Count > 0)
                    {
                        var @interface = queue.Dequeue();
                        if (seen.Add(@interface))
                        {
                            foreach (var subInterface in @interface.GetInterfaces())
                            {
                                queue.Enqueue(subInterface);
                            }
                        }
                    }

                    baseTypes = seen;
                }
                else
                {
                    baseTypes = new[] { t };
                }

                var fields = baseTypes.SelectMany(bt => bt.GetFields(BindingFlags.Public | BindingFlags.Instance));
                var properties = baseTypes.SelectMany(bt => bt.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(p => p.CanRead));

                var staticFields = baseTypes.SelectMany(bt => bt.GetFields(BindingFlags.Public | BindingFlags.Static));
                var staticProperties = baseTypes.SelectMany(bt => bt.GetProperties(BindingFlags.Public | BindingFlags.Static).Where(p => p.CanRead));
                return fields.Cast<MemberInfo>().Concat(properties).Concat(staticFields).Concat(staticProperties).ToArray();
            });
        }

        private class FieldDisplay : MemberDisplay
        {
            private FieldDisplay()
            {
            }

            public static FieldDisplay Instance { get; } = new FieldDisplay();

            public override bool CanDisplay(Scope scope, MemberInfo member, object obj) => member is FieldInfo;

            protected override Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays)
            {
                var field = (FieldInfo)member;
                var value = field.GetValue(obj);
                return Display.FindAndUpdate(
                    control,
                    scope.Extend(member.Name, value),
                    field.FieldType,
                    value,
                    displays);
            }
        }

        private class ListItemDisplay : MemberDisplay
        {
            private ListItemDisplay()
            {
            }

            public static ListItemDisplay Instance { get; } = new ListItemDisplay();

            public override bool CanDisplay(Scope scope, MemberInfo member, object obj)
            {
                if (!(member is PropertyInfo property && property.Name == "Item"))
                {
                    return false;
                }

                var parameters = property.GetIndexParameters();
                if (parameters.Length != 1)
                {
                    return false;
                }

                var parameter = parameters[0];
                if (parameter.Name != "index" || parameter.ParameterType != typeof(int))
                {
                    return false;
                }

                return GetReadableMembers(property.DeclaringType).Where(IsCount).Take(2).Count() == 1;
            }

            protected override Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays)
            {
                var property = (PropertyInfo)member;
                var countProperty = GetReadableMembers(property.DeclaringType).First(IsCount);
                var count = (int)GetMemberValue(countProperty, obj);
                var values = Enumerable.Range(0, count).Select(argument => property.GetValue(obj, new object[] { argument })).ToList();

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

                    Display.FindAndUpdate(
                        flowPanel.Controls.Count > i ? flowPanel.Controls[i] : null,
                        scope.Extend($"{member.Name}[{i}]", item, new Dictionary<string, object> { [Scope.SharedProperties.Key] = i }),
                        property.PropertyType,
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

            private static bool IsCount(MemberInfo member) =>
                            (member.Name == "Count" || member.Name == "Length") && GetMemberValueType(member) == typeof(int) && !GetMemberIsStatic(member);
        }

        private class MatrixItemDisplay : MemberDisplay
        {
            private MatrixItemDisplay()
            {
            }

            public static MatrixItemDisplay Instance { get; } = new MatrixItemDisplay();

            public override bool CanDisplay(Scope scope, MemberInfo member, object obj)
            {
                if (!(member is PropertyInfo property && property.Name == "Item"))
                {
                    return false;
                }

                var parameters = property.GetIndexParameters();
                if (parameters.Length != 2)
                {
                    return false;
                }

                var first = parameters[0];
                var second = parameters[1];

                if (!(first.Name == "x" && first.ParameterType == typeof(int) && second.Name == "y" && second.ParameterType == typeof(int)))
                {
                    return false;
                }

                var dimensions = GetDimensionMembers(GetReadableMembers(property.DeclaringType));
                return dimensions.width != null && dimensions.height != null && GetMemberIsStatic(dimensions.width) == GetMemberIsStatic(dimensions.height);
            }

            protected override Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays)
            {
                var property = (PropertyInfo)member;
                var dimensions = GetDimensionMembers(GetReadableMembers(property.DeclaringType));
                var width = (int)GetMemberValue(dimensions.width, obj);
                var height = (int)GetMemberValue(dimensions.height, obj);
                var range = from x in Enumerable.Range(0, width)
                            from y in Enumerable.Range(0, height)
                            select new { x, y };

                if (control is TableLayoutPanel tablePanel && tablePanel.Tag == this)
                {
                    tablePanel.SuspendLayout();

                    for (var y = width < tablePanel.ColumnCount ? 0 : height; y < tablePanel.RowCount; y++)
                    {
                        for (var x = height < tablePanel.RowCount && y >= height ? 0 : width; x < tablePanel.ColumnCount; x++)
                        {
                            var oldControl = tablePanel.GetControlFromPosition(x, y);
                            if (oldControl != null)
                            {
                                tablePanel.Controls.Remove(oldControl);
                                oldControl.Dispose();
                            }
                        }
                    }

                    tablePanel.ColumnCount = width;
                    tablePanel.RowCount = height;
                }
                else
                {
                    tablePanel = MakeTablePanel(Math.Max(1, height), Math.Max(1, width), tag: this);
                    tablePanel.SuspendLayout();
                }

                foreach (var pair in range)
                {
                    var value = property.GetValue(obj, new object[] { pair.x, pair.y });
                    Display.FindAndUpdate(
                        tablePanel.GetControlFromPosition(pair.x, pair.y),
                        scope.Extend($"{member.Name}[{pair.x}, {pair.y}]", value, new Dictionary<string, object> { [Scope.SharedProperties.Key] = new object[] { pair.x, pair.y } }),
                        property.PropertyType,
                        value,
                        displays,
                        (oldControl, newControl) =>
                        {
                            if (oldControl != null)
                            {
                                tablePanel.Controls.Remove(oldControl);
                                oldControl.Dispose();
                            }

                            if (newControl != null)
                            {
                                tablePanel.Controls.Add(newControl, pair.x, pair.y);
                            }
                        });
                }

                tablePanel.ResumeLayout();

                return tablePanel;
            }

            private static (MemberInfo width, MemberInfo height) GetDimensionMembers(MemberInfo[] members)
            {
                var widthProperty = members.FirstOrDefault(member => member.Name == "Width" && GetMemberValueType(member) == typeof(int));
                var heightProperty = members.FirstOrDefault(member => member.Name == "Height" && GetMemberValueType(member) == typeof(int));

                if (widthProperty == null && heightProperty == null)
                {
                    var sizeProperty = members.FirstOrDefault(member => member.Name == "Size" && GetMemberValueType(member) == typeof(int));
                    if (sizeProperty != null)
                    {
                        widthProperty = sizeProperty;
                        heightProperty = sizeProperty;
                    }
                }

                return (widthProperty, heightProperty);
            }
        }

        private abstract class MemberDisplay
        {
            private static readonly IList<MemberDisplay> MemberDisplays = new List<MemberDisplay>
            {
                FieldDisplay.Instance,
                SimplePropertyDisplay.Instance,
                ListItemDisplay.Instance,
                MatrixItemDisplay.Instance,
            }.AsReadOnly();

            public static Control FindAndUpdate(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays, Action<Control, Control> update)
            {
                foreach (var display in MemberDisplays)
                {
                    if (display.CanDisplay(scope, member, obj))
                    {
                        return display.Update(control, scope, member, obj, displays, update);
                    }
                }

                return null;
            }

            public abstract bool CanDisplay(Scope scope, MemberInfo member, object obj);

            public Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays, Action<Control, Control> update = null)
            {
                var newControl = this.Update(control, scope, member, obj, displays);

                if (update != null && !object.ReferenceEquals(control, newControl))
                {
                    update.Invoke(control, newControl);
                }

                return newControl;
            }

            protected abstract Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays);
        }

        private class SimplePropertyDisplay : MemberDisplay
        {
            private SimplePropertyDisplay()
            {
            }

            public static SimplePropertyDisplay Instance { get; } = new SimplePropertyDisplay();

            public override bool CanDisplay(Scope scope, MemberInfo member, object obj) => member is PropertyInfo property && property.GetIndexParameters().Length == 0;

            protected override Control Update(Control control, Scope scope, MemberInfo member, object obj, IReadOnlyList<Display> displays)
            {
                var property = (PropertyInfo)member;
                var value = property.GetValue(obj);
                return Display.FindAndUpdate(
                    control,
                    scope.Extend(member.Name, value),
                    property.PropertyType,
                    value,
                    displays);
            }
        }
    }
}
