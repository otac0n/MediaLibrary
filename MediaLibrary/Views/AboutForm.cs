// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Views
{
    using System.Diagnostics;
    using System.Reflection;
    using System.Windows.Forms;

    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            this.InitializeComponent();
            this.versionLabel.Text = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
        }

        private void IconAttributionLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start(@"https://www.streamlineicons.com/");
        }
    }
}
