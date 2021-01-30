// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.FormsClerk.Editors;

    public abstract class Editor
    {
        protected const int ErrorIconPadding = 32;

        private static IList<Editor> editors;

        private static IList<Editor> Editors => editors ?? (editors = new List<Editor>
        {
            BoolEditor.Instance,
            StringEditor.Instance,
            Int32Editor.Instance,
            EnumEditor.Instance,
            ObjectGraphEditor.Instance,
        }.AsReadOnly());

        public static Control FindAndUpdate(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set, Action<Control, Control> update = null)
        {
            foreach (var editor in editors == null ? Editors : editors.Concat(Editors))
            {
                if (editor.CanEdit(scope, parameter, value))
                {
                    return editor.Update(control, scope, parameter, value, out errorControl, editors, setError, set, update);
                }
            }

            errorControl = null;
            return null;
        }

        public abstract bool CanEdit(Scope scope, Parameter parameter, object value);

        public Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set, Action<Control, Control> update = null)
        {
            var newControl = this.Update(control, scope, parameter, value, out errorControl, editors, setError, set);

            if (update != null && !object.ReferenceEquals(control, newControl))
            {
                update.Invoke(control, newControl);
            }

            return newControl;
        }

        protected abstract Control Update(Control control, Scope scope, Parameter parameter, object value, out Control errorControl, IReadOnlyList<Editor> editors, Action<Control, string> setError, Action<object, bool> set);
    }
}
