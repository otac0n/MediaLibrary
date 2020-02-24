// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage
{
    using System.Collections.Generic;
    using System.Linq;

    internal sealed class FileTypeRecognizer
    {
        /// <remarks>
        /// From
        /// * https://en.wikipedia.org/wiki/List_of_file_signatures
        /// * https://www.garykessler.net/library/file_sigs.html
        /// </remarks>
        private static readonly List<FileTypeRecognizer> Recognizers = new RecognizerList
        {
            { "image/jpeg",      0xff, 0xd8 },
            { "image/gif",       0x47, 0x49, 0x46, 0x38, 0x37, 0x61 },
            { "image/gif",       0x47, 0x49, 0x46, 0x38, 0x39, 0x61 },
            { "image/png",       0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a },
            { "image/bmp",       0x42, 0x4d },
            { "image/x-icon",    0x00, 0x00, 0x01, 0x00 },
            { "image/tiff",      0x49, 0x49, 0x2a, 0x00 },
            { "image/tiff",      0x4d, 0x4d, 0x00, 0x2a },
            { "image/tiff",      0x43, 0x52 }, // Canon RAW
            { "image",           0x4d, 0x54, 0x68, 0x64 }, // FLIF
            { "image",           0x80, 0x2a, 0x5f, 0xd7 }, // Kodak Cineon
            { "image",           0x53, 0x44, 0x50, 0x58 }, // SMPTE DPX
            { "image",           0x58, 0x50, 0x44, 0x53 }, // SMPTE DPX
            { "image",           0x76, 0x2f, 0x31, 0x01 }, // OpenEXR
            { "image",           0x42, 0x50, 0x47, 0xfb }, // Better Portable Graphics
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x41, 0x43, 0x42, 0x4d }, // Amiga Bitmap
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x46, 0x41, 0x4e, 0x54 }, // Amiga Movie
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x41, 0x4e, 0x42, 0x4d }, // IFF Animated Bitmap
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x49, 0x4c, 0x42, 0x4d }, // IFF Interleaved Bitmap
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x41, 0x4e, 0x49, 0x4d }, // IFF CEL Animation
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x46, 0x41, 0x58, 0x58 }, // IFF FAX
            { "image",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x59, 0x55, 0x56, 0x4e }, // IFF YUV
            { "audio",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x38, 0x53, 0x56, 0x58 }, // IFF Voice
            { "audio",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x53, 0x4d, 0x55, 0x53 }, // IFF Simple Musical Score
            { "audio",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x43, 0x4d, 0x55, 0x53 }, // IFF Musical Score
            { "audio",           0x46, 0x4f, 0x52, 0x4d, null, null, null, null, 0x41, 0x49, 0x46, 0x46 }, // Audio Interchange Format
            { "audio/wav",       0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x57, 0x41, 0x56, 0x45 },
            { "audio/mpeg",      0xff, 0xfb },
            { "audio/mpeg",      0x49, 0x44, 0x33 },
            { "audio/flac",      0x66, 0x4c, 0x61, 0x43 },
            { "audio/midi",      0x4d, 0x54, 0x68, 0x64 },
            { "video/x-msvideo", 0x52, 0x49, 0x46, 0x46, null, null, null, null, 0x41, 0x56, 0x49, 0x20 },
            { "video/mpeg",      0x00, 0x00, 0x01, 0xba },
            { "video/mpeg",      0x00, 0x00, 0x01, 0xb3 },
            { "video/mp4",       null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x4d, 0x53, 0x4e, 0x56 },
            { "video/mp4",       null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x69, 0x73, 0x6f, 0x6d },
            { "video/mp4",       null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x6d, 0x70, 0x34, 0x32 },
            { "video/quicktime", null, null, null, null, 0x66, 0x74, 0x79, 0x70, 0x71, 0x74, 0x20, 0x20 },
            { "video",           0x4d, 0x4c, 0x56, 0x49 }, // Magic Lantern Video

            // Below are container formats that could be used for audio or video. Deeper inspection is required to disambiguate.
            { "video/wmv",      0x30, 0x26, 0xb2, 0x75, 0x8e, 0x66, 0xcf, 0x11, 0xa6, 0xd9, 0x00, 0xaa, 0x00, 0x62, 0xce, 0x6c },
            { "video/webm",     0x1a, 0x45, 0xdf, 0xa3 },
            { "audio/ogg",      0x4f, 0x67, 0x67, 0x53 },
        };

        private FileTypeRecognizer(string type, byte?[] pattern)
        {
            this.Type = type;
            this.Pattern = pattern;
        }

        public byte?[] Pattern { get; }

        public string Type { get; }

        public static void Advance(State[] states, byte[] buffer, int index, int count)
        {
            for (var i = 0; i < states.Length; i++)
            {
                State.Advance(ref states[i], buffer, index, count);
            }
        }

        public static string GetType(State[] states) =>
            string.Join(";", states.Where(s => s.Matches).Select(s => s.Type).Distinct().OrderBy(s => s));

        public static State[] Initialize()
        {
            var states = new State[Recognizers.Count];
            for (var i = 0; i < states.Length; i++)
            {
                states[i] = new State(Recognizers[i]);
            }

            return states;
        }

        public struct State
        {
            private readonly int index;
            private readonly bool matches;
            private readonly FileTypeRecognizer recognizer;

            public State(FileTypeRecognizer recognizer)
                : this(recognizer, 0, true)
            {
            }

            private State(FileTypeRecognizer recognizer, int index, bool matches)
            {
                this.recognizer = recognizer;
                this.index = index;
                this.matches = matches;
            }

            public bool Matches => this.matches && this.index >= this.recognizer.Pattern.Length;

            public string Type => this.recognizer.Type;

            public static void Advance(ref State state, byte[] buffer, int index, int count)
            {
                var patternMatches = state.matches;
                if (!patternMatches)
                {
                    return;
                }

                var recognizer = state.recognizer;
                var pattern = recognizer.Pattern;
                var patternIndex = state.index;
                if (patternIndex >= pattern.Length)
                {
                    return;
                }

                for (; count > 0 && patternMatches && patternIndex < pattern.Length; count--, index++, patternIndex++)
                {
                    var value = pattern[patternIndex];
                    if (value != null)
                    {
                        patternMatches = value == buffer[index];
                    }
                }

                state = new State(recognizer, patternIndex, patternMatches);
            }
        }

        private class RecognizerList : List<FileTypeRecognizer>
        {
            public void Add(string type, params byte?[] pattern)
            {
                this.Add(new FileTypeRecognizer(type, pattern));
            }
        }
    }
}
