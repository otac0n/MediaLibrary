// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary
{
    using System;
    using System.IO;
    using CommandLine;
    using MediaLibrary.Storage;
    using static System.Environment;

    internal class Program
    {
        public static int Main(string[] args)
        {
            Options options = null;
            var result = Parser.Default.ParseArguments<Options>(args)
                .WithParsed(o =>
                {
                    PopulateDefaults(options = o);
                })
                .WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.Write(error);
                    }
                });

            if (options == null)
            {
                return 1;
            }

            var path = new MediaIndex(options.IndexPath);
            return 0;
        }

        private static void PopulateDefaults(Options options)
        {
            if (string.IsNullOrWhiteSpace(options.IndexPath))
            {
                options.IndexPath = Path.Combine(Environment.GetFolderPath(SpecialFolder.LocalApplicationData, SpecialFolderOption.DoNotVerify), "MediaLibrary.db");
            }
        }

        public class Options
        {
            [Option('p', "index-path", HelpText = "The path of the index database.")]
            public string IndexPath { get; set; }
        }
    }
}
