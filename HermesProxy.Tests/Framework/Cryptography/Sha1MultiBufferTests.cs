using System;
using System.Security.Cryptography;
using Framework.Cryptography;
using Xunit;

namespace HermesProxy.Tests.Framework.Cryptography;

/// <summary>
/// Locks the shape of <c>global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(params byte[][])</c> as currently used
/// across SRP6 (AuthClient) and the WorldClient packet checksum. After HashHelper is
/// removed, these inputs will be hashed inline at each call site via SHA1.HashData /
/// IncrementalHash; the byte output must be identical.
/// </summary>
public class Sha1MultiBufferTests
{
    [Fact]
    public void Hash_SingleBuffer_MatchesSha1HashData()
    {
        byte[] input = "username:password"u8.ToArray();
        byte[] expected = SHA1.HashData(input);

        byte[] actual = global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(input);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Hash_TwoBuffers_MatchesSha1HashDataOfConcatenated()
    {
        byte[] a = new byte[] { 0xAA, 0xBB, 0xCC };
        byte[] b = new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 };

        byte[] combined = Concat(a, b);
        byte[] expected = SHA1.HashData(combined);

        byte[] actual = global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(a, b);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Hash_FourBuffers_MatchesSha1HashDataOfConcatenated()
    {
        // Mirrors AuthClient's most-buffer-heavy SRP6 sites (M1 / M2 proofs).
        byte[] a = "alpha"u8.ToArray();
        byte[] b = "bravo"u8.ToArray();
        byte[] c = "charlie"u8.ToArray();
        byte[] d = "delta"u8.ToArray();

        byte[] combined = Concat(a, b, c, d);
        byte[] expected = SHA1.HashData(combined);

        byte[] actual = global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(a, b, c, d);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Hash_EmptyBuffer_HashesEmptyInput()
    {
        byte[] expected = SHA1.HashData(ReadOnlySpan<byte>.Empty);

        byte[] actual = global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(Array.Empty<byte>());

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Hash_MixedSizes_MatchesConcatenated()
    {
        // Exercises buffers of differing widths (typical SRP6 BigInteger-derived sizes).
        byte[] a = new byte[1] { 0x07 };
        byte[] b = new byte[20];
        byte[] c = new byte[16];
        for (int i = 0; i < b.Length; i++) b[i] = (byte)(i + 1);
        for (int i = 0; i < c.Length; i++) c[i] = (byte)(0xF0 + i);

        byte[] combined = Concat(a, b, c);
        byte[] expected = SHA1.HashData(combined);

        byte[] actual = global::Framework.Cryptography.HashAlgorithm.SHA1.Hash(a, b, c);

        Assert.Equal(expected, actual);
    }

    private static byte[] Concat(params byte[][] parts)
    {
        int total = 0;
        foreach (var p in parts) total += p.Length;
        byte[] result = new byte[total];
        int offset = 0;
        foreach (var p in parts)
        {
            Buffer.BlockCopy(p, 0, result, offset, p.Length);
            offset += p.Length;
        }
        return result;
    }
}
