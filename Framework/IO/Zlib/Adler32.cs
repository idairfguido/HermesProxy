// Adler-32 — port of Mark Adler / Jean-loup Gailly's reference implementation
// (zlib adler32.c, 1995-2007), modernized for .NET 10: ReadOnlySpan<byte> input,
// ref-byte indexing, AggressiveInlining. System.IO.Hashing does not ship Adler-32,
// so we keep our own implementation. The NMAX-blocked unrolled loop and modulo
// scheduling come straight from the upstream C source.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Framework.IO;

public static class Adler32
{
    private const uint Base = 65521;     // largest prime < 65536
    private const uint NMax = 5552;      // largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static uint Update(uint adler, ReadOnlySpan<byte> buf)
    {
        uint sum2 = (adler >> 16) & 0xFFFF;
        adler &= 0xFFFF;

        int len = buf.Length;
        if (len == 0)
            return adler | (sum2 << 16);

        ref byte p = ref MemoryMarshal.GetReference(buf);

        // single-byte fast path keeps len==1 callers branchless past the modulo work
        if (len == 1)
        {
            adler += p;
            if (adler >= Base) adler -= Base;
            sum2 += adler;
            if (sum2 >= Base) sum2 -= Base;
            return adler | (sum2 << 16);
        }

        // tiny lengths — keep modulos out of the hot path entirely
        if (len < 16)
        {
            int j = 0;
            while (j < len)
            {
                adler += Unsafe.Add(ref p, j++);
                sum2 += adler;
            }
            if (adler >= Base) adler -= Base;
            sum2 %= Base;
            return adler | (sum2 << 16);
        }

        // NMAX-sized blocks: 16 sums per inner step, one modulo per block
        int idx = 0;
        while (len >= NMax)
        {
            len -= (int)NMax;
            uint n = NMax / 16;
            do
            {
                adler += Unsafe.Add(ref p, idx); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 1); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 2); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 3); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 4); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 5); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 6); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 7); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 8); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 9); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 10); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 11); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 12); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 13); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 14); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 15); sum2 += adler;
                idx += 16;
            } while (--n != 0);

            adler %= Base;
            sum2 %= Base;
        }

        // remainder < NMAX, still one modulo at the end
        if (len != 0)
        {
            while (len >= 16)
            {
                len -= 16;
                adler += Unsafe.Add(ref p, idx); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 1); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 2); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 3); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 4); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 5); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 6); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 7); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 8); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 9); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 10); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 11); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 12); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 13); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 14); sum2 += adler;
                adler += Unsafe.Add(ref p, idx + 15); sum2 += adler;
                idx += 16;
            }

            while (len-- != 0)
            {
                adler += Unsafe.Add(ref p, idx++);
                sum2 += adler;
            }

            adler %= Base;
            sum2 %= Base;
        }

        return adler | (sum2 << 16);
    }
}
