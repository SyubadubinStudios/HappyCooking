using ThreeNet;

namespace HappyCooking.App.Services;

public enum Sound
{
    Tap,
    Pop,
    Good,
    Perfect,
    Oops,
    Coin,
    Splash,
    LevelUp,
    Sizzle,
}

/// <summary>
/// Sound effects and background music, synthesised at start-up from a few sine and
/// noise envelopes. No audio files to ship, about 1.5 MB of samples in memory, and
/// it degrades to silence when the device has no audio output.
/// </summary>
public sealed class Sfx : IDisposable
{
    private const int Rate = 22050;
    private readonly AudioEngine? _engine;
    private readonly Dictionary<Sound, AudioClip> _clips = [];
    private AudioClip? _music;
    private SoundInstance? _musicInstance;
    private float _effectsVolume = 0.8f;
    private float _musicVolume = 0.6f;

    public Sfx()
    {
        try
        {
            _engine = AudioEngine.Open();
            Build();
        }
        catch (Exception ex) when (ex is ThreeNetException or DllNotFoundException or InvalidOperationException)
        {
            _engine = null;
        }
    }

    public bool IsAvailable => _engine is not null;

    public float EffectsVolume
    {
        get => _effectsVolume;
        set => _effectsVolume = Math.Clamp(value, 0f, 1f);
    }

    public float MusicVolume
    {
        get => _musicVolume;
        set
        {
            _musicVolume = Math.Clamp(value, 0f, 1f);
            _musicInstance?.Update(o => o with { Gain = _musicVolume * 0.35f });
        }
    }

    public void Play(Sound sound, float pitch = 1f)
    {
        if (_engine is null || _effectsVolume <= 0f || !_clips.TryGetValue(sound, out var clip))
        {
            return;
        }

        var options = SoundOptions.Flat(_effectsVolume);
        options.Pitch = pitch;
        _engine.Play(clip, ref options);
    }

    public void StartMusic()
    {
        if (_engine is null || _music is null || _musicInstance is { IsPlaying: true })
        {
            return;
        }

        var options = SoundOptions.Flat(_musicVolume * 0.35f, loop: true);
        _musicInstance = _engine.Play(_music, ref options);
    }

    private void Build()
    {
        Add(Sound.Tap, Tone(880, 0.05f, 0.4f, decay: 60));
        Add(Sound.Pop, Sweep(500, 1100, 0.09f, 0.5f));
        Add(Sound.Good, Chord([659, 880], 0.25f, 0.4f));
        Add(Sound.Perfect, Arpeggio([523, 659, 784, 1047], 0.08f, 0.45f));
        Add(Sound.Oops, Sweep(400, 180, 0.3f, 0.45f));
        Add(Sound.Coin, Arpeggio([988, 1319], 0.07f, 0.4f));
        Add(Sound.Splash, Noise(0.35f, 0.35f, decay: 9));
        Add(Sound.LevelUp, Arpeggio([523, 659, 784, 1047, 1319], 0.1f, 0.45f));
        Add(Sound.Sizzle, Noise(0.6f, 0.15f, decay: 3));
        _music = _engine!.CreateClip(Music(), 1, Rate);
    }

    private void Add(Sound sound, float[] samples) => _clips[sound] = _engine!.CreateClip(samples, 1, Rate);

    private static float[] Tone(float freq, float seconds, float gain, float decay)
    {
        var n = (int)(seconds * Rate);
        var data = new float[n];
        for (var i = 0; i < n; i++)
        {
            var t = (float)i / Rate;
            data[i] = MathF.Sin(2 * MathF.PI * freq * t) * gain * MathF.Exp(-decay * t);
        }

        return data;
    }

    private static float[] Sweep(float from, float to, float seconds, float gain)
    {
        var n = (int)(seconds * Rate);
        var data = new float[n];
        var phase = 0f;
        for (var i = 0; i < n; i++)
        {
            var k = (float)i / n;
            phase += 2 * MathF.PI * (from + ((to - from) * k)) / Rate;
            data[i] = MathF.Sin(phase) * gain * (1 - k) * MathF.Min(1, i / 60f);
        }

        return data;
    }

