// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk.Displays
{
    using System;
    using System.Collections.Generic;
    using System.Windows.Forms;
    using static Controls;

    public class NullDisplay : Display
    {
        private NullDisplay()
        {
        }

        public static NullDisplay Instance { get; } = new NullDisplay();

        public override bool CanDisplay(Scope scope, Type type, object value) => value is null;

        protected override Control Update(Control originalDisplay, Scope scope, Type type, object value, IReadOnlyList<Display> displays) =>
            originalDisplay is Label label && label.Tag == this
                ? originalDisplay
                : MakeLabel("(null)", tag: this);
    }
}
