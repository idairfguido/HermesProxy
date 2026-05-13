using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Framework.Cryptography;
using Xunit;

namespace HermesProxy.Tests.Framework.Cryptography;

public class WorldCryptTests : IDisposable
{
    // 16-byte AES-128 key. WoW uses 16-byte keys derived per session; the algorithm doesn't care
    // what's in the bytes, only that the length is 16/24/32.
    private static readonly byte[] Key16 = "0123456789ABCDEF"u8.ToArray();

    // Probe once at class load: does the platform's native AesGcm accept the 12-byte (96-bit) tag
    // the WoW protocol uses? macOS Apple CommonCrypto rejects it; .NET there falls back to BC.
    // Tests that depend on native AesGcm as an independent verifier use Assert.SkipUnless on this.
    private static readonly bool PlatformSupportsAesGcm12ByteTag = ProbeNativeAesGcm12();
    private static bool ProbeNativeAesGcm12()
    {
        Span<byte> probeKey = stackalloc byte[16];
        try { using var _ = new AesGcm(probeKey, 12); return true; }
        catch (ArgumentException) { return false; }
        catch (PlatformNotSupportedException) { return false; }
    }

    public WorldCryptTests()
    {
        WorldCrypt.ForceBouncyCastleForTests = false;
    }

    public void Dispose()
    {
        WorldCrypt.ForceBouncyCastleForTests = false;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Encrypt_ThenDecrypt_RoundtripsThroughSameBackend(bool forceBouncyCastle)
    {
        Assert.SkipUnless(PlatformSupportsAesGcm12ByteTag,
            "Native AesGcm rejects 12-byte tags here; both the unforced-backend invariant and the cross-decrypt verifier require it.");

        WorldCrypt.ForceBouncyCastleForTests = forceBouncyCastle;

        // The two WorldCrypt instances simulate the proxy's two halves: one Encrypt-side
        // and one Decrypt-side. Encrypt uses _serverCounter+SRVR, Decrypt uses _clientCounter+CLNT,
        // so we can't roundtrip on a single instance — we mirror the SRVR-direction nonce by
        // hand and use the underlying primitive to verify.
        using var sender = new WorldCrypt();
        sender.Initialize(Key16);
        Assert.Equal(forceBouncyCastle, sender.UsingBouncyCastle);

        byte[] plaintext = "Hello, WoW packet"u8.ToArray();
        byte[] data = (byte[])plaintext.Clone();
        byte[] tag = new byte[12];

        Assert.True(sender.Encrypt(data, tag));
        Assert.NotEqual(plaintext, data);

        // Build the same nonce sender used: counter=0, suffix="SRVR".
        byte[] nonce = new byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.AsSpan(), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(8), 0x52565253);

        byte[] decrypted = new byte[data.Length];
        using var refDecryptor = new AesGcm(Key16, 12);
        refDecryptor.Decrypt(nonce, data, tag, decrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Decrypt_TamperedTag_ReturnsFalse(bool forceBouncyCastle)
    {
        WorldCrypt.ForceBouncyCastleForTests = forceBouncyCastle;

        // Hand-encrypt a CLNT-direction packet using the platform primitive, then feed a
        // bit-flipped tag into WorldCrypt.Decrypt and assert it surfaces as a false return.
        byte[] plaintext = "client packet"u8.ToArray();
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[12];
        byte[] nonce = new byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.AsSpan(), 0);
        BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(8), 0x544E4C43);

        using (var refEncryptor = new AesGcm(Key16, 12))
            refEncryptor.Encrypt(nonce, plaintext, ciphertext, tag);

        tag[0] ^= 0xFF; // tamper

        using var receiver = new WorldCrypt();
        receiver.Initialize(Key16);
        byte[] data = (byte[])ciphertext.Clone();
        Assert.False(receiver.Decrypt(data, tag));
    }

    [Fact]
    public void Initialize_CalledTwice_Throws()
    {
        using var crypt = new WorldCrypt();
        crypt.Initialize(Key16);
        Assert.Throws<InvalidOperationException>(() => crypt.Initialize(Key16));
    }

    [Fact]
    public void UsingBouncyCastle_ReflectsForceFlag()
    {
        WorldCrypt.ForceBouncyCastleForTests = true;
        using var bc = new WorldCrypt();
        bc.Initialize(Key16);
        Assert.True(bc.UsingBouncyCastle);

        WorldCrypt.ForceBouncyCastleForTests = false;
        Assert.SkipUnless(PlatformSupportsAesGcm12ByteTag,
            "Platform falls back to BC even without the test flag; cannot verify unforced→native invariant here.");

        using var native = new WorldCrypt();
        native.Initialize(Key16);
        Assert.False(native.UsingBouncyCastle);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Encrypt_NativeAndBc_ProduceWireCompatibleOutput(bool senderUsesBouncyCastle)
    {
        // Wire-compatibility check: regardless of which backend the sender uses, the output
        // must decrypt cleanly with a stock AesGcm at 12-byte tag (when the platform supports it).
        // This proves the BC fallback emits standards-compliant GCM output.
        WorldCrypt.ForceBouncyCastleForTests = senderUsesBouncyCastle;

        using var sender = new WorldCrypt();
        sender.Initialize(Key16);

        byte[] plaintext = Encoding.UTF8.GetBytes("payload of moderate length spanning more than one block");
        byte[] data = (byte[])plaintext.Clone();
        byte[] tag = new byte[12];
        Assert.True(sender.Encrypt(data, tag));

        byte[] nonce = new byte[12];
        BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(8), 0x52565253);

        Assert.SkipUnless(PlatformSupportsAesGcm12ByteTag,
            "Native AesGcm rejects 12-byte tags here; cross-check requires it.");

        byte[] decrypted = new byte[data.Length];
        using var reference = new AesGcm(Key16, 12);
        reference.Decrypt(nonce, data, tag, decrypted);
        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Encrypt_AdvancesServerCounter_DecryptAdvancesClientCounter()
    {
        // After 3 encrypts, the 4th Encrypt's nonce must be counter=3+SRVR. Confirm by
        // hand-decrypting with that nonce.
        using var sender = new WorldCrypt();
        sender.Initialize(Key16);

        byte[] plaintext = "x"u8.ToArray();
        for (int i = 0; i < 3; i++)
        {
            byte[] data = (byte[])plaintext.Clone();
            byte[] tag = new byte[12];
            Assert.True(sender.Encrypt(data, tag));
        }

        byte[] finalData = (byte[])plaintext.Clone();
        byte[] finalTag = new byte[12];
        Assert.True(sender.Encrypt(finalData, finalTag));

        byte[] nonce = new byte[12];
        BinaryPrimitives.WriteUInt64LittleEndian(nonce.AsSpan(), 3);
        BinaryPrimitives.WriteUInt32LittleEndian(nonce.AsSpan(8), 0x52565253);

        Assert.SkipUnless(PlatformSupportsAesGcm12ByteTag,
            "Native AesGcm rejects 12-byte tags here; counter-advancement verifier requires it.");

        byte[] decrypted = new byte[finalData.Length];
        using var reference = new AesGcm(Key16, 12);
        reference.Decrypt(nonce, finalData, finalTag, decrypted);
        Assert.Equal(plaintext, decrypted);
    }
}
