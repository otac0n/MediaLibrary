// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    internal static class ControlHelpers
    {
        /// <summary>
        /// Attach a <see cref="ContextMenuStrip"/> to a button.
        /// </summary>
        /// <param name="button">The button that will activate the context menu.</param>
        /// <param name="menu">The context menu that the button will activate.</param>
        /// <param name="container">The container in which to put into the component.</param>
        public static void AttachDropDownMenu(this Button button, ContextMenuStrip menu, IContainer container)
        {
            Construct(
                () => button.AttachDropDownMenu(menu),
                container.Add);
        }

        /// <summary>
        /// Attach a <see cref="ContextMenuStrip"/> to a button.
        /// </summary>
        /// <param name="button">The button that will activate the context menu.</param>
        /// <param name="menu">The context menu that the button will activate.</param>
        /// <returns>A disposable that can be used to remove the context menu.</returns>
        public static IComponent AttachDropDownMenu(this Button button, ContextMenuStrip menu)
        {
            void Click(object sender, EventArgs e) => menu.PopUnder((Control)sender);

            void PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
            {
                if (e.KeyCode == Keys.Down && e.Modifiers == Keys.None)
                {
                    e.IsInputKey = true;
                }
            }

            void KeyDown(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Down && e.Modifiers == Keys.None)
                {
                    e.Handled = true;
                    menu.PopUnder((Control)sender, focusFirstItem: true);
                }
            }

            button.Click += Click;
            button.PreviewKeyDown += PreviewKeyDown;
            button.KeyDown += KeyDown;

            void Dispose()
            {
                button.Click -= Click;
                button.PreviewKeyDown -= PreviewKeyDown;
                button.KeyDown -= KeyDown;
            }

            return new ActionDisposableComponent(Dispose)
            {
                Site = button.Site,
            };
        }

        /// <summary>
        /// A utility to construct a control or other disposable, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Action<TControl> update)
            where TControl : class, IDisposable, new() =>
                Construct(() => new TControl(), update);

        /// <summary>
        /// A utility to construct a control or other disposable, disposing of the control if an exception is thrown before completion.
        /// </summary>
        /// <typeparam name="TControl">The type of control being constructed.</typeparam>
        /// <param name="constructor">The parameterless function to construct the control.</param>
        /// <param name="update">The actions atomically performed after the construction of the control.</param>
        /// <returns>The constructed control.</returns>
        public static TControl Construct<TControl>(Func<TControl> constructor, Action<TControl> update)
            where TControl : class, IDisposable
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

        public static void UpdateComponentsCollection<TItem>(this ToolStripItemCollection components, int start, int length, IList<TItem> items, Func<TItem, ToolStripItem> create, Action<ToolStripItem, TItem> update, Action<ToolStripItem> destroy) =>
            components.UpdateComponentsCollection(start, length, items, create, (c, i) => true, update, destroy);

        public static void UpdateComponentsCollection<TItem>(this ToolStripItemCollection components, int start, int length, IList<TItem> items, Func<TItem, ToolStripItem> create, Func<ToolStripItem, TItem, bool> canUpdate, Action<ToolStripItem, TItem> update, Action<ToolStripItem> destroy) =>
            new ListWrapper<IComponent>(components).UpdateComponentsCollection(start, length, items, create, canUpdate, update, destroy);

        public static void UpdateComponentsCollection<TItem, TComponent>(this IList<IComponent> components, int start, int length, IList<TItem> items, Func<TItem, TComponent> create, Action<TComponent, TItem> update, Action<TComponent> destroy)
            where TComponent : IComponent =>
                components.UpdateComponentsCollection(start, length, items, create, (c, i) => true, update, destroy);

        public static void UpdateComponentsCollection<TItem, TComponent>(this IList<IComponent> components, int start, int length, IList<TItem> items, Func<TItem, TComponent> create, Func<TComponent, TItem, bool> canUpdate, Action<TComponent, TItem> update, Action<TComponent> destroy)
            where TComponent : IComponent
        {
            void RemoveAndDestroy(int index, TComponent toDestroy)
            {
                components.RemoveAt(index);
                destroy(toDestroy);
                toDestroy.Dispose();
            }

            var tail = components.Count - (start + length);
            int End() => components.Count - tail;

            var write = start;
            foreach (var item in items)
            {
                var updated = false;
                if (write < End())
                {
                    var child = (TComponent)components[write];
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
                    components.Insert(write, child);
                }

                write++;
            }

            while (write < End())
            {
                var child = (TComponent)components[write];
                RemoveAndDestroy(write, child);
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

        private class ActionDisposable : IDisposable
        {
            private readonly Action dispose;

            public ActionDisposable(Action dispose)
            {
                this.dispose = dispose;
            }

            public event EventHandler Disposed;

            public void Dispose()
            {
                this.dispose();
                this.Disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        private class ActionDisposableComponent : ActionDisposable, IComponent
        {
            public ActionDisposableComponent(Action action)
                : base(action)
            {
            }

            public ISite Site { get; set; }
        }

        private class ListWrapper<T> : IList<T>
        {
            public ListWrapper(IList source)
            {
                this.Source = source;
            }

            public int Count => this.Source.Count;

            public bool IsReadOnly => this.Source.IsReadOnly;

            public IList Source { get; }

            public T this[int index]
            {
                get => (T)this.Source[index];
                set => this.Source[index] = value;
            }

            public void Add(T item) => this.Source.Add(item);

            public void Clear() => this.Source.Clear();

            public bool Contains(T item) => this.Source.Contains(item);

            public void CopyTo(T[] array, int arrayIndex) => this.Source.CopyTo(array, arrayIndex);

            public IEnumerator<T> GetEnumerator()
            {
                foreach (var item in this.Source)
                {
                    yield return (T)item;
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

            public int IndexOf(T item) => this.Source.IndexOf(item);

            public void Insert(int index, T item) => this.Source.Insert(index, item);

            public bool Remove(T item)
            {
                var before = this.Count;
                this.Source.Remove(item);
                return this.Count == before;
            }

            public void RemoveAt(int index) => this.Source.RemoveAt(index);
        }
    }
}
