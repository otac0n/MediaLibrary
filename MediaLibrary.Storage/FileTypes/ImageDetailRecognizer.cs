// Copyright © John Gietzen. All Rights Reserved. This source is subject to the MIT license. Please see license.md for more information.

namespace MediaLibrary.Storage.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.Globalization;
    using System.Linq;
    using System.Text;

    public static class ImageDetailRecognizer
    {
        private static readonly PropertyParserList<Image> PropertyTags = new PropertyParserList<Image>
        {
            { Properties.GpsTime, ReadGpsDateTime(PropertyTag.GpsDate, PropertyTag.GpsTime) },
            { Properties.DocumentName, ReadString(PropertyTag.DocumentName) },
            { Properties.ImageDescription, ReadString(PropertyTag.ImageDescription) },
            { Properties.PageName, ReadString(PropertyTag.PageName) },
            { Properties.PageNumber, ReadUInt16(PropertyTag.PageNumber) },
            { Properties.DateTime, ReadDate(PropertyTag.DateTime, PropertyTag.DateTimeSubSecond) },
            { Properties.OriginalDateTime, ReadDate(PropertyTag.DateTimeOriginal, PropertyTag.DateTimeOriginalSubSecond) },
            { Properties.DigitizedDateTime, ReadDate(PropertyTag.DateTimeDigitized, PropertyTag.DateTimeDigitizedSubSecond) },
            { Properties.Artist, ReadString(PropertyTag.Artist) },
            { Properties.ImageTitle, ReadString(PropertyTag.ImageTitle) },
            { Properties.Duration, ReconstructDuration() },
            { Properties.LoopCount, ReadUInt16(PropertyTag.LoopCount) },
            { Properties.Copyright, ReadString(PropertyTag.Copyright) },
            { Properties.MakerNote, ReadString(PropertyTag.MakerNote) },
            { Properties.UserComment, ReadString(PropertyTag.UserComment) },
            { Properties.Width, img => img.Width },
            { Properties.Height, img => img.Height },
            {
                Properties.AverageIntensityHash,
                AverageIntensityHash.GetImageHash
            },
        };

        /// <remarks>
        /// From
        /// <see href="https://exiv2.org/tags.html"/> and
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-constant-property-tags-in-numerical-order"/>.
        /// </remarks>
        private enum PropertyTag
        {
            DocumentName = 0x010D,
            ImageDescription = 0x010E,
            PageName = 0x011D,
            PageNumber = 0x0129,
            Artist = 0x013B,
            Gamma = 0x0301,
            ImageTitle = 0x0320,
            FrameDelay = 0x5100,
            LoopCount = 0x5101,
            Copyright = 0x8298,
            MakerNote = 0x927C,
            UserComment = 0x9286,

            GpsTime = 0x0007,
            GpsDate = 0x001d,

            DateTime = 0x0132,
            DateTimeSubSecond = 0x9290,

            DateTimeOriginal = 0x9003,
            DateTimeOriginalSubSecond = 0x9291,

            DateTimeDigitized = 0x9004,
            DateTimeDigitizedSubSecond = 0x9292,
        }

        /// <summary>
        /// From
        /// <see href="https://docs.microsoft.com/en-us/windows/win32/gdiplus/-gdiplus-constant-image-property-tag-type-constants"/>.
        /// </summary>
        private enum PropertyTagType : short
        {
            /// <summary>
            /// Specifies that the value data member is an array of bytes.
            /// </summary>
            ByteArray = 1,

            /// <summary>
            /// Specifies that the value data member is a null-terminated ASCII string. If you set the type data member of a PropertyItem object to <see cref="ASCII"/>, you should set the length data member to the length of the string including the NULL terminator.For example, the string HELLO would have a length of 6.
            /// </summary>
            ASCII = 2,

            /// <summary>
            /// Specifies that the value data member is an array of unsigned short (16-bit) integers.
            /// </summary>
            UInt16 = 3,

            /// <summary>
            /// Specifies that the value data member is an array of unsigned long (32-bit) integers.
            /// </summary>
            UInt32 = 4,

            /// <summary>
            /// Specifies that the value data member is an array of pairs of unsigned long integers.Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.
            /// </summary>
            URational = 5,

            /// <summary>
            /// Specifies that the value data member is an array of bytes that can hold values of any data type.
            /// </summary>
            Any = 6,

            /// <summary>
            /// Specifies that the value data member is an array of signed long (32-bit) integers.
            /// </summary>
            Int32 = 7,

            /// <summary>
            /// Specifies that the value data member is an array of pairs of signed long integers.Each pair represents a fraction; the first integer is the numerator and the second integer is the denominator.
            /// </summary>
            SRational = 10,
        }

        public static Dictionary<string, object> Recognize(Image image) => PropertyTags.Recognize(image);

        internal static double GetGamma(Image img)
        {
            if (Array.IndexOf(img.PropertyIdList, (int)PropertyTag.Gamma) > -1)
            {
                var gamma = img.GetPropertyItem((int)PropertyTag.Gamma);
                if (gamma != null && gamma.Type == 5 && gamma.Len == 8 && gamma.Len == gamma.Value.Length)
                {
                    var numerator = BitConverter.ToUInt32(gamma.Value, 0);
                    var denominator = BitConverter.ToUInt32(gamma.Value, 4);
                    return (double)numerator / denominator;
                }
            }

            return 1.8;
        }

        private static bool ParseValue(PropertyTagType type, int? length, byte[] value, out object result)
        {
            if (length != null && length.Value != value.Length)
            {
                result = null;
                return false;
            }

            var len = length ?? value.Length;

            switch (type)
            {
                case PropertyTagType.ByteArray:
                case PropertyTagType.ASCII:
                    result = value;
                    return true;

                case PropertyTagType.UInt16:
                    if (len % sizeof(ushort) != 0)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        var array = new ushort[len / sizeof(ushort)];
                        result = array;
                        for (var i = 0; i < len; i += sizeof(ushort))
                        {
                            array[i / sizeof(ushort)] = BitConverter.ToUInt16(value, i); // TODO: Consider endianness.
                        }

                        return true;
                    }

                case PropertyTagType.UInt32:
                    if (len % sizeof(uint) != 0)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        var array = new uint[len / sizeof(uint)];
                        result = array;
                        for (var i = 0; i < len; i += sizeof(uint))
                        {
                            array[i / sizeof(uint)] = BitConverter.ToUInt32(value, i); // TODO: Consider endianness.
                        }

                        return true;
                    }

                case PropertyTagType.URational:
                    if (len % (sizeof(uint) * 2) != 0)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        var array = new FractionUInt32[len / (sizeof(uint) * 2)];
                        result = array;
                        for (var i = 0; i < len / (sizeof(uint) * 2); i += 1)
                        {
                            // TODO: Consider endianness.
                            array[i] = new FractionUInt32(
                                BitConverter.ToUInt32(value, sizeof(uint) * (i * 2)),
                                BitConverter.ToUInt32(value, sizeof(uint) * (i * 2 + 1)));
                        }

                        return true;
                    }

                case PropertyTagType.Int32:
                    if (len % sizeof(int) != 0)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        var array = new int[len / sizeof(int)];
                        result = array;
                        for (var i = 0; i < len; i += sizeof(int))
                        {
                            array[i / sizeof(int)] = BitConverter.ToInt32(value, i); // TODO: Consider endianness.
                        }

                        return true;
                    }

                case PropertyTagType.SRational:
                    if (len % (sizeof(int) * 2) != 0)
                    {
                        result = null;
                        return false;
                    }
                    else
                    {
                        var array = new FractionInt32[len / (sizeof(int) * 2)];
                        result = array;
                        for (var i = 0; i < len / (sizeof(int) * 2); i += sizeof(int))
                        {
                            // TODO: Consider endianness.
                            array[i] = new FractionInt32(
                                BitConverter.ToInt32(value, sizeof(uint) * (i * 2)),
                                BitConverter.ToInt32(value, sizeof(uint) * (i * 2 + 1)));
                        }

                        return true;
                    }

                case PropertyTagType.Any:
                default:
                    result = null;
                    return false;
            }
        }

        private static PropertyGetter<Image, DateTime> ReadDate(PropertyTag dateTag, PropertyTag subSecondTag)
        {
            string[] exifDateFormats =
            {
                "yyyy:MM:dd HH:mm:ss",
                "yyyy:MM:dd HH:mm:ss.",
                "yyyy:MM:dd HH:mm:ss.f",
                "yyyy:MM:dd HH:mm:ss.ff",
                "yyyy:MM:dd HH:mm:ss.fff",
                "yyyy:MM:dd HH:mm:ss.ffff",
                "yyyy:MM:dd HH:mm:ss.fffff",
                "yyyy:MM:dd HH:mm:ss.ffffff",
            };
            var readDate = ReadString(dateTag);
            var readSubSecond = ReadString(subSecondTag);
            return (Image img, out DateTime value) =>
            {
                if (readDate(img, out var dateString))
                {
                    if (readSubSecond(img, out var subSecondString))
                    {
                        dateString += "." + subSecondString;
                    }

                    if (DateTime.TryParseExact(dateString, exifDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out value))
                    {
                        return true;
                    }
                }

                value = default;
                return false;
            };
        }

        private static PropertyGetter<Image, FractionUInt32[]> ReadFractionUInt32Array(PropertyTag tag, int? count = null)
        {
            return (Image img, out FractionUInt32[] value) =>
            {
                if (ReadProperty(img, tag, PropertyTagType.URational, count * 2 * sizeof(uint), out value))
                {
                    return true;
                }

                value = default;
                return false;
            };
        }

        private static PropertyGetter<Image, DateTime> ReadGpsDateTime(PropertyTag dateTag, PropertyTag timeTag)
        {
            string[] gpsDateFormats = { "yyyy:MM:dd" };
            var readDate = ReadString(dateTag);
            var readTime = ReadFractionUInt32Array(timeTag, count: 3);
            return (Image img, out DateTime value) =>
            {
                if (readDate(img, out var dateString))
                {
                    if (DateTime.TryParseExact(dateString, gpsDateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out value))
                    {
                        if (readTime(img, out var timeArray))
                        {
                            value = value
                                .AddHours(timeArray[0].Value)
                                .AddMinutes(timeArray[1].Value)
                                .AddSeconds(timeArray[2].Value);
                        }

                        return true;
                    }
                }

                value = default;
                return false;
            };
        }

        private static bool ReadProperty<T>(Image img, PropertyTag tag, PropertyTagType type, int? length, out T value)
        {
            if (Array.IndexOf(img.PropertyIdList, (int)tag) != -1)
            {
                var property = img.GetPropertyItem((int)tag);
                if (property.Type == (short)type && ParseValue(type, length, property.Value, out var obj) && obj is T asT)
                {
                    value = asT;
                    return true;
                }
            }

            value = default;
            return false;
        }

        private static PropertyGetter<Image, string> ReadString(PropertyTag tag)
        {
            return (Image img, out string value) =>
            {
                if (ReadProperty(img, tag, PropertyTagType.ASCII, null, out byte[] encoded))
                {
                    value = Encoding.UTF8.GetString(encoded).TrimEnd('\0');
                    return true;
                }

                value = default;
                return false;
            };
        }

        private static PropertyGetter<Image, ushort> ReadUInt16(PropertyTag tag)
        {
            return (Image img, out ushort value) =>
            {
                if (ReadProperty(img, tag, PropertyTagType.UInt16, sizeof(ushort), out ushort[] array) && array?.Length == 1)
                {
                    value = array[0];
                    return true;
                }

                value = default;
                return false;
            };
        }

        private static PropertyGetter<Image, uint> ReadUInt32(PropertyTag tag)
        {
            return (Image img, out uint value) =>
            {
                if (ReadProperty(img, tag, PropertyTagType.UInt32, sizeof(uint), out uint[] array) && array?.Length == 1)
                {
                    value = array[0];
                    return true;
                }

                value = default;
                return false;
            };
        }

        private static PropertyGetter<Image, double> ReconstructDuration()
        {
            return (Image image, out double value) =>
            {
                value = default;
                if (image.FrameDimensionsList.Length == 0)
                {
                    return false;
                }

                var dimension = new FrameDimension(image.FrameDimensionsList.First());
                var frames = image.GetFrameCount(dimension);
                if (frames <= 1)
                {
                    return false;
                }

                var minFrameTime = image.RawFormat.Guid == ImageFormat.Gif.Guid ? 1 / 60.0 : 0.0;
                if (!ReadProperty(image, PropertyTag.FrameDelay, PropertyTagType.UInt32, sizeof(uint) * frames, out uint[] frameTimes) || !(frameTimes?.Length == frames))
                {
                    return false;
                }

                var duration = 0.0;
                for (var i = 0; i < frames; i++)
                {
                    duration += Math.Max(frameTimes[i] / 100.0, minFrameTime);
                }

                value = duration;
                return duration != 0;
            };
        }

        private struct FractionInt32
        {
            public FractionInt32(int numerator, int denominator)
            {
                this.Numerator = numerator;
                this.Denominator = denominator;
            }

            public int Denominator { get; }

            public int Numerator { get; }

            public double Value => (double)this.Numerator / this.Denominator;
        }

        private struct FractionUInt32
        {
            public FractionUInt32(uint numerator, uint denominator)
            {
                this.Numerator = numerator;
                this.Denominator = denominator;
            }

            public uint Denominator { get; }

            public uint Numerator { get; }

            public double Value => (double)this.Numerator / this.Denominator;
        }

        public static class Properties
        {
            public static readonly string Artist = "Artist";
            public static readonly string AverageIntensityHash = "AverageIntensityHash";
            public static readonly string Copyright = "Copyright";
            public static readonly string DateTime = "DateTime";
            public static readonly string DigitizedDateTime = "DigitizedDateTime";
            public static readonly string DocumentName = "DocumentName";
            public static readonly string Duration = "Duration";
            public static readonly string GpsTime = "GpsTime";
            public static readonly string Height = "Height";
            public static readonly string ImageDescription = "ImageDescription";
            public static readonly string ImageTitle = "ImageTitle";
            public static readonly string LoopCount = "LoopCount";
            public static readonly string MakerNote = "MakerNote";
            public static readonly string OriginalDateTime = "OriginalDateTime";
            public static readonly string PageName = "PageName";
            public static readonly string PageNumber = "PageNumber";
            public static readonly string UserComment = "UserComment";
            public static readonly string Width = "Width";
        }
    }
}
