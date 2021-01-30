// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Editors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Windows.Forms;
    using static Controls;

    public class ObjectGraphEditor : Editor
    {
        private ObjectGraphEditor()
        {
        }

        public delegate bool OverrideEditor(Scope scope, Parameter parameter, object value, out Control control, out Control errorControl, out Label label, Action<Control, string> setError, Action<object, bool> set);

        public static ObjectGraphEditor Instance { get; } = new ObjectGraphEditor();

        public override bool CanEdit(Scope scope, Parameter parameter, object value) => true;

        public Control Update(Control control, Scope scope, Parameter parameter, IInitializer[] initializers, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set, Action<Control, Control> update = null)
        {
            var newControl = this.Update(control, scope, parameter, initializers, value, out errorControl, editors, setError, set);

            if (update != null && !object.ReferenceEquals(control, newControl))
            {
                update.Invoke(control, newControl);
            }

            return newControl;
        }

        public Control Update(Control control, Scope scope, Parameter parameter, IInitializer[] initializers, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set)
        {
            var propertiesTable = MakeTablePanel(1, 2);

            var constructorList = new ComboBox
            {
                DisplayMember = "Name",
                DropDownStyle = ComboBoxStyle.DropDownList,
                Tag = this,
            };
            constructorList.AddMargin(right: ErrorIconPadding);

            constructorList.SelectedIndexChanged += (_, a) =>
            {
                var constructor = constructorList.SelectedItem as IInitializer;
                var parameterCount = constructor.Parameters.Count;
                propertiesTable.SuspendLayout();
                propertiesTable.Controls.DisposeAndClear();
                propertiesTable.RowCount = Math.Max(1, parameterCount);
                var parameters = new object[parameterCount];
                var innerValid = new bool[parameterCount];
                var disposeControls = new Control[parameterCount];
                var errorControls = new Dictionary<string, Control>();

                void Touch()
                {
                    setError(constructorList, null);

                    object parameterValue = null;
                    var valid = innerValid.All(v => v);
                    if (valid)
                    {
                        try
                        {
                            parameterValue = constructor.Accessor(parameters);
                            foreach (var innerErrorControl in errorControls.Values)
                            {
                                setError(innerErrorControl, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            valid = false;

                            if (ex is TargetInvocationException)
                            {
                                ex = ex.InnerException;
                            }

                            Control innerErrorControl = null;
                            switch (ex)
                            {
                                case ArgumentException argumentException:
                                    errorControls.TryGetValue(argumentException.ParamName, out innerErrorControl);
                                    break;

                                default:
                                    break;
                            }

                            setError(innerErrorControl ?? constructorList, ex.Message);
                        }
                    }

                    set(parameterValue, valid);
                }

                for (var i = 0; i < parameterCount; i++)
                {
                    var p = i; // Closure variable.
                    var innerParameter = constructor.Parameters[p];

                    var item = innerParameter.Default.ValueOrDefault;
                    var innerControl = Editor.FindAndUpdate(
                        null,
                        scope.Extend(innerParameter.Name, item),
                        innerParameter,
                        item,
                        out var innerErrorControl,
                        editors,
                        setError,
                        (innerValue, valid) =>
                        {
                            (parameters[p] as IDisposable)?.Dispose();
                            parameters[p] = innerValue;
                            innerValid[p] = valid;
                            if (i >= parameterCount)
                            {
                                Touch();
                            }
                        },
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

                    if (!(innerControl is CheckBox))
                    {
                        var label = MakeLabel(innerParameter.DisplayName, tag: this);

                        switch (innerControl)
                        {
                            case ComboBox comboBox:
                                label.AddMargin(top: 10);
                                break;

                            case TextBox textbox:
                            case NumericUpDown numericUpDown:
                                label.AddMargin(top: 5);
                                break;
                        }

                        propertiesTable.Controls.Add(label, 0, p);
                    }

                    disposeControls[p] = innerControl;
                    errorControls[innerParameter.Name] = innerErrorControl;
                }

                propertiesTable.ResumeLayout();

                Touch();
            };

            constructorList.Items.AddRange(initializers);
            if (constructorList.Items.Count > 0)
            {
                constructorList.SelectedIndex = 0;
            }

            var tablePanel = MakeTablePanel(2, 1);
            tablePanel.Controls.Add(constructorList, 0, 0);
            tablePanel.Controls.Add(propertiesTable, 0, 1);

            // TODO: Dispose controls when tablePanel is disposed.
            errorControl = constructorList;
            return tablePanel;
        }

        protected override Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set)
        {
            var publicInitializers = parameter.ParameterType.GetPublicInitializers();
            var initializers = (parameter.ParameterType.IsValueType
                ? publicInitializers
                : new[] { new Initializer("(null)", args => null, Array.Empty<Parameter>()) }.Concat(publicInitializers)).ToArray();
            return this.Update(control, scope, parameter, initializers, value, out errorControl, editors, setError, set);
        }
    }
}
