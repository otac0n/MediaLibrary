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
        /// <summary>
        /// A utility to construct a control, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Action<TControl> update)
            where TControl : Control, new() =>
                Construct(() => new TControl(), update);

        /// <summary>
        /// A utility to construct a control, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="update">The parameterless function to construct the control.</param>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Func<TControl> constructor, Action<TControl> update)
            where TControl : Control
        {
            TControl control = default;
            try
            {
                control = constructor();
                update(control);
                var result = control;
                control = null;
                return result;
            }
            finally
            {
                control?.Dispose();
            }
        }

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

        public static Rectangle GetFormRectangle(this Control control, bool clip = true)
        {
            if (control is Form form)
            {
                return form.ClientRectangle;
            }
            else if (control.Parent is Control parent)
            {
                var rect = parent.GetFormRectangle(clip);
                var size = control.Size;
                var position = new Point(
                    control.Left,
                    control.Top);
                rect.Offset(position);

                if (clip)
                {
                    var bounds = parent.ClientSize;
                    size = new Size(
                        Math.Min(size.Width, Math.Max(0, bounds.Width - position.X)),
                        Math.Min(size.Height, Math.Max(0, bounds.Height - position.Y)));
                }

                rect.Size = size;

                return rect;
            }
            else
            {
                throw new InvalidOperationException();
            }
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

        public static void PopUnder(this ContextMenuStrip contextMenu, Control control, bool focusFirstItem = false)
        {
            var offset = new Point(0, control.Height);
            contextMenu.Show(control, offset);
            if (focusFirstItem && contextMenu.Items.Count > 0)
            {
                contextMenu.Items[0].Select();
            }
        }

        public static void UpdateControlsCollection<TItem, TControl>(this Control control, IList<TItem> items, Func<TItem, TControl> create, Action<TControl, TItem> update, Action<TControl> destroy)
            where TControl : Control
        {
            control.UpdateControlsCollection(items, create, (c, i) => true, update, destroy);
        }

        public static void UpdateControlsCollection<TItem, TControl>(this Control control, IList<TItem> items, Func<TItem, TControl> create, Func<TControl, TItem, bool> canUpdate, Action<TControl, TItem> update, Action<TControl> destroy)
            where TControl : Control
        {
            void RemoveAndDestroy(int index, TControl toDestroy)
            {
                control.Controls.RemoveAt(index);
                destroy(toDestroy);
                (toDestroy as IDisposable)?.Dispose();
            }

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
                    foreach (var item in items)
                    {
                        var updated = false;
                        if (write < control.Controls.Count)
                        {
                            var child = (TControl)control.Controls[write];
                            if (canUpdate(child, item))
                            {
                                update(child, item);
                                updated = true;
                            }
                            else
                            {
                                RemoveAndDestroy(write, child);
                            }
                        }

                        if (!updated)
                        {
                            var child = create(item);
                            update(child, item);
                            control.Controls.Add(child);
                            control.Controls.SetChildIndex(child, write);
                        }

                        write++;
                    }

                    while (write < control.Controls.Count)
                    {
                        var child = (TControl)control.Controls[write];
                        RemoveAndDestroy(write, child);
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
