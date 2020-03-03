// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;

    public partial class TagControl : UserControl
    {
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
    }
}
