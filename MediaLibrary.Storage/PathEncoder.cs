// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System.Text;

    internal static class PathEncoder
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
    }
}
