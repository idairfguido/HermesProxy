using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Framework.Cryptography;
using Xunit;

namespace HermesProxy.Tests.Framework.Cryptography;

public class Sha256Tests
{
    [Fact]
    public void Process_NistVector_ProducesExpectedDigest()
    {
        // NIST FIPS 180-2: SHA-256("abc") -> ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad
        byte[] input = "abc"u8.ToArray();
        byte[] expected = new byte[]
        {
            0xba, 0x78, 0x16, 0xbf, 0x8f, 0x01, 0xcf, 0xea,
            0x41, 0x41, 0x40, 0xde, 0x5d, 0xae, 0x22, 0x23,
            0xb0, 0x03, 0x61, 0xa3, 0x96, 0x17, 0x7a, 0x9c,
            0xb4, 0x10, 0xff, 0x61, 0xf2, 0x00, 0x15, 0xad,
        };

        var sha = new Sha256();
        sha.Finish(input);

        Assert.NotNull(sha.Digest);
        Assert.Equal(expected, sha.Digest);
    }

    [Fact]
    public void ChainedProcess_MatchesSingleShot()
    {
        // Streaming Process(...)+Finish(...) must equal SHA256.HashData(concatenated).
        byte[] a = "Hello, "u8.ToArray();
        byte[] b = "WoW "u8.ToArray();
        byte[] c = "packet"u8.ToArray();

        byte[] combined = new byte[a.Length + b.Length + c.Length];
        Buffer.BlockCopy(a, 0, combined, 0, a.Length);
        Buffer.BlockCopy(b, 0, combined, a.Length, b.Length);
        Buffer.BlockCopy(c, 0, combined, a.Length + b.Length, c.Length);
        byte[] reference = SHA256.HashData(combined);

        var sha = new Sha256();
        sha.Process(a, a.Length);
        sha.Process(b, b.Length);
        sha.Finish(c);

        Assert.Equal(reference, sha.Digest);
    }

    [Fact]
    public void ProcessUint_LittleEndianMatchesBitConverter()
    {
        // Locks endianness: Process((uint)X) must equal feeding the 4 little-endian bytes.
        const uint value = 0xCAFEBABEu;
        byte[] suffix = "tail"u8.ToArray();

        byte[] leBytes = new byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(leBytes, value);
        byte[] combined = new byte[4 + suffix.Length];
        Buffer.BlockCopy(leBytes, 0, combined, 0, 4);
        Buffer.BlockCopy(suffix, 0, combined, 4, suffix.Length);
        byte[] reference = SHA256.HashData(combined);

        var sha = new Sha256();
        sha.Process(value);
        sha.Finish(suffix);

        Assert.Equal(reference, sha.Digest);
    }

    [Fact]
    public void Finish_OnlyFinalBuffer_HashesThatBuffer()
    {
        byte[] input = Encoding.UTF8.GetBytes("payload");
        byte[] reference = SHA256.HashData(input);

        var sha = new Sha256();
        sha.Finish(input);

        Assert.Equal(reference, sha.Digest);
    }
}

public class HmacSha256Tests
{
    [Fact]
    public void HashData_MatchesRfc4231TestCase1()
    {
        // RFC 4231, Test Case 1: key = 0x0b * 20, data = "Hi There"
        // Expected: b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7
        byte[] key = new byte[20];
        Array.Fill(key, (byte)0x0b);
        byte[] data = "Hi There"u8.ToArray();
        byte[] expected = new byte[]
        {
            0xb0, 0x34, 0x4c, 0x61, 0xd8, 0xdb, 0x38, 0x53,
            0x5c, 0xa8, 0xaf, 0xce, 0xaf, 0x0b, 0xf1, 0x2b,
            0x88, 0x1d, 0xc2, 0x00, 0xc9, 0x83, 0x3d, 0xa7,
            0x26, 0xe9, 0x37, 0x6c, 0x2e, 0x32, 0xcf, 0xf7,
        };

        var hmac = new HmacSha256(key);
        hmac.Finish(data, data.Length);

        Assert.NotNull(hmac.Digest);
        Assert.Equal(expected, hmac.Digest);
    }

    [Fact]
    public void ChainedProcess_MatchesSingleShot()
    {
        byte[] key = new byte[16];
        for (int i = 0; i < key.Length; i++) key[i] = (byte)(i + 1);

        byte[] a = "alpha"u8.ToArray();
        byte[] b = "bravo"u8.ToArray();
        byte[] c = "charlie"u8.ToArray();

        byte[] combined = new byte[a.Length + b.Length + c.Length];
        Buffer.BlockCopy(a, 0, combined, 0, a.Length);
        Buffer.BlockCopy(b, 0, combined, a.Length, b.Length);
        Buffer.BlockCopy(c, 0, combined, a.Length + b.Length, c.Length);
        byte[] reference = HMACSHA256.HashData(key, combined);

        var hmac = new HmacSha256(key);
        hmac.Process(a, a.Length);
        hmac.Process(b, b.Length);
        hmac.Finish(c, c.Length);

        Assert.Equal(reference, hmac.Digest);
    }

    [Fact]
    public void Finish_PartialLength_HashesOnlyPrefix()
    {
        // Finish(byte[] data, int length) must hash only [0..length), not the full array.
        byte[] key = "secret-key"u8.ToArray();
        byte[] data = "the quick brown fox"u8.ToArray();
        const int length = 10; // "the quick "

        byte[] reference = HMACSHA256.HashData(key, data.AsSpan(0, length));

        var hmac = new HmacSha256(key);
        hmac.Finish(data, length);

        Assert.Equal(reference, hmac.Digest);
    }
}
