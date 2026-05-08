using System;
using System.Security.Cryptography;
using Framework.Cryptography;
using Xunit;

namespace HermesProxy.Tests.Framework.Cryptography;

public class SessionKeyGeneratorTests
{
    /// <summary>
    /// Reference implementation that mirrors the WoW-protocol KDF exactly using only
    /// stock SHA256 primitives. The production class must produce identical output
    /// across the planned refactor.
    /// </summary>
    private static byte[] ReferenceGenerate(byte[] input, int inputSize, int outputLen)
    {
        int halfSize = inputSize / 2;
        byte[] o1 = SHA256.HashData(input.AsSpan(0, halfSize));
        byte[] o2 = SHA256.HashData(input.AsSpan(halfSize, inputSize - halfSize));
        byte[] o0 = new byte[32];
        byte[] result = new byte[outputLen];
        int taken = 32; // forces FillUp on first iteration

        for (int i = 0; i < outputLen; i++)
        {
            if (taken == 32)
            {
                using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
                ih.AppendData(o1);
                ih.AppendData(o0);
                ih.AppendData(o2);
                o0 = ih.GetHashAndReset();
                taken = 0;
            }
            result[i] = o0[taken];
            taken++;
        }
        return result;
    }

    private static byte[] BuildInput(int size, int seed = 0)
    {
        byte[] input = new byte[size];
        for (int i = 0; i < size; i++) input[i] = (byte)((i + seed) & 0xFF);
        return input;
    }

    [Fact]
    public void Generate_KnownInput_MatchesReferenceImplementation()
    {
        byte[] input = BuildInput(32);
        byte[] expected = ReferenceGenerate(input, 32, 40);

        var gen = new SessionKeyGenerator(input, 32);
        byte[] output = new byte[40];
        gen.Generate(output, 40);

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Generate_LargerThanFillUpBoundary_StillMatchesReference()
    {
        // 80 bytes forces three internal FillUp cycles (32 + 32 + 16). Validates the chained-FillUp behavior.
        byte[] input = BuildInput(32, seed: 7);
        byte[] expected = ReferenceGenerate(input, 32, 80);

        var gen = new SessionKeyGenerator(input, 32);
        byte[] output = new byte[80];
        gen.Generate(output, 80);

        Assert.Equal(expected, output);
    }

    [Fact]
    public void Generate_TwoInstancesSameInput_ProduceSameOutput()
    {
        byte[] input = BuildInput(32);

        var a = new SessionKeyGenerator(input, 32);
        byte[] outA = new byte[40];
        a.Generate(outA, 40);

        var b = new SessionKeyGenerator(input, 32);
        byte[] outB = new byte[40];
        b.Generate(outB, 40);

        Assert.Equal(outA, outB);
    }

    [Fact]
    public void Generate_DifferentInputs_ProduceDifferentOutput()
    {
        byte[] inputA = BuildInput(32, seed: 0);
        byte[] inputB = BuildInput(32, seed: 1);

        var genA = new SessionKeyGenerator(inputA, 32);
        var genB = new SessionKeyGenerator(inputB, 32);
        byte[] outA = new byte[40];
        byte[] outB = new byte[40];
        genA.Generate(outA, 40);
        genB.Generate(outB, 40);

        Assert.NotEqual(outA, outB);
    }

    [Fact]
    public void Generate_OddInputSize_HandlesUnevenSplit()
    {
        // Constructor splits at size/2 with the second half taking the remainder.
        byte[] input = BuildInput(33);
        byte[] expected = ReferenceGenerate(input, 33, 40);

        var gen = new SessionKeyGenerator(input, 33);
        byte[] output = new byte[40];
        gen.Generate(output, 40);

        Assert.Equal(expected, output);
    }
}
