using Microsoft.Maui.Controls;

namespace MediaLibrary.App
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            this.MainPage = new AppShell();
        }
    }
}
