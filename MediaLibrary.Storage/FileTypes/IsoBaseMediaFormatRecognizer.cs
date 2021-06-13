namespace MediaLibrary.Storage.FileTypes
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <remarks>
    /// https://www.iso.org/standard/74428.html
    /// </remarks>
    public static class IsoBaseMediaFormatRecognizer
    {
        private static readonly Encoding OneByteEncoding = Encoding.GetEncoding(28591);

        private static readonly PropertyParserList<MediaFile> PropertyTags = new PropertyParserList<MediaFile>
        {
            { Properties.Duration, FindValue(new[] { "moov", "mvhd" }, (MovieHeaderBox box) => box.DurationInSeconds) },
            { Properties.Width, FindValue(new[] { "moov", "trak", "tkhd" }, (TrackHeaderBox box) => box.Width) },
            { Properties.Height, FindValue(new[] { "moov", "trak", "tkhd" }, (TrackHeaderBox box) => box.Height) },
        };

        private static Dictionary<string, Func<BoxInfo, Stream, Box>> BoxTypes = new Dictionary<string, Func<BoxInfo, Stream, Box>>()
        {
            ["ftyp"] = FileTypeBox.Read,
            ["mdia"] = SimpleContainerBox.Read,
            ["minf"] = SimpleContainerBox.Read,
            ["moov"] = SimpleContainerBox.Read,
            ["mvhd"] = MovieHeaderBox.Read,
            ["trak"] = SimpleContainerBox.Read,
            ["tkhd"] = TrackHeaderBox.Read,
        };

        private interface IBoxContainer
        {
            ImmutableList<Box> Boxes { get; }
        }

        public static Dictionary<string, object> Recognize(Stream stream) => PropertyTags.Recognize(MediaFile.Read(stream));

        private static IEnumerable<Box> EnumerateBoxes(Stream stream, ulong? limit)
        {
            Box box;
            while ((box = Box.Read(stream, limit)) != null)
            {
                yield return box;
                if (box.BoxInfo.Size is ulong size)
                {
                    limit -= size;
                    stream.Seek((long)(box.BoxInfo.Offset + size), SeekOrigin.Begin);
                }
                else
                {
                    yield break;
                }
            }
        }

        private static PropertyGetter<MediaFile, TValue> FindValue<TBox, TValue>(string[] boxPath, Func<TBox, TValue> getter)
        {
            return (MediaFile mediaFile, out TValue value) =>
            {
                var box = boxPath.Aggregate(
                    new object[] { mediaFile }.AsEnumerable(),
                    (nodes, type) => nodes.OfType<IBoxContainer>().SelectMany(b => b.Boxes).Where(b => b.BoxInfo.Type == type))
                    .OfType<TBox>()
                    .FirstOrDefault();

                if (box != null)
                {
                    value = getter(box);
                    return true;
                }
                else
                {
                    value = default;
                    return false;
                }
            };
        }

        public static class Properties
        {
            public static readonly string Duration = nameof(Duration);
            public static readonly string Height = nameof(Height);
            public static readonly string Width = nameof(Width);
        }

        private static class BigEndian
        {
            public static short ToInt16(byte[] buffer, int offset) =>
                (short)((buffer[offset] << 8) | buffer[offset + 1]);

            public static int ToInt32(byte[] buffer, int offset) =>
                (buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3];

            public static long ToInt64(byte[] buffer, int offset) =>
                ((long)((buffer[offset] << 24) | (buffer[offset + 1] << 16) | (buffer[offset + 2] << 8) | buffer[offset + 3]) << 32) |
                ((long)((buffer[offset + 4] << 24) | (buffer[offset + 5] << 16) | (buffer[offset + 6] << 8) | buffer[offset + 7]));

            public static ushort ToUInt16(byte[] buffer, int offset) =>
                (ushort)ToInt16(buffer, offset);

            public static uint ToUInt32(byte[] buffer, int offset) =>
                (uint)ToInt32(buffer, offset);

            public static ulong ToUInt64(byte[] buffer, int offset) =>
                (uint)ToInt64(buffer, offset);
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} (box of size {BoxInfo.Size,nq})")]
        private class Box
        {
            public Box(BoxInfo boxInfo)
            {
                this.BoxInfo = boxInfo;
            }

            public BoxInfo BoxInfo { get; }

            public static Box Read(Stream stream, ulong? limit)
            {
                var boxInfo = BoxInfo.Read(stream, limit);
                if (boxInfo == null || boxInfo.Size > limit)
                {
                    return null;
                }

                if (BoxTypes.TryGetValue(boxInfo.Type, out var read))
                {
                    var box = read(boxInfo, stream);
                    if (box != null)
                    {
                        return box;
                    }
                }

                return new Box(boxInfo);
            }

            public static bool ReadSafe(Stream stream, byte[] buffer, int offset, int count, ulong? limit, ref ulong read)
            {
                if (!(limit < read + (ulong)count) && stream.Read(buffer, offset, count) == count)
                {
                    read += (ulong)count;
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        [DebuggerDisplay("{Type,nq} (size {Size,nq})")]
        private sealed class BoxInfo
        {
            private BoxInfo(ulong offset, ulong dataOffset, ulong? size, string type)
            {
                this.Offset = offset;
                this.DataOffset = dataOffset;
                this.Size = size;
                this.Type = type;
            }

            public ulong DataOffset { get; }

            public ulong? DataSize => this.Size - (this.DataOffset - this.Offset);

            public ulong Offset { get; }

            public ulong? Size { get; }

            public string Type { get; }

            public static BoxInfo Read(Stream stream, ulong? limit)
            {
                var read = 0UL;

                var buffer = new byte[16];
                var offset = (ulong)stream.Position;
                if (!Box.ReadSafe(stream, buffer, 0, sizeof(uint) * 2, limit, ref read))
                {
                    return null;
                }

                var size = (ulong)BigEndian.ToUInt32(buffer, 0);
                var type = OneByteEncoding.GetString(buffer, sizeof(uint), sizeof(uint));

                if (size == 1)
                {
                    if (!Box.ReadSafe(stream, buffer, 0, sizeof(ulong), limit, ref read))
                    {
                        return null;
                    }

                    size = BigEndian.ToUInt64(buffer, 0);
                }

                if (type == "uuid")
                {
                    if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, limit, ref read))
                    {
                        return null;
                    }

                    type = new Guid(buffer).ToString();
                }

                return new BoxInfo(offset, offset + read, size == 0 ? default(ulong?) : size, type);
            }
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} ({MajorBrand,nq} v{MinorVersion,nq})")]
        private class FileTypeBox : Box
        {
            public FileTypeBox(BoxInfo boxInfo, string majorBrand, uint minorVersion, ImmutableList<string> compatibleBrands)
                : base(boxInfo)
            {
                this.MajorBrand = majorBrand;
                this.MinorVersion = minorVersion;
                this.CompatibleBrands = compatibleBrands;
            }

            public ImmutableList<string> CompatibleBrands { get; }

            public string MajorBrand { get; }

            public uint MinorVersion { get; }

            public static FileTypeBox Read(BoxInfo boxInfo, Stream stream)
            {
                var buffer = new byte[8];

                var read = 0UL;
                string majorBrand = null;
                uint minorVersion = 0;
                var compatibleBrands = ImmutableList<string>.Empty;
                if (Box.ReadSafe(stream, buffer, 0, sizeof(uint) * 2, boxInfo.DataSize, ref read))
                {
                    majorBrand = OneByteEncoding.GetString(buffer, 0, sizeof(uint));
                    minorVersion = BigEndian.ToUInt32(buffer, sizeof(uint));
                    while (Box.ReadSafe(stream, buffer, 0, sizeof(uint), boxInfo.DataSize, ref read))
                    {
                        compatibleBrands = compatibleBrands.Add(OneByteEncoding.GetString(buffer, 0, sizeof(uint)));
                    }
                }

                return new FileTypeBox(boxInfo, majorBrand, minorVersion, compatibleBrands);
            }
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} (v{Version,nq})")]
        private class FullBox : Box
        {
            public FullBox(BoxInfo boxInfo, byte version, int flags)
                : base(boxInfo)
            {
                this.Version = version;
                this.Flags = flags;
            }

            public int Flags { get; }

            public byte Version { get; }

            protected static bool ReadVersion(Stream stream, ulong? limit, ref ulong read, out byte version, out int flags)
            {
                var buffer = new byte[sizeof(uint)];
                if (Box.ReadSafe(stream, buffer, 0, buffer.Length, limit, ref read))
                {
                    version = buffer[0];
                    buffer[0] = 0;
                    flags = BigEndian.ToInt32(buffer, 0);
                    return true;
                }
                else
                {
                    version = default;
                    flags = default;
                    return false;
                }
            }
        }

        private sealed class MediaFile : IBoxContainer
        {
            public MediaFile(ImmutableList<Box> boxes)
            {
                this.Boxes = boxes;
            }

            public ImmutableList<Box> Boxes { get; }

            public static MediaFile Read(Stream stream)
            {
                var boxes = EnumerateBoxes(stream, limit: null).ToImmutableList();
                return new MediaFile(boxes);
            }
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} ({TotalDuration,nq})")]
        private class MovieHeaderBox : FullBox
        {
            public MovieHeaderBox(BoxInfo boxInfo, byte version, int flags, ulong creationTime, ulong modificationTime, uint timescale, ulong duration)
                : base(boxInfo, version, flags)
            {
                this.CreationTime = creationTime;
                this.ModificationTime = modificationTime;
                this.Timescale = timescale;
                this.Duration = duration;
            }

            public ulong CreationTime { get; }

            public ulong Duration { get; }

            public double DurationInSeconds => this.Duration / (double)this.Timescale;

            public ulong ModificationTime { get; }

            public uint Timescale { get; }

            public static MovieHeaderBox Read(BoxInfo boxInfo, Stream stream)
            {
                var read = 0UL;
                if (!FullBox.ReadVersion(stream, boxInfo.DataSize, ref read, out var version, out var flags))
                {
                    return null;
                }

                byte[] buffer;
                ulong creationTime, modificationTime, duration;
                uint timescale;
                switch (version)
                {
                    case 0:
                        buffer = new byte[sizeof(uint) * 4];
                        if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, boxInfo.DataSize, ref read))
                        {
                            return null;
                        }

                        creationTime = BigEndian.ToUInt32(buffer, 0);
                        modificationTime = BigEndian.ToUInt32(buffer, sizeof(uint) * 1);
                        timescale = BigEndian.ToUInt32(buffer, sizeof(uint) * 2);
                        duration = BigEndian.ToUInt32(buffer, sizeof(uint) * 3);
                        break;

                    case 1:
                        buffer = new byte[sizeof(ulong) * 3 + sizeof(uint)];
                        if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, boxInfo.DataSize, ref read))
                        {
                            return null;
                        }

                        creationTime = BigEndian.ToUInt64(buffer, 0);
                        modificationTime = BigEndian.ToUInt64(buffer, sizeof(ulong) * 1);
                        timescale = BigEndian.ToUInt32(buffer, sizeof(ulong) * 2);
                        duration = BigEndian.ToUInt64(buffer, sizeof(ulong) * 2 + sizeof(uint));
                        break;

                    default:
                        return null;
                }

                return new MovieHeaderBox(boxInfo, version, flags, creationTime, modificationTime, timescale, duration);
            }
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} (count = {Boxes.Count,nq})")]
        private class SimpleContainerBox : Box, IBoxContainer
        {
            public SimpleContainerBox(BoxInfo boxInfo, ImmutableList<Box> boxes)
                : base(boxInfo)
            {
                this.Boxes = boxes;
            }

            public ImmutableList<Box> Boxes { get; }

            public static SimpleContainerBox Read(BoxInfo boxInfo, Stream stream)
            {
                var boxes = EnumerateBoxes(stream, boxInfo.DataSize).ToImmutableList();
                return new SimpleContainerBox(boxInfo, boxes);
            }
        }

        [DebuggerDisplay("{BoxInfo.Type,nq} (#{TrackId,nq}, {Width,nq}x{Height,nq})")]
        private class TrackHeaderBox : FullBox
        {
            public TrackHeaderBox(BoxInfo boxInfo, byte version, int flags, ulong creationTime, ulong modificationTime, uint trackId, ulong duration, ushort layer, ushort alternateGroup, ushort volume, double width, double height)
                : base(boxInfo, version, flags)
            {
                this.CreationTime = creationTime;
                this.ModificationTime = modificationTime;
                this.TrackId = trackId;
                this.Duration = duration;
                this.Layer = layer;
                this.AlternateGroup = alternateGroup;
                this.Volume = volume;
                this.Width = width;
                this.Height = height;
            }

            public ushort AlternateGroup { get; }

            public ulong CreationTime { get; }

            public ulong Duration { get; }

            public double Height { get; }

            public ushort Layer { get; }

            public ulong ModificationTime { get; }

            public uint TrackId { get; }

            public ushort Volume { get; }

            public double Width { get; }

            public static TrackHeaderBox Read(BoxInfo boxInfo, Stream stream)
            {
                var read = 0UL;
                if (!FullBox.ReadVersion(stream, boxInfo.DataSize, ref read, out var version, out var flags))
                {
                    return null;
                }

                byte[] buffer;
                ulong creationTime, modificationTime, duration;
                uint trackId;
                ushort layer, alternateGroup, volume;
                double width, height;
                switch (version)
                {
                    case 0:
                        buffer = new byte[sizeof(uint) * 5];
                        if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, boxInfo.DataSize, ref read))
                        {
                            return null;
                        }

                        creationTime = BigEndian.ToUInt32(buffer, 0);
                        modificationTime = BigEndian.ToUInt32(buffer, sizeof(uint) * 1);
                        trackId = BigEndian.ToUInt32(buffer, sizeof(uint) * 2);
                        duration = BigEndian.ToUInt32(buffer, sizeof(uint) * 4);
                        break;

                    case 1:
                        buffer = new byte[sizeof(ulong) * 3 + sizeof(uint) * 2];
                        if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, boxInfo.DataSize, ref read))
                        {
                            return null;
                        }

                        creationTime = BigEndian.ToUInt64(buffer, 0);
                        modificationTime = BigEndian.ToUInt64(buffer, sizeof(ulong) * 1);
                        trackId = BigEndian.ToUInt32(buffer, sizeof(ulong) * 2);
                        duration = BigEndian.ToUInt64(buffer, sizeof(ulong) * 2 + sizeof(uint) * 2);
                        break;

                    default:
                        return null;
                }

                buffer = new byte[sizeof(uint) * 13 + sizeof(ushort) * 4];
                if (!Box.ReadSafe(stream, buffer, 0, buffer.Length, boxInfo.DataSize, ref read))
                {
                    return null;
                }

                layer = BigEndian.ToUInt16(buffer, sizeof(uint) * 2);
                alternateGroup = BigEndian.ToUInt16(buffer, sizeof(uint) * 2 + sizeof(ushort));
                volume = BigEndian.ToUInt16(buffer, sizeof(uint) * 2 + sizeof(ushort) * 2);
                width = (double)BigEndian.ToUInt32(buffer, sizeof(uint) * 11 + sizeof(ushort) * 4) / 0x10000;
                height = (double)BigEndian.ToUInt32(buffer, sizeof(uint) * 12 + sizeof(ushort) * 4) / 0x10000;

                return new TrackHeaderBox(boxInfo, version, flags, creationTime, modificationTime, trackId, duration, layer, alternateGroup, volume, width, height);
            }
        }
    }
}
