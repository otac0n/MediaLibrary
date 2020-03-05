// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Windows.Forms;
    using MediaLibrary.Storage;

    public partial class PersonControl : UserControl
    {
        private bool indeterminate;
        private Person person;

        public PersonControl()
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
        public Person Person
        {
            get
            {
                return this.person;
            }

            set
            {
                this.person = value;
                if (value != null)
                {
                    this.personName.Text = value.Name;
                }
                else
                {
                    this.personName.Text = "?";
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.DeleteClick?.Invoke(this, e);
        }
    }
}
