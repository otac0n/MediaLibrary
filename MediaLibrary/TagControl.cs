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
                this.indeterminate = value;
                if (this.indeterminate)
                {
                    this.BackColor = SystemColors.ControlLightLight;
                    this.ForeColor = SystemColors.GrayText;
                }
                else
                {
                    this.BackColor = SystemColors.Info;
                    this.ForeColor = SystemColors.InfoText;
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

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.DeleteClick?.Invoke(this, e);
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
