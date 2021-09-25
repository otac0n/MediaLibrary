// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class TagControl : UserControl
    {
        private bool indeterminate;
        private Color? tagColor;

        public TagControl()
        {
            this.InitializeComponent();
        }

        public event EventHandler DeleteClick;

        [DefaultValue(true)]
        [SettingsBindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public bool AllowDelete
        {
            get => this.deleteButton.Visible;
            set => this.deleteButton.Visible = value;
        }

        [DefaultValue(false)]
        [SettingsBindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public bool Indeterminate
        {
            get => this.indeterminate;

            set
            {
                if (this.indeterminate != value)
                {
                    this.indeterminate = value;
                    this.RefreshColor();
                }
            }
        }

        public Color? TagColor
        {
            get => this.tagColor;

            set
            {
                if (this.tagColor != value)
                {
                    this.tagColor = value;
                    this.RefreshColor();
                    this.Invalidate();
                }
            }
        }

        /// <inheritdoc/>
        [SettingsBindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public override string Text
        {
            get => this.tagName.Text;
            set => this.tagName.Text = base.Text = value;
        }

        /// <inheritdoc/>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (this.indeterminate && this.tagColor is Color borderColor)
            {
                var rect = this.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                using (var pen = new Pen(borderColor))
                {
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
            this.Invalidate();
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.DeleteClick?.Invoke(this, e);
        }

        private void RefreshColor()
        {
            if (this.tagColor is Color tagColor)
            {
                if (this.indeterminate)
                {
                    var blended = ColorService.Blend(0.25, tagColor, SystemColors.ControlLightLight);
                    this.BackColor = blended;
                    this.ForeColor = ColorService.ContrastColor(blended);
                }
                else
                {
                    this.BackColor = tagColor;
                    this.ForeColor = ColorService.ContrastColor(tagColor);
                }
            }
            else
            {
                if (this.indeterminate)
                {
                    this.BackColor = SystemColors.ControlLightLight;
                    this.ForeColor = SystemColors.GrayText;
                }
                else
                {
                    this.BackColor = SystemColors.Info;
                    this.ForeColor = ColorService.ContrastColor(SystemColors.Info);
                }
            }
        }

        private void TagName_Click(object sender, EventArgs e)
        {
            this.OnClick(e);
        }

        private void TagName_DoubleClick(object sender, EventArgs e)
        {
            this.OnDoubleClick(e);
        }

        private void TagName_MouseClick(object sender, MouseEventArgs e)
        {
            this.OnMouseClick(e);
        }

        private void TagName_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.OnMouseDoubleClick(e);
        }
    }
}
