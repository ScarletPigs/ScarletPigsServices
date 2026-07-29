using System.Text;
using ScarletPigsServices.Api.Services.Ocap;

namespace ScarletPigsServices.Api.Tests;

public sealed class OcapManifestReaderTests
{
    [Fact]
    public void Read_UsesTheManifestUtcStartAndOperationDuration()
    {
        var manifest = BuildManifest(
            "2026-07-28T19:05:00Z",
            endFrame: 120,
            captureDelayMilliseconds: 1000);

        var range = OcapManifestReader.Read(manifest, missionDurationSeconds: 7200);

        Assert.NotNull(range);
        Assert.Equal(
            new DateTimeOffset(2026, 7, 28, 19, 5, 0, TimeSpan.Zero),
            range.StartsAt);
        Assert.Equal(range.StartsAt.AddHours(2), range.EndsAt);
    }

    [Fact]
    public void Read_FallsBackToFrameTimingWhenDurationIsMissing()
    {
        var manifest = BuildManifest(
            "2026-07-28T19:05:00Z",
            endFrame: 120,
            captureDelayMilliseconds: 500);

        var range = OcapManifestReader.Read(manifest, missionDurationSeconds: 0);

        Assert.NotNull(range);
        Assert.Equal(range.StartsAt.AddMinutes(1), range.EndsAt);
    }

    [Fact]
    public void Read_ReturnsNullWhenTheManifestHasNoUtcTimeSample()
    {
        var manifest = BuildManifest(
            systemTimeUtc: null,
            endFrame: 120,
            captureDelayMilliseconds: 500);

        Assert.Null(OcapManifestReader.Read(manifest, missionDurationSeconds: 60));
    }

    private static byte[] BuildManifest(
        string? systemTimeUtc,
        ulong endFrame,
        ulong captureDelayMilliseconds)
    {
        using var manifest = new MemoryStream();
        WriteVarintField(manifest, 4, endFrame);
        WriteVarintField(manifest, 6, captureDelayMilliseconds);

        if (systemTimeUtc is not null)
        {
            using var timeSample = new MemoryStream();
            WriteStringField(timeSample, 2, systemTimeUtc);
            WriteBytesField(manifest, 9, timeSample.ToArray());
        }

        return manifest.ToArray();
    }

    private static void WriteVarintField(Stream output, int fieldNumber, ulong value)
    {
        WriteVarint(output, (ulong)(fieldNumber << 3));
        WriteVarint(output, value);
    }

    private static void WriteStringField(Stream output, int fieldNumber, string value)
    {
        WriteBytesField(output, fieldNumber, Encoding.UTF8.GetBytes(value));
    }

    private static void WriteBytesField(Stream output, int fieldNumber, byte[] value)
    {
        WriteVarint(output, (ulong)((fieldNumber << 3) | 2));
        WriteVarint(output, (ulong)value.Length);
        output.Write(value);
    }

    private static void WriteVarint(Stream output, ulong value)
    {
        while (value >= 0x80)
        {
            output.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }

        output.WriteByte((byte)value);
    }
}
