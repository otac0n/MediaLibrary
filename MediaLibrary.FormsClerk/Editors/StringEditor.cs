// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Editors
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;

    public class StringEditor : Editor
    {
        private StringEditor()
        {
        }

        public static StringEditor Instance { get; } = new StringEditor();

        public override bool CanEdit(Scope scope, Parameter parameter, object value) => parameter.ParameterType == typeof(string);

        protected override Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set)
        {
            var textBox = new TextBox
            {
                Text = value as string ?? string.Empty,
                PasswordChar = parameter.IsPassword ? '*' : '\0',
                UseSystemPasswordChar = true,
                Tag = this,
            };
            textBox.AddMargin(right: ErrorIconPadding);
            textBox.TextChanged += (_, a) =>
            {
                setError(textBox, null);
                set(textBox.Text, true);
            };
            set(textBox.Text, true);

            return errorControl = textBox;
        }
    }
}
