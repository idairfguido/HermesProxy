using System;
using BenchmarkDotNet.Attributes;
using Framework.Cryptography;

namespace HermesProxy.Benchmarks;

// Compares the platform System.Security.Cryptography.AesGcm path against the BouncyCastle
// GcmBlockCipher fallback used on platforms that reject the WoW protocol's 12-byte tag
// (notably macOS). Encrypt covers the per-packet hot path (Init + ProcessBytes + DoFinal in
// the BC case, single Encrypt call in the native case) — Decrypt has the same shape so its
// relative cost tracks Encrypt's.
[MemoryDiagnoser]
[ShortRunJob]
public class WorldCryptBenchmarks
{
    [Params(32, 128, 512, 2048)]
    public int PayloadSize;

    private static readonly byte[] Key16 = "0123456789ABCDEF"u8.ToArray();

    private WorldCrypt _native = null!;
    private WorldCrypt _bouncyCastle = null!;
    private byte[] _payload = null!;
    private byte[] _data = null!;
    private byte[] _tag = null!;

    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        new Random(42).NextBytes(_payload);

        WorldCrypt.ForceBouncyCastleForTests = false;
        _native = new WorldCrypt();
        _native.Initialize(Key16);

        WorldCrypt.ForceBouncyCastleForTests = true;
        _bouncyCastle = new WorldCrypt();
        _bouncyCastle.Initialize(Key16);
        WorldCrypt.ForceBouncyCastleForTests = false;

        _data = new byte[PayloadSize];
        _tag = new byte[12];
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _native?.Dispose();
        _bouncyCastle?.Dispose();
        WorldCrypt.ForceBouncyCastleForTests = false;
    }

    // Refresh the in-place buffer between iterations so we don't keep encrypting ciphertext.
    [IterationSetup(Targets = new[] { nameof(Encrypt_Native), nameof(Encrypt_BouncyCastle) })]
    public void RefreshData()
    {
        Buffer.BlockCopy(_payload, 0, _data, 0, _payload.Length);
    }

    [Benchmark(Baseline = true)]
    public bool Encrypt_Native()
    {
        return _native.Encrypt(_data, _tag);
    }

    [Benchmark]
    public bool Encrypt_BouncyCastle()
    {
        return _bouncyCastle.Encrypt(_data, _tag);
    }
}
