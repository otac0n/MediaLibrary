// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web.Hosting
{
    using System;
    using System.IO;
    using System.Reflection;

    internal static class StaticContent
    {
        public static Assembly ContentAssembly = Assembly.GetExecutingAssembly();

        public static Stream GetContent(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            path = path.ToLowerInvariant().Replace('\\', '.').Replace('/', '.');
            return ContentAssembly.GetManifestResourceStream($"MediaLibrary.Web.{path}");
        }
    }
}
