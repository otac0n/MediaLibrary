using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommandLine;
using MediaLibrary.App.ViewModels;
using MediaLibrary.App.Views;
using MediaLibrary.Storage;

namespace MediaLibrary.App
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            Options? options = null;
            var result = Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                .WithParsed(o =>
                {
                    PopulateDefaults(options = o);
                });

            if (options == null)
            {
                throw new ArgumentOutOfRangeException();
            }

            var index = new MediaIndex(options.IndexPath);

            if (this.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel(index),
                };
            }

            base.OnFrameworkInitializationCompleted();
        }

        private static void PopulateDefaults(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexPath))
            {
                options.IndexPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData, Environment.SpecialFolderOption.DoNotVerify), "MediaLibrary", "MediaLibrary.db");
            }

            if (string.IsNullOrEmpty(options.BaseUri))
            {
                options.BaseUri = "http://localhost:9000/";
            }
        }

        public class Options
        {
            [Option('u', "base-uri", HelpText = "The base URI of the application.")]
            public string? BaseUri { get; set; }

            [Option('p', "index-path", HelpText = "The path of the index database.")]
            public string? IndexPath { get; set; }
        }
    }
}