    private static float[] Chord(float[] freqs, float seconds, float gain)
    {
        var n = (int)(seconds * Rate);
        var data = new float[n];
        for (var i = 0; i < n; i++)
        {
            var t = (float)i / Rate;
            var s = 0f;
            foreach (var f in freqs)
            {
                s += MathF.Sin(2 * MathF.PI * f * t);
            }

            data[i] = s / freqs.Length * gain * MathF.Exp(-6 * t) * MathF.Min(1, i / 40f);
        }

        return data;
    }

    private static float[] Arpeggio(float[] notes, float noteSeconds, float gain)
    {
        var per = (int)(noteSeconds * Rate);
        var tail = (int)(0.25f * Rate);
        var data = new float[(per * notes.Length) + tail];
        for (var k = 0; k < notes.Length; k++)
        {
            for (var i = 0; i < per + tail && (k * per) + i < data.Length; i++)
            {
                var t = (float)i / Rate;
                data[(k * per) + i] += MathF.Sin(2 * MathF.PI * notes[k] * t) * gain * 0.6f * MathF.Exp(-9 * t) * MathF.Min(1, i / 30f);
            }
        }

        return data;
    }

    private static float[] Noise(float seconds, float gain, float decay)
    {
        var n = (int)(seconds * Rate);
        var data = new float[n];
        var rng = new Random(7);
        var last = 0f;
        for (var i = 0; i < n; i++)
        {
            var t = (float)i / Rate;
            // One-pole low-pass keeps the hiss soft.
            last += (((float)rng.NextDouble() * 2) - 1 - last) * 0.35f;
            data[i] = last * gain * MathF.Exp(-decay * t);
        }

        return data;
    }

    /// <summary>A gentle 16-bar pentatonic loop: soft plucked melody over a bouncing bass.</summary>
    private static float[] Music()
    {
        const float bpm = 104f;
        var beat = 60f / bpm;
        int[] melody =
        [
            0, 2, 4, 7, 9, 7, 4, 2, 0, 2, 4, 2, 0, -3, 0, -1,
            4, 7, 9, 12, 9, 7, 4, 7, 9, 7, 4, 2, 4, 2, 0, -1,
            0, 4, 7, 4, 9, 7, 4, 2, 0, 2, 4, 7, 4, 2, 0, -1,
            7, 9, 12, 9, 7, 4, 2, 4, 2, 0, 2, 4, 2, 0, 0, -1,
        ];
        int[] bass = [0, 0, 5, 5, 7, 7, 5, 5, 0, 0, 5, 5, 7, 7, 0, 0];
        var eighths = melody.Length;
        var total = (int)(eighths * beat * 0.5f * Rate);
        var data = new float[total];
        var noteLen = (int)(beat * 0.5f * Rate);

        for (var k = 0; k < eighths; k++)
        {
            if (melody[k] < 0)
            {
                continue;
            }

            var freq = 523.25f * MathF.Pow(2, melody[k] / 12f);
            for (var i = 0; i < noteLen * 2 && (k * noteLen) + i < total; i++)
            {
                var t = (float)i / Rate;
                var env = MathF.Exp(-5 * t) * MathF.Min(1, i / 80f);
                data[(k * noteLen) + i] += (MathF.Sin(2 * MathF.PI * freq * t) + (0.3f * MathF.Sin(4 * MathF.PI * freq * t))) * 0.18f * env;
            }
        }

        var barLen = noteLen * 4;
        for (var b = 0; b < bass.Length; b++)
        {
            var freq = 130.81f * MathF.Pow(2, bass[b] / 12f);
            for (var hit = 0; hit < 2; hit++)
            {
                var start = (b * barLen) + (hit * noteLen * 2);
                for (var i = 0; i < noteLen * 2 && start + i < total; i++)
                {
                    var t = (float)i / Rate;
                    data[start + i] += MathF.Sin(2 * MathF.PI * freq * t) * 0.22f * MathF.Exp(-4 * t) * MathF.Min(1, i / 100f);
                }
            }
        }

        return data;
    }

    public void Dispose()
    {
        _musicInstance?.Stop();
        _engine?.Dispose();
    }
}
