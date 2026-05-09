using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using Framework.IO;
using Xunit;

namespace HermesProxy.Tests.Framework.Zlib;

// THE Phase-B parity gate. Mirrors WorldSocket.CompressPacket (WorldSocket.cs:503-528):
// one z_stream constructed once with deflateInit2(level=1, method=8, windowBits=-15,
// memLevel=8, strategy=0), reused across multiple packets, each packet's bytes followed
// by a deflate(strm, 2 /* Z_SYNC_FLUSH */). Phase B's DeflateStream-based replacement
// must reproduce these exact bytes — that's the wire-format contract the modern client's
// inflate side observes.
public class StatefulDeflateTests
{
    private const int Z_SYNC_FLUSH = 2;
    private const int Z_OK = 0;

    private static ZLib.z_stream NewStream()
    {
        var strm = new ZLib.z_stream();
        int rc = ZLib.deflateInit2(strm, 1, 8, -15, 8, 0);
        Assert.Equal(Z_OK, rc);
        return strm;
    }

    // Push one packet (opcode + body) through the deflater with Z_SYNC_FLUSH and append
    // the produced bytes to `output`. Mirrors WorldSocket.cs:503-528 exactly.
    private static void DeflateOnePacket(ZLib.z_stream strm, ushort opcode, ReadOnlySpan<byte> body, MemoryStream output)
    {
        byte[] uncompressed = new byte[2 + body.Length];
        BinaryPrimitives.WriteUInt16LittleEndian(uncompressed, opcode);
        body.CopyTo(uncompressed.AsSpan(2));

        uint bound = ZLib.deflateBound(strm, (uint)body.Length);
        byte[] outBuf = new byte[bound];

        strm.next_in = 0;
        strm.avail_in = (uint)uncompressed.Length;
        strm.in_buf = uncompressed;
        strm.next_out = 0;
        strm.avail_out = bound;
        strm.out_buf = outBuf;

        int rc = ZLib.deflate(strm, Z_SYNC_FLUSH);
        Assert.Equal(Z_OK, rc);

        output.Write(outBuf, 0, (int)(bound - strm.avail_out));
    }

    [Fact]
    public void WorldSocketDeflate_ThreePacketsSyncFlushed_MatchesGolden()
    {
        // Three deterministic packets pushed through one shared deflater. Bytes captured
        // from today's jzlib code; Phase B's replacement must produce the same hex.
        var strm = NewStream();
        var output = new MemoryStream();

        DeflateOnePacket(strm, 0x0042, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }, output);
        DeflateOnePacket(strm, 0x4711, "Hello, WoW packet body content"u8, output);

        byte[] body3 = new byte[1024];
        new Random(42).NextBytes(body3);
        DeflateOnePacket(strm, 0xCAFE, body3, output);

        string actual = Convert.ToHexString(output.ToArray());

