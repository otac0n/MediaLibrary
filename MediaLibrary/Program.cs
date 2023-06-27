// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using CommandLine;
    using MediaLibrary.Storage;
    using MediaLibrary.Web.Hosting;
    using Microsoft.AspNetCore;
    using Microsoft.AspNetCore.Hosting;
    using static System.Environment;

    internal class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            Options options = null;
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    PopulateDefaults(options = o);
                });

            if (options == null)
            {
                return 1;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(options.IndexPath));

            var index = new MediaIndex(options.IndexPath);

            var builder = WebHost.CreateDefaultBuilder();
            Startup.Build(builder, options.BaseUri, index);

            using (var app = builder.Build())
            {
                var task = app.RunAsync();

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                NativeMethods.SetProcessDpiAwareness(NativeMethods.DpiAwareness.PerMonitorAware);
                Application.Run(new MainForm(index));

                app.StopAsync();
                task.Wait();
                return 0;
            }
        }

        private static void PopulateDefaults(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexPath))
            {
                options.IndexPath = Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify), "MediaLibrary", "MediaLibrary.db");
            }

            if (string.IsNullOrEmpty(options.BaseUri))
            {
                options.BaseUri = "http://localhost:9000/";
            }
        }

        public class Options
        {
            [Option('u', "base-uri", HelpText = "The base URI of the application.")]
            public string BaseUri { get; set; }

            [Option('p', "index-path", HelpText = "The path of the index database.")]
            public string IndexPath { get; set; }
        }
    }
}
