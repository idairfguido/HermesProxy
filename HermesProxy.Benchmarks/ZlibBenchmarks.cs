using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using BenchmarkDotNet.Attributes;
using Framework.IO;

namespace HermesProxy.Benchmarks;

// Performance baselines for Framework.IO.Adler32 and the WorldSocket-style stateful
// DeflateStream path. Run before and after a perf-relevant change to read the
// alloc/time deltas. The pre-modernization numbers (jzlib + array-only Adler32) live
// in the test+bench commit (test+bench(zlib): lock Framework.IO.Zlib behavior
// pre-refactor) — check that out and re-run if you need a head-to-head against the
// legacy implementation.

[MemoryDiagnoser]
[ShortRunJob]
public class Adler32Benchmark
{
    [Params(64, 512, 2048, 8192)]
    public int PayloadSize;

    private byte[] _payload = null!;

    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        new Random(42).NextBytes(_payload);
    }

    [Benchmark(Baseline = true)]
    public uint Adler32_Span()
    {
        return Adler32.Update(1, _payload);
    }

    [Benchmark]
    public uint Adler32_Sliced()
    {
        // Mirrors callers that hash the body portion of a buffer that has a header
        // prefix (e.g. WorldSocket.cs:471 hashing data after the opcode).
        return Adler32.Update(0x9827D8F1, _payload.AsSpan(0, _payload.Length));
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class ZlibCompressBenchmark
{
    [Params(2048, 16384)]
    public int PayloadSize;

    private byte[] _payload = null!;
    private byte[] _compressed = null!;

    [GlobalSetup]
    public void Setup()
    {
        _payload = new byte[PayloadSize];
        new Random(42).NextBytes(_payload);
        _compressed = ZLib.Compress(_payload);
    }

    [Benchmark(Baseline = true)]
    public byte[] Compress_Roundtrip()
    {
        byte[] c = ZLib.Compress(_payload);
        return ZLib.Decompress(c, (uint)PayloadSize);
    }

    [Benchmark]
    public byte[] Decompress_Only()
    {
        return ZLib.Decompress(_compressed, (uint)PayloadSize);
    }
}

// Simulates the WorldSocket.CompressPacket hot path: one DeflateStream, N packets pushed
// through, each followed by Flush() (which emits Z_SYNC_FLUSH on .NET 6+). This is the
// most performance-sensitive caller — every outbound game-server packet > 1024 bytes
// runs through here.
[MemoryDiagnoser]
[ShortRunJob]
public class StatefulDeflateBenchmark
{
    [Params(8, 32)]
    public int PacketCount;

    [Params(2048, 8192)]
    public int PacketSize;

    private byte[][] _packets = null!;

    [GlobalSetup]
    public void Setup()
    {
        _packets = new byte[PacketCount][];
        var rng = new Random(42);
        for (int i = 0; i < PacketCount; i++)
        {
            _packets[i] = new byte[PacketSize];
            rng.NextBytes(_packets[i]);
        }
    }

    [Benchmark(Baseline = true)]
    public long Deflate_DeflateStream_NPackets()
    {
        using var buf = new MemoryStream();
        using var deflater = new DeflateStream(buf, CompressionLevel.Fastest, leaveOpen: true);

        Span<byte> hdr = stackalloc byte[2];
        for (int i = 0; i < PacketCount; i++)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(hdr, (ushort)(0x1000 + i));
            deflater.Write(hdr);
            deflater.Write(_packets[i]);
            deflater.Flush();
        }
        return buf.Length;
    }
}