        const string Golden =
            "726260646266616563E700000000FFFF1274F748CDC9C9D75108CF0F5728484CCE4E2D5148CA4FA9" +
            "5448CECF2B49CD2B01000000FFFF000204FDFBFECA3E17BA96AE04CD3B99869E56F0ADBF3A6FB74D" +
            "2555175DCC6E8B0914579AB036CFD6280BB3C707DBAEF270DC9505096E676BE7F10D46D827BAED27" +
            "22FBB9E4FED667871A85B386A8AF727663A7813769819AADEBB3BF2989FD0051C69FE4E0F50E7105" +
            "2D7EEFE9B3E53E429BCF758038B5BEA0600BF84A173EFD1D8462C009CA918154357B0AEA92B1FA06" +
            "747C8EB987801810E04480EA1527EEEE80D26451253577094E204BBDABD40DAE11F2D59AB0449503" +
            "49AF288611963F0D8C1B69C04CA8E03F05846B67345A650C36D1D82251543E3C3C6007750CF1F8B7" +
            "E03D31906A4D61681296942BD82DCFEF4A1C503FBE1DBC37DC2C83EBA16A32148BB486F728D5A1E8" +
            "706F0FBAC013640F91182BF0A0B9F8C281FAB33EC975C7A764809F23A7D7C6757A67CE4E0E0177C9" +
            "C8745A883F4173D2BBC3762E21631FFAA868563F734067BA52B67539EAAAB47F82EA75551B3E6822" +
            "0DCD67E36197531748D4A5A5AA3755870C9F3688CB39E8110FF6AB36EDCADF6A003432A6D98458BA" +
            "625EF116040FD44B95EE487B07DF56D3E456D46AEB1C297DF43BA57605AEAE88467B256D47D20CFF" +
            "2260A1FE82636220DD5EC182AFEECE46347321C96A5D45E640BF348BB9E98B124AD4488B7A2AFB90" +
            "D98499A7EC2CD858ACDF38D4B04395C41BBBBEC22FD667F4D2AF37DBA643CB097790EFE5C49DF914" +
            "E6DCDFD9B0B761073E836FBF6C3961021DA5D53DF4FDB169142F5C2053AF007EC2AADC53F78D431A" +
            "4D309C23D30C9D3E2EECC81FC7BB1FE92A75C5815B41B48F83F1FCC25D0E0B8F61BBEFC4618E9508" +
            "921DF166E6E1B6068AF32981C7C2BF0C7B0D9FE20FDAE120E138BD0CCC5B889BE0BCEF3D4F4DA8A1" +
            "3BF6386AE58AC79EFCE24853EFB3F9B73CD185AD23AA3A2E6B8DA00BD381BD2E64FDA719A2773A56" +
            "711E0FE91234AC2DC337007DC8FA7E90BB09FF57E9BCCB7A251AEFF10BDF14C4962E3C059A501C36" +
            "E172479B8040656A7F165D9978E36CB45395C4F845BB21E3223BE699BD78F66BEDC87577752FF9E5" +
            "B48471F5827B4CB3C188B2058B23F1265163794459CCAC0620F2ABB06DF53F239BDF43C0492C61A6" +
            "EF32D735C6F1DFC9D64F2511B6B48BE8D86AD490D09C46CC498E69ACE031DD2402D25526F477402F" +
            "12E2D91ECF2D6DE6545BE7DF57D1D653EC0BEB388733E9A472000EE35F64F4612AA57DEEC228FC1B" +
            "208258844AFDCD31FB023AE88B07A7DEF834ACCE1F8987EF6785B4B153E890C30FD01705DB645A74" +
            "33DDE85D290BD622FE9FF58D7C2DFC97FB681DC4481BDE137177D9BAF0A20393DA379B42A129C55B" +
            "AF92B3B4F20247E1D480097246962B17FFD4EBB2311B325B9B61B34492BAF5D2A3C114B6682A206E" +
            "2FF8E66F98904D430C8342CC3D122A935F5718914E024F1A8B37CF848174AED5B47D833835C3C20B" +
            "147037";
        Assert.Equal(Golden, actual);
    }

    [Fact]
    public void WorldSocketDeflate_LargeRandomPayloads_RoundtripThroughDeflateStream()
    {
        // Inflate-roundtrip side of the gate. Even if literal byte equality with jzlib
        // fails (CompressionLevel.Fastest may emit a different but still-valid encoding),
        // the produced bytes MUST decode back to the original packets via the modern
        // DeflateStream — that's what the WoW client does on the wire.
        var strm = NewStream();
        var output = new MemoryStream();

        var rng = new Random(99);
        var packets = new (ushort opcode, byte[] body)[8];
        for (int i = 0; i < packets.Length; i++)
        {
            ushort op = (ushort)(0x1000 + i);
            byte[] body = new byte[1024 + i * 256];
            rng.NextBytes(body);
            packets[i] = (op, body);
            DeflateOnePacket(strm, op, body, output);
        }

        // Inflate the entire concatenated stream and verify the decoded bytes equal
        // [opcode1][body1]…[opcodeN][bodyN]. DeflateStream over a raw-deflate stream
        // (windowBits=-15) decodes Z_SYNC_FLUSH-bounded output up to the last sync block.
        byte[] compressed = output.ToArray();
        using var inflateInput = new MemoryStream(compressed);
        using var inflater = new DeflateStream(inflateInput, CompressionMode.Decompress);
        using var decoded = new MemoryStream();
        inflater.CopyTo(decoded);

        using var expected = new MemoryStream();
        Span<byte> hdr = stackalloc byte[2];
        foreach (var (op, body) in packets)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(hdr, op);
            expected.Write(hdr);
            expected.Write(body);
        }

        Assert.Equal(expected.ToArray(), decoded.ToArray());
    }
}
