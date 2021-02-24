// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    internal static class ControlHelpers
    {
        public static int? FindTabIndex(this TabControl tabControl, Point location)
        {
            for (var i = 0; i < tabControl.TabCount; i++)
            {
                if (tabControl.GetTabRect(i).Contains(location))
                {
                    return i;
                }
            }

            return null;
        }

        /// <summary>
        /// Extension method allowing conditional invoke usage.
        /// </summary>
        /// <param name="this">The object with which to synchronize.</param>
        /// <param name="action">The action to perform.</param>
        public static void InvokeIfRequired(this ISynchronizeInvoke @this, MethodInvoker action)
        {
            if (@this.InvokeRequired)
            {
                try
                {
                    @this.Invoke(action, Array.Empty<object>());
                }
                catch (ObjectDisposedException)
                {
                }
            }
            else
            {
                action();
            }
        }

        public static void PopUnder(this ContextMenuStrip contextMenu, Control control)
        {
            var offset = new Point(0, control.Height);
            contextMenu.Show(control, offset);
        }

        public static void UpdateControlsCollection<TItem, TControl>(this Control control, IList<TItem> items, Func<TControl> create, Action<TControl> destroy, Action<TControl, TItem> update)
            where TControl : Control
        {
            control.SuspendLayout();
            try
            {
                if (items.Count == 0)
                {
                    foreach (var child in control.Controls)
                    {
                        (child as IDisposable)?.Dispose();
                    }

                    control.Controls.Clear();
                }
                else
                {
                    var write = 0;
                    foreach (var tag in items)
                    {
                        if (write < control.Controls.Count)
                        {
                            var child = (TControl)control.Controls[write];
                            update(child, tag);
                        }
                        else
                        {
                            var child = create();
                            update(child, tag);
                            control.Controls.Add(child);
                        }

                        write++;
                    }

                    while (write < control.Controls.Count)
                    {
                        var child = (TControl)control.Controls[write];
                        control.Controls.RemoveAt(write);
                        (child as IDisposable)?.Dispose();
                        destroy(child);
                    }
                }
            }
            finally
            {
                control.ResumeLayout();
            }
        }
    }
}
