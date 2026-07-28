using System.Buffers.Binary;
using System.Globalization;
using System.Text;

namespace ScarletPigsServices.Api.Services.Ocap;

internal static class OcapManifestReader
{
    public static OcapRecordingTimeRange? Read(
        ReadOnlySpan<byte> manifest,
        double missionDurationSeconds)
    {
        var offset = 0;
        ulong endFrame = 0;
        ulong captureDelayMilliseconds = 0;
        DateTimeOffset? recordingStart = null;

        while (offset < manifest.Length && TryReadVarint(manifest, ref offset, out var tag))
        {
            var fieldNumber = (int)(tag >> 3);
            var wireType = (int)(tag & 7);

            if (fieldNumber == 4 && wireType == 0)
            {
                if (!TryReadVarint(manifest, ref offset, out endFrame))
                {
                    return null;
                }

                continue;
            }

            if (fieldNumber == 6 && wireType == 0)
            {
                if (!TryReadVarint(manifest, ref offset, out captureDelayMilliseconds))
                {
                    return null;
                }

                continue;
            }

            if (fieldNumber == 9 && wireType == 2)
            {
                if (!TryReadLengthDelimited(manifest, ref offset, out var timeSample))
                {
                    return null;
                }

                var sample = ReadSystemTimeUtc(timeSample);
                if (sample is not null && (recordingStart is null || sample < recordingStart))
                {
                    recordingStart = sample;
                }

                continue;
            }

            if (!TrySkipField(manifest, ref offset, wireType))
            {
                return null;
            }
        }

        if (recordingStart is null)
        {
            return null;
        }

        var duration = missionDurationSeconds > 0 && double.IsFinite(missionDurationSeconds)
            ? TimeSpan.FromSeconds(missionDurationSeconds)
            : TimeSpan.FromMilliseconds(checked((double)endFrame * captureDelayMilliseconds));

        return duration > TimeSpan.Zero
            ? new OcapRecordingTimeRange(recordingStart.Value, recordingStart.Value.Add(duration))
            : null;
    }

    private static DateTimeOffset? ReadSystemTimeUtc(ReadOnlySpan<byte> timeSample)
    {
        var offset = 0;

        while (offset < timeSample.Length && TryReadVarint(timeSample, ref offset, out var tag))
        {
            var fieldNumber = (int)(tag >> 3);
            var wireType = (int)(tag & 7);

            if (fieldNumber == 2 && wireType == 2)
            {
                if (!TryReadLengthDelimited(timeSample, ref offset, out var value))
                {
                    return null;
                }

                var text = Encoding.UTF8.GetString(value);
                return DateTimeOffset.TryParse(
                    text,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var timestamp)
                    ? timestamp
                    : null;
            }

            if (!TrySkipField(timeSample, ref offset, wireType))
            {
                return null;
            }
        }

        return null;
    }

    private static bool TryReadLengthDelimited(
        ReadOnlySpan<byte> input,
        ref int offset,
        out ReadOnlySpan<byte> value)
    {
        value = default;
        if (!TryReadVarint(input, ref offset, out var rawLength) || rawLength > int.MaxValue)
        {
            return false;
        }

        var length = (int)rawLength;
        if (length < 0 || offset > input.Length - length)
        {
            return false;
        }

        value = input.Slice(offset, length);
        offset += length;
        return true;
    }

    private static bool TryReadVarint(ReadOnlySpan<byte> input, ref int offset, out ulong value)
    {
        value = 0;

        for (var shift = 0; shift < 64 && offset < input.Length; shift += 7)
        {
            var current = input[offset++];
            value |= (ulong)(current & 0x7f) << shift;

            if ((current & 0x80) == 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool TrySkipField(ReadOnlySpan<byte> input, ref int offset, int wireType)
    {
        switch (wireType)
        {
            case 0:
                return TryReadVarint(input, ref offset, out _);
            case 1:
                if (offset > input.Length - sizeof(ulong))
                {
                    return false;
                }

                _ = BinaryPrimitives.ReadUInt64LittleEndian(input[offset..]);
                offset += sizeof(ulong);
                return true;
            case 2:
                return TryReadLengthDelimited(input, ref offset, out _);
            case 5:
                if (offset > input.Length - sizeof(uint))
                {
                    return false;
                }

                _ = BinaryPrimitives.ReadUInt32LittleEndian(input[offset..]);
                offset += sizeof(uint);
                return true;
            default:
                return false;
        }
    }
}
