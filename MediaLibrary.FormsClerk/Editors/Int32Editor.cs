// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Editors
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Windows.Forms;

    public class Int32Editor : Editor
    {
        private Int32Editor()
        {
        }

        public static Int32Editor Instance { get; } = new Int32Editor();

        public override bool CanEdit(Scope scope, Parameter parameter, object value) => parameter.ParameterType == typeof(int);

        protected override Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set)
        {
            var min = int.MinValue;
            var max = int.MaxValue;

            var range = parameter.Validations.OfType<RangeAttribute>().FirstOrDefault();
            if (range != null && range.OperandType == typeof(int))
            {
                max = (int)range.Maximum;
                min = (int)range.Minimum;
            }

            var numericUpDown = new NumericUpDown
            {
                Minimum = min,
                Maximum = max,
                Tag = this,
            };
            numericUpDown.Value = value as int? ?? default;
            numericUpDown.AddMargin(right: ErrorIconPadding);
            numericUpDown.ValueChanged += (_, a) =>
            {
                setError(numericUpDown, null);
                set((int)numericUpDown.Value, true);
            };
            set((int)numericUpDown.Value, true);

            return errorControl = numericUpDown;
        }
    }
}
