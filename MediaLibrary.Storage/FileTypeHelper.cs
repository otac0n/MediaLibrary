// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System;

    public static class FileTypeHelper
    {
        public static bool IsAudio(string fileType) => fileType == "audio" || fileType.StartsWith("audio/", StringComparison.Ordinal);

        public static bool IsImage(string fileType) => fileType == "image" || fileType.StartsWith("image/", StringComparison.Ordinal);

        public static bool IsVideo(string fileType) => fileType == "video" || fileType.StartsWith("video/", StringComparison.Ordinal);
    }
}
