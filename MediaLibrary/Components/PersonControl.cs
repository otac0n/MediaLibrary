// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Components
{
    using System;
    using System.ComponentModel;
    using System.Drawing;
    using System.Threading;
    using System.Windows.Forms;
    using MediaLibrary.Services;
    using MediaLibrary.Storage;

    public partial class PersonControl : UserControl
    {
        private Alias alias;
        private int iconVersion;
        private bool indeterminate;
        private Person person;

        public PersonControl()
        {
            this.InitializeComponent();
        }

        public event EventHandler DeleteClick;

        [SettingsBindable(true)]
        [Browsable(true)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        [EditorBrowsable(EditorBrowsableState.Always)]
        public Alias Alias
        {
            get
            {
                return this.alias;
            }

            set
            {
                this.alias = value;
                if (value != null)
                {
                    this.person = null;
                    this.personName.Text = value.Name;
                    this.UpdateIcon(value.Site);
                }
                else if (this.person == null)
                {
                    this.personName.Text = "?";
                }
            }
        }

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
                    this.alias = null;
                    this.personName.Text = value.Name;
                    this.UpdateIcon(null);
                }
                else if (this.alias == null)
                {
                    this.personName.Text = "?";
                }
            }
        }

        private void DeleteButton_Click(object sender, EventArgs e)
        {
            this.DeleteClick?.Invoke(this, e);
        }

        private void Person_Click(object sender, EventArgs e)
        {
            this.OnClick(e);
        }

        private void Person_DoubleClick(object sender, EventArgs e)
        {
            this.OnDoubleClick(e);
        }

        private void Person_MouseClick(object sender, MouseEventArgs e)
        {
            this.OnMouseClick(e);
        }

        private void Person_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.OnMouseDoubleClick(e);
        }

        private async void UpdateIcon(string site)
        {
            var version = Interlocked.Increment(ref this.iconVersion);
            this.personPicture.Image = Properties.Resources.single_neutral;
            if (site != null)
            {
                Image favicon;

                try
                {
                    favicon = await FaviconCache.GetFavicon(new UriBuilder("http", site).Uri).ConfigureAwait(true);
                }
                catch
                {
                    favicon = null;
                }

                if (favicon != null && this.iconVersion == version && !this.IsDisposed)
                {
                    this.personPicture.Image = favicon;
                }
            }
        }
    }
}
