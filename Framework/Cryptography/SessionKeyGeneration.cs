/*
 * Copyright (C) 2012-2020 CypherCore <http://github.com/CypherCore>
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Security.Cryptography;

namespace Framework.Cryptography;

public sealed class SessionKeyGenerator
{
    private const int HashSize = 32; // SHA-256

    private readonly byte[] _o0 = new byte[HashSize];
    private readonly byte[] _o1 = new byte[HashSize];
    private readonly byte[] _o2 = new byte[HashSize];
    private int _taken;

    public SessionKeyGenerator(ReadOnlySpan<byte> buff)
    {
        int halfSize = buff.Length / 2;
        SHA256.HashData(buff[..halfSize], _o1);
        SHA256.HashData(buff[halfSize..], _o2);
        FillUp();
    }

    public SessionKeyGenerator(byte[] buff, int size) : this(buff.AsSpan(0, size)) { }

    public void Generate(Span<byte> buf)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            if (_taken == HashSize)
                FillUp();

            buf[i] = _o0[_taken];
            _taken++;
        }
    }

    public void Generate(byte[] buf, uint sz) => Generate(buf.AsSpan(0, (int)sz));

    private void FillUp()
    {
        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        ih.AppendData(_o1);
        ih.AppendData(_o0);
        ih.AppendData(_o2);

        // Hash directly into _o0 to avoid the byte[] allocation that GetHashAndReset would produce.
        if (!ih.TryGetHashAndReset(_o0, out int written) || written != HashSize)
            throw new CryptographicException("SessionKeyGenerator.FillUp: SHA-256 produced unexpected output size.");

        _taken = 0;
    }
}
