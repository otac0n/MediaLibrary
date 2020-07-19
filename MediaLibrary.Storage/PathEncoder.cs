// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;
    using System.Text;

    public static class PathEncoder
    {
        public static string Decode(byte[] input)
        {
            var sb = new StringBuilder();

            for (var i = 0; i < input.Length; i += 2)
            {
                var c = (char)((input[i] << 8) | input[i + 1]);
                sb.Append(c);
            }

            return sb.ToString();
        }

        public static byte[] Encode(string str)
        {
            var result = new byte[str.Length * 2];

            for (var i = 0; i < str.Length; i++)
            {
                var c = str[i];
                result[i * 2] = (byte)(c >> 8);
                result[i * 2 + 1] = (byte)(c & 0xFF);
            }

            return result;
        }

        public static string ExtendPath(string path)
        {
            if (path == null ||
                path.Length < 260 ||
                path.StartsWith(@"\\?\", StringComparison.Ordinal) ||
                path.StartsWith(@"\\.\", StringComparison.Ordinal))
            {
                return path;
            }

            if (path.StartsWith(@"\\", StringComparison.Ordinal))
            {
                return @"\\?\UNC" + path.Substring(1);
            }
            else
            {
                return @"\\?\" + path;
            }
        }

        public static string GetPath(string path, byte[] pathRaw) =>
            pathRaw == null ? path : PathEncoder.Decode(pathRaw);

        public static byte[] GetPathRaw(string path) =>
            path != Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(path))
                ? PathEncoder.Encode(path)
                : null;
    }
}
