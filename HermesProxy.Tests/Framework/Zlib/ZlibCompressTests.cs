using System;
using System.IO;
using System.IO.Compression;
using Framework.IO;
using Xunit;

namespace HermesProxy.Tests.Framework.Zlib;

// Locks the wire bytes produced by ZLib.Compress / ZLib.Decompress (compress.cs:27-65)
// before the planned cleanup of the surrounding jzlib port. The format is:
//   78 9C [raw deflate bytes …] [adler32 of original payload, big-endian]
// Phase B's slim wrapper must reproduce identical bytes for the same input — the format
// is observed by external consumers (the modern client decoding compressed pieces).
public class ZlibCompressTests
{
    [Fact]
    public void Compress_Decompress_RoundtripsByteForByte()
    {
        foreach (int size in new[] { 0, 1, 256, 4096, 65536 })
        {
            byte[] payload = new byte[size];
            new Random(42 + size).NextBytes(payload);

            byte[] compressed = ZLib.Compress(payload);
            byte[] decompressed = ZLib.Decompress(compressed, (uint)size);

            Assert.Equal(payload, decompressed);
        }
    }

    [Fact]
    public void Compress_HasZlibHeader_AndAdlerTrail()
    {
        byte[] payload = "Hello, WoW"u8.ToArray();
        byte[] compressed = ZLib.Compress(payload);

        // 78 9C is the zlib header for default-compression / 32K window with no preset dict.
        Assert.True(compressed.Length >= 6);
        Assert.Equal(0x78, compressed[0]);
        Assert.Equal(0x9C, compressed[1]);

        // Last 4 bytes are big-endian Adler-32 of the original payload.
        uint expectedAdler = ZLib.adler32(1, payload, (uint)payload.Length);
        uint trailingAdler =
            (uint)compressed[^4] << 24 |
            (uint)compressed[^3] << 16 |
            (uint)compressed[^2] << 8 |
            (uint)compressed[^1];
        Assert.Equal(expectedAdler, trailingAdler);
    }

    [Fact]
    public void Compress_KnownInput_MatchesGolden()
    {
        // Wire-format gate. If Phase B's replacement changes deflate level/strategy, this
        // test fires and the swap requires explicit triage. We use the existing
        // System.IO.Compression.DeflateStream that compress.cs already builds on, so the
        // current bytes are what the runtime produces today — not jzlib-specific.
        byte[] payload = "Hello, WoW"u8.ToArray();
        byte[] compressed = ZLib.Compress(payload);
        string actual = Convert.ToHexString(compressed);

        const string Golden = "789CF248CDC9C9D75108CF0F07000000FFFF030012EB035E";
        Assert.Equal(Golden, actual);
    }

    [Fact]
    public void Compress_FixedRandomPayload_MatchesGoldenLength()
    {
        // Random-payload compressed bytes are large; lock the length only as a smoke
        // gate. The Compress_KnownInput_MatchesGolden test above is the byte-equality
        // gate over a smaller payload.
        byte[] payload = new byte[4096];
        new Random(42).NextBytes(payload);
        byte[] compressed = ZLib.Compress(payload);

        Assert.Equal(4114, compressed.Length);
        Assert.Equal(0x78, compressed[0]);
        Assert.Equal(0x9C, compressed[1]);

        // Adler32 trail must round-trip back to plaintext.
        byte[] decompressed = ZLib.Decompress(compressed, 4096);
        Assert.Equal(payload, decompressed);
    }

    [Fact]
    public void Decompress_ProducedByDeflateStream_ReturnsExpectedPayload()
    {
        // Build a zlib stream by hand using only built-in APIs, then verify ZLib.Decompress
        // accepts it. This guards Packet.cs:281 (Inflate) against any header / trailer
        // assumption changes during the refactor.
        byte[] payload = "compressed update block"u8.ToArray();

        using var ms = new MemoryStream();
        ms.WriteByte(0x78);
        ms.WriteByte(0x9C);
        using (var deflater = new DeflateStream(ms, CompressionMode.Compress, leaveOpen: true))
            deflater.Write(payload, 0, payload.Length);
        // Trailing adler in big-endian order.
        uint adler = ZLib.adler32(1, payload, (uint)payload.Length);
        ms.WriteByte((byte)(adler >> 24));
        ms.WriteByte((byte)(adler >> 16));
        ms.WriteByte((byte)(adler >> 8));
        ms.WriteByte((byte)adler);

        byte[] decompressed = ZLib.Decompress(ms.ToArray(), (uint)payload.Length);
        Assert.Equal(payload, decompressed);
    }
}
