using Microsoft.Maui.Controls.Hosting;
using Microsoft.Maui.Hosting;

namespace MediaLibrary.App
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp() =>
            MauiApp.CreateBuilder()
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .Build();
    }
}
