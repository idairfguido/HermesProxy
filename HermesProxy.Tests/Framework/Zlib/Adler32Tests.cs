using System;
using Framework.IO;
using Xunit;

namespace HermesProxy.Tests.Framework.Zlib;

// Locks the byte-exact output of the Framework.IO.Adler32 implementation. The
// NMAX-boundary and WoW-seed cases use goldens captured from the original jzlib
// port, so any future change to the algorithm must reproduce them bit-for-bit.
public class Adler32Tests
{
    [Fact]
    public void Adler32_EmptyBuffer_ReturnsSeed()
    {
        Assert.Equal(1u, Adler32.Update(1, ReadOnlySpan<byte>.Empty));
        Assert.Equal(0x9827D8F1u, Adler32.Update(0x9827D8F1, ReadOnlySpan<byte>.Empty));
    }

    [Fact]
    public void Adler32_KnownVector_Wikipedia()
    {
        // Public spec vector — adler32(seed=1, "Wikipedia") = 0x11E60398.
        Assert.Equal(0x11E60398u, Adler32.Update(1, "Wikipedia"u8));
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
        // Goldens captured from the original jzlib port; the Span-based implementation
        // must reproduce them without modifying this test.
        byte[] buf = new byte[length];
        new Random(42).NextBytes(buf);
        Assert.Equal(expectedGolden, Adler32.Update(1, buf));
    }

    [Fact]
    public void Adler32_SliceMatchesCopy()
    {
        byte[] buf = new byte[256];
        new Random(7).NextBytes(buf);

        // Adler over a slice of the buffer equals adler over a copy of that slice.
        byte[] slice = new byte[buf.Length - 64];
        Array.Copy(buf, 64, slice, 0, slice.Length);
        Assert.Equal(
            Adler32.Update(1, slice),
            Adler32.Update(1, buf.AsSpan(64)));
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
        Assert.Equal(expectedGolden, Adler32.Update(0x9827D8F1, buf));
    }
}
