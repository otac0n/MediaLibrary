// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.IO;
    using System.Windows.Forms;
    using CommandLine;
    using MediaLibrary.Storage;
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

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm(index));
            return 0;
        }

        private static void PopulateDefaults(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexPath))
            {
                options.IndexPath = Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify), "MediaLibrary", "MediaLibrary.db");
            }
        }

        public class Options
        {
            [Option('p', "index-path", HelpText = "The path of the index database.")]
            public string IndexPath { get; set; }
        }
    }
}
