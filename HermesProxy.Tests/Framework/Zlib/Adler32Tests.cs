using System;
using Framework.IO;
using Xunit;

namespace HermesProxy.Tests.Framework.Zlib;

// Locks the current Framework.IO.Zlib Adler32 byte output prior to the planned port from
// the jzlib-derived implementation (Adler32.cs:33-165) to a Span-based replacement. The
// NMAX-boundary and WoW-seed cases capture goldens produced by today's code so the
// replacement must reproduce them bit-for-bit.
public class Adler32Tests
{
    [Fact]
    public void Adler32_EmptyBuffer_ReturnsSeed()
    {
        Assert.Equal(1u, ZLib.adler32(1, Array.Empty<byte>(), 0));
        Assert.Equal(0x9827D8F1u, ZLib.adler32(0x9827D8F1, Array.Empty<byte>(), 0));
    }

    [Fact]
    public void Adler32_NullBuffer_Returns1()
    {
        // The jzlib idiom: passing null requests the spec's initial seed (1).
        Assert.Equal(1u, ZLib.adler32(0, null!, 0));
    }

    [Fact]
    public void Adler32_KnownVector_Wikipedia()
    {
        // Public spec vector — adler32(seed=1, "Wikipedia") = 0x11E60398.
        byte[] input = "Wikipedia"u8.ToArray();
        Assert.Equal(0x11E60398u, ZLib.adler32(1, input, (uint)input.Length));
    }

    [Theory]
    [InlineData(1, 0x003F003Fu)]     // single-byte fast path
    [InlineData(15, 0x36C607CFu)]    // < 16 fast path
    [InlineData(16, 0x3ECF0809u)]    // exactly one unrolled block
    [InlineData(5552, 0xE145C37Fu)]  // exactly NMAX
    [InlineData(5553, 0xA596C442u)]  // NMAX + 1
    [InlineData(11104, 0x7E9F9DF7u)] // 2 * NMAX
    [InlineData(16656, 0x1C3D5848u)] // 3 * NMAX
    public void Adler32_DeterministicRandom_MatchesGolden(int length, uint expectedGolden)
    {
        // Goldens captured from today's ZLib.adler32 (the jzlib port). Phase B's Span
        // replacement must reproduce them without modifying this test.
        byte[] buf = new byte[length];
        new Random(42).NextBytes(buf);
        uint actual = ZLib.adler32(1, buf, (uint)length);
        Assert.Equal(expectedGolden, actual);
    }

    [Fact]
    public void Adler32_OffsetOverload_EquivalentToSlice()
    {
        byte[] buf = new byte[256];
        new Random(7).NextBytes(buf);

        // Whole buffer via the (adler, buf, len) overload …
        uint whole = ZLib.adler32(1, buf, (uint)buf.Length);

        // … must match the (adler, buf, ind, len) overload with ind=0.
        Assert.Equal(whole, ZLib.adler32(1, buf, 0, (uint)buf.Length));

        // Slice consistency: adler over buf[64..] equals adler over a copy of that slice.
        byte[] slice = new byte[buf.Length - 64];
        Array.Copy(buf, 64, slice, 0, slice.Length);
        Assert.Equal(
            ZLib.adler32(1, slice, (uint)slice.Length),
            ZLib.adler32(1, buf, 64, (uint)slice.Length));
    }

    [Theory]
    [InlineData(2, 0x4B0ED9D5u)]     // matches the 2-byte opcode at WorldSocket.cs:471
    [InlineData(64, 0x182AF944u)]
    [InlineData(1024, 0x8E07CD9Eu)]
    [InlineData(8192, 0xF8AFE5DAu)]
    public void Adler32_WoWSeed_MatchesGolden(int length, uint expectedGolden)
    {
        // Locks the SMSG_COMPRESSED_PACKET checksum format (seed = 0x9827D8F1) used at
        // WorldSocket.cs:471,475. WoW's protocol contract — do not adjust.
        byte[] buf = new byte[length];
        new Random(123).NextBytes(buf);
        uint actual = ZLib.adler32(0x9827D8F1, buf, (uint)length);
        Assert.Equal(expectedGolden, actual);
    }
}
