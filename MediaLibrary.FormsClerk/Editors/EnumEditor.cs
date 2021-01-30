// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Editors
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;

    public class EnumEditor : Editor
    {
        private EnumEditor()
        {
        }

        public static EnumEditor Instance { get; } = new EnumEditor();

        public override bool CanEdit(Scope scope, Parameter parameter, object value) => parameter.ParameterType.IsEnum;

        protected override Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set)
        {
            var values = Enum.GetValues(parameter.ParameterType).Cast<object>().ToArray();
            var comboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Tag = this,
            };
            comboBox.Items.AddRange(values);
            comboBox.SelectedItem = value;
            comboBox.AddMargin(right: ErrorIconPadding);
            comboBox.SelectedValueChanged += (_, a) =>
            {
                setError(comboBox, null);
                set(comboBox.SelectedItem, true);
            };

            set(comboBox.SelectedItem, true);

            return errorControl = comboBox;
        }
    }
}
