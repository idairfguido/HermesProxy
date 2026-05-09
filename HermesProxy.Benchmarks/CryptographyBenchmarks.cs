using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Framework.Cryptography;

namespace HermesProxy.Benchmarks;

// Baseline benchmarks for Framework.Cryptography prior to the modernization pass
// (delete dead code + IncrementalHash + Span APIs). After the refactor lands, the
// same benchmark file is re-run to read the alloc/time deltas.

[MemoryDiagnoser]
[ShortRunJob]
public class Sha256ChainBenchmark
{
    [Params(64, 256, 1024)]
    public int InputSize;

    private byte[] _input = null!;
    private const uint UintTag = 0xCAFEBABEu;
    private byte[] _tail = null!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[InputSize];
        new Random(42).NextBytes(_input);
        _tail = "tail"u8.ToArray();
    }

    [Benchmark(Baseline = true)]
    public byte[]? Sha256_Process_Wrapper()
    {
        var sha = new Sha256();
        sha.Process(_input, _input.Length);
        sha.Process(UintTag);
        sha.Finish(_tail);
        return sha.Digest;
    }

    [Benchmark]
    public byte[] Sha256_IncrementalHash_Direct()
    {
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        ih.AppendData(_input);
        Span<byte> uintBytes = stackalloc byte[sizeof(uint)];
        BinaryPrimitives.WriteUInt32LittleEndian(uintBytes, UintTag);
        ih.AppendData(uintBytes);
        ih.AppendData(_tail);
        return ih.GetHashAndReset();
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class HmacSha256ChainBenchmark
{
    [Params(64, 256, 1024)]
    public int InputSize;

    private byte[] _key = null!;
    private byte[] _input = null!;
    private byte[] _tail = null!;

    [GlobalSetup]
    public void Setup()
    {
        _key = new byte[32];
        new Random(7).NextBytes(_key);
        _input = new byte[InputSize];
        new Random(42).NextBytes(_input);
        _tail = "auth"u8.ToArray();
    }

    [Benchmark(Baseline = true)]
    public byte[]? HmacSha256_Process_Wrapper()
    {
        var h = new HmacSha256(_key);
        h.Process(_input, _input.Length);
        h.Finish(_tail, _tail.Length);
        return h.Digest;
    }

    [Benchmark]
    public byte[] HmacSha256_IncrementalHash_Direct()
    {
        using var ih = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, _key);
        ih.AppendData(_input);
        ih.AppendData(_tail);
        return ih.GetHashAndReset();
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class SessionKeyGeneratorBenchmark
{
    private byte[] _input = null!;
    private byte[] _output = null!;

    [GlobalSetup]
    public void Setup()
    {
        _input = new byte[32];
        new Random(42).NextBytes(_input);
        _output = new byte[40]; // typical session-key size
    }

    [Benchmark]
    public byte[] SessionKeyGenerator_Generate40()
    {
        var gen = new SessionKeyGenerator(_input, 32);
        gen.Generate(_output, 40);
        return _output;
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class Sha1MultiBufferBenchmark
{
    // Measures the IncrementalHash multi-buffer pattern that replaced the deleted
    // HashHelper.Combine() shim. Used in SRP6 (AuthClient) for 1-5 buffers per hash.
    [Params(2, 5)]
    public int Parts;

    private byte[][] _parts = null!;

    [GlobalSetup]
    public void Setup()
    {
        _parts = new byte[Parts][];
        var rnd = new Random(42);
        for (int i = 0; i < Parts; i++)
        {
            _parts[i] = new byte[i == 0 ? 20 : 16];
            rnd.NextBytes(_parts[i]);
        }
    }

    [Benchmark]
    public byte[] IncrementalHash_MultiBuffer()
    {
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA1);
        for (int i = 0; i < _parts.Length; i++)
            ih.AppendData(_parts[i]);
        return ih.GetHashAndReset();
    }
}
