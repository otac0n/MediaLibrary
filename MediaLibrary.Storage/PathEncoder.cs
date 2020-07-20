// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Text;

    public static class PathEncoder
    {
        private const string DeviceNamespacePrefix = @"\\.\";
        private const string FileNamespacePrefix = @"\\?\";
        private const string UncPrefix = FileNamespacePrefix + @"UNC\";

        /// <summary>
        /// Converts a raw byte encoding containing a path into a string without performing any Unicode validation.
        /// </summary>
        /// <param name="input">The raw byte encoding of a path.</param>
        /// <returns>A string matching the originally encoded input.</returns>
        public static string Decode(byte[] input)
        {
            if (input == null)
            {
                return null;
            }

            var sb = new StringBuilder();

            for (var i = 0; i < input.Length; i += 2)
            {
                var c = (char)((input[i] << 8) | input[i + 1]);
                sb.Append(c);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Converts a string into a raw byte encoding without any consideration for Unicode validation rules.
        /// </summary>
        /// <param name="path">The string containing the raw operating system path.</param>
        /// <returns>The raw byte encoding of <paramref name="path"/>.</returns>
        public static byte[] Encode(string path)
        {
            if (path == null)
            {
                return null;
            }

            var result = new byte[path.Length * 2];

            for (var i = 0; i < path.Length; i++)
            {
                var c = path[i];
                result[i * 2] = (byte)(c >> 8);
                result[i * 2 + 1] = (byte)(c & 0xFF);
            }

            return result;
        }

        /// <summary>
        /// Extends a path (if necessary) with the <c>\\.\</c> Win32 file namespace prefix.
        /// </summary>
        /// <param name="path">The path to extend.</param>
        /// <returns>The extended path.</returns>
        public static string ExtendPath(string path)
        {
            if (path == null ||
                path.Length < 260 ||
                path.StartsWith(FileNamespacePrefix, StringComparison.Ordinal) ||
                path.StartsWith(DeviceNamespacePrefix, StringComparison.Ordinal))
            {
                return path;
            }

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return UncPrefix + path.Substring(2);
            }
            else
            {
                return FileNamespacePrefix + path;
            }
        }

        /// <summary>
        /// Overrides the value of <paramref name="path"/> if <paramref name="pathRaw"/> is not <c>null</c>.
        /// </summary>
        /// <param name="path">The (possibly lossy) decoded path.</param>
        /// <param name="pathRaw">The raw byte encoding of <paramref name="path"/>, or <c>null</c> if <paramref name="path"/> is found to round-trip.</param>
        /// <returns>The original path.</returns>
        public static string GetPath(string path, byte[] pathRaw) =>
            pathRaw == null ? path : PathEncoder.Decode(pathRaw);

        /// <summary>
        /// Converts a (possibly invalid) unicode string into a raw byte encoding without any consideration for unicode validation rules.
        /// Returns <c>null</c> if <see cref="Encoding.UTF8"/> will round-trip the value.
        /// </summary>
        /// <param name="path">The string containing the raw operating system path.</param>
        /// <returns><c>null</c>, if the path can be round-tripped; the raw byte encoding of <paramref name="path"/>, otherwise.</returns>
        public static byte[] GetPathRaw(string path) =>
            path != null && path != Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(path))
                ? PathEncoder.Encode(path)
                : null;
    }
}
