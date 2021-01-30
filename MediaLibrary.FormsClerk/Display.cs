// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Windows.Forms;
    using MediaLibrary.FormsClerk.Displays;

    public abstract class Display
    {
        private static IList<Display> displays;

        private static IList<Display> Displays => displays ?? (displays = new List<Display>
        {
            NullDisplay.Instance,
            PrimitiveDisplay.Instance,
            DictionaryDisplay.Instance,
            ListDisplay.Instance,
            ObjectGraphDisplay.Instance,
        }.AsReadOnly());

        public static Control FindAndUpdate<T>(Control control, Scope scope, T value, IReadOnlyList<Display> displays, Action<Control, Control> update = null) =>
            FindAndUpdate(control, scope, typeof(T), value, displays, update);

        public static Control FindAndUpdate(Control control, Scope scope, Type type, object value, IReadOnlyList<Display> displays, Action<Control, Control> update = null)
        {
            foreach (var display in displays == null ? Displays : displays.Concat(Displays))
            {
                if (display.CanDisplay(scope, type, value))
                {
                    return display.Update(control, scope, type, value, displays, update);
                }
            }

            return null;
        }

        public abstract bool CanDisplay(Scope scope, Type type, object value);

        public Control Update(Control control, Scope scope, Type type, object value, IReadOnlyList<Display> displays, Action<Control, Control> update = null)
        {
            var newControl = this.Update(control, scope, type, value, displays);

            if (update != null && !object.ReferenceEquals(control, newControl))
            {
                update.Invoke(control, newControl);
            }

            return newControl;
        }

        protected abstract Control Update(Control control, Scope scope, Type type, object value, IReadOnlyList<Display> displays);
    }
}
