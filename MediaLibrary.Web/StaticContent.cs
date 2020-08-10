// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Web
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Reflection;

    public static class StaticContent
    {
        public static Assembly ContentAssembly = Assembly.GetExecutingAssembly();

        public static Stream GetContent(string path = null)
        {
            path = path ?? "index.html";
            path = path.ToLowerInvariant().Replace('\\', '.').Replace('/', '.');
            return ContentAssembly.GetManifestResourceStream($"MediaLibrary.Web.{path}");
        }
    }
}
