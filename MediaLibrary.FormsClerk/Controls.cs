// Copyright © John & Katie Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.FormsClerk
{
    using System.Linq;
    using System.Windows.Forms;

    public static class Controls
    {
        public static Control AddMargin(this Control control, int left = 0, int top = 0, int right = 0, int bottom = 0)
        {
            control.Margin = new Padding(
                control.Margin.Left + left,
                control.Margin.Top + top,
                control.Margin.Right + right,
                control.Margin.Bottom + bottom);
            return control;
        }

        public static void DisposeAndClear(this Control.ControlCollection controls)
        {
            var list = controls.Cast<Control>().ToList();
            controls.Clear();
            list.ForEach(c => c.Dispose());
        }

        public static FlowLayoutPanel MakeFlowPanel(object tag = null)
        {
            var flowLayoutPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                Margin = Padding.Empty,
                Tag = tag,
            };

            flowLayoutPanel.Disposed += (sender, args) =>
            {
                flowLayoutPanel.Controls.DisposeAndClear();
            };

            return flowLayoutPanel;
        }

        public static Label MakeLabel(string text, object tag = null) => new Label
        {
            Text = text,
            AutoSize = true,
            Margin = Padding.Empty,
            Tag = tag,
        };

        public static TableLayoutPanel MakeTablePanel(int rows, int columns, object tag = null)
        {
            var tableLayoutPanel = new TableLayoutPanel
            {
                AutoSize = true,
                RowCount = rows,
                ColumnCount = columns,
                Margin = Padding.Empty,
                Tag = tag,
            };

            tableLayoutPanel.Disposed += (sender, args) =>
            {
                tableLayoutPanel.Controls.DisposeAndClear();
            };

            return tableLayoutPanel;
        }
    }
}
