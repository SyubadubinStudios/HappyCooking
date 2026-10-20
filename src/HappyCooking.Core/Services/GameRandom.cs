namespace HappyCooking.Core.Services;

/// <summary>
/// Small, fast, seedable random generator (xoshiro128**). No allocations per call,
/// so it is safe inside per-frame updates, and seedable so tests are deterministic.
/// </summary>
public sealed class GameRandom
{
    private uint _s0, _s1, _s2, _s3;

    public GameRandom(int seed)
    {
        // SplitMix32 spreads the seed so nearby seeds give unrelated sequences.
        var x = (uint)seed;
        _s0 = Mix(ref x);
        _s1 = Mix(ref x);
        _s2 = Mix(ref x);
        _s3 = Mix(ref x);
    }

    public GameRandom()
        : this(Environment.TickCount)
    {
    }

    private static uint Mix(ref uint x)
    {
        var z = x += 0x9E3779B9;
        z = (z ^ (z >> 16)) * 0x85EBCA6B;
        z = (z ^ (z >> 13)) * 0xC2B2AE35;
        return z ^ (z >> 16);
    }

    public uint NextUInt()
    {
        var result = Rotl(_s1 * 5, 7) * 9;
        var t = _s1 << 9;
        _s2 ^= _s0;
        _s3 ^= _s1;
        _s1 ^= _s2;
        _s0 ^= _s3;
        _s2 ^= t;
        _s3 = Rotl(_s3, 11);
        return result;
    }

    private static uint Rotl(uint v, int k) => (v << k) | (v >> (32 - k));

    /// <summary>A float in [0, 1).</summary>
    public float NextFloat() => (NextUInt() >> 8) * (1f / 16777216f);

    public float Range(float min, float max) => min + (NextFloat() * (max - min));

    /// <summary>An int in [min, max).</summary>
    public int Range(int min, int max) => max <= min ? min : min + (int)(NextUInt() % (uint)(max - min));

    public bool Chance(float probability) => NextFloat() < probability;

    public T Pick<T>(IReadOnlyList<T> items) => items[Range(0, items.Count)];

    /// <summary>Fisher-Yates shuffle in place.</summary>
    public void Shuffle<T>(IList<T> items)
    {
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
