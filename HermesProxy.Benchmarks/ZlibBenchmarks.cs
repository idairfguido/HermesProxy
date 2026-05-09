using System;
using System.Buffers.Binary;
using BenchmarkDotNet.Attributes;
using Framework.IO;

namespace HermesProxy.Benchmarks;

// Baseline benchmarks for Framework.IO.Zlib prior to the modernization pass (delete the
// jzlib port + Span-based Adler32 + DeflateStream-backed stateful compression). After
// the refactor lands, the same benchmark file is re-run to read the alloc/time deltas.

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
    public uint Adler32_Jzlib_Array()
    {
        return ZLib.adler32(1, _payload, (uint)_payload.Length);
    }

    [Benchmark]
    public uint Adler32_Jzlib_WithOffset()
    {
        // The (adler, buf, ind, len) overload is the path WorldSocket.cs takes when
        // hashing a partial buffer. Keep tracking it independently.
        return ZLib.adler32(1, _payload, 0, (uint)_payload.Length);
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

// Simulates the WorldSocket.CompressPacket hot path: one deflater, N packets pushed
// through, each followed by Z_SYNC_FLUSH. This is the most performance-sensitive caller
// — every outbound game-server packet > 1024 bytes runs through here.
[MemoryDiagnoser]
[ShortRunJob]
public class StatefulDeflateBenchmark
{
    private const int Z_SYNC_FLUSH = 2;

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
    public long Deflate_Jzlib_NPackets()
    {
        var strm = new ZLib.z_stream();
        int rc = ZLib.deflateInit2(strm, 1, 8, -15, 8, 0);
        if (rc != 0) throw new InvalidOperationException("deflateInit2 failed");

        long totalOut = 0;
        Span<byte> hdrSpan = stackalloc byte[2];
        for (int i = 0; i < PacketCount; i++)
        {
            byte[] body = _packets[i];
            byte[] uncompressed = new byte[2 + body.Length];
            BinaryPrimitives.WriteUInt16LittleEndian(hdrSpan, (ushort)(0x1000 + i));
            uncompressed[0] = hdrSpan[0]; uncompressed[1] = hdrSpan[1];
            Buffer.BlockCopy(body, 0, uncompressed, 2, body.Length);

            uint bound = ZLib.deflateBound(strm, (uint)body.Length);
            byte[] outBuf = new byte[bound];

            strm.next_in = 0;
            strm.avail_in = (uint)uncompressed.Length;
            strm.in_buf = uncompressed;
            strm.next_out = 0;
            strm.avail_out = bound;
            strm.out_buf = outBuf;

            ZLib.deflate(strm, Z_SYNC_FLUSH);
            totalOut += bound - strm.avail_out;
        }
        return totalOut;
    }
}
