using System.Numerics;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// A rigged character from the asset pipeline with its Idle / Happy / Talk clips.
/// Loops Idle, and returns to it after a one-shot clip finishes.
/// </summary>
public sealed class CharacterActor
{
    private readonly Dictionary<string, AnimationClip> _clips = new(StringComparer.OrdinalIgnoreCase);
    private AnimationPlayer? _player;
    private string _current = string.Empty;
    private bool _oneShot;
    private float _bob;

    public CharacterActor(Scene scene, string model, Vector3 position, float yaw = 0f, float scale = 1f, Node? parent = null)
    {
        Model = model;
        var (root, clips) = ModelLibrary.LoadAnimated(scene, model, parent);
        Root = root;
        Root.Position = position;
        Root.EulerAngles = new Vector3(0, yaw, 0);
        Root.Scale = new Vector3(scale);
        foreach (var clip in clips)
        {
            // Blender may suffix names (Idle.001); the part before the dot is the clip.
            var name = clip.Name.Split('.')[0];
            _clips.TryAdd(name, clip);
        }

        Play("Idle");
        if (_player is not null)
        {
            // Desynchronise characters so a group does not breathe in lockstep.
            _player.Time = Random.Shared.NextSingle() * 2f;
        }
    }

    public string Model { get; }

    public Node Root { get; }

    public bool HasClips => _clips.Count > 0;

    public void Play(string clip, bool loop = true)
    {
        if (!_clips.TryGetValue(clip, out var c))
        {
            return;
        }

        if (_current == clip && loop && _player is { IsPlaying: true })
        {
            return;
        }

        _player?.Stop();
        _player = c.Play(loop);
        _current = clip;
        _oneShot = !loop;
    }

    /// <summary>Plays a clip once, then goes back to Idle.</summary>
    public void Cheer() => Play("Happy", loop: false);

    public void Talk() => Play("Talk", loop: false);

    public void Update(float dt)
    {
        if (_oneShot && _player is { IsPlaying: false })
        {
            Play("Idle");
        }

        if (!HasClips)
        {
            // Unrigged fallback: a gentle bob so the character still feels alive.
            _bob += dt;
            var p = Root.Position;
            Root.Position = new Vector3(p.X, MathF.Abs(MathF.Sin(_bob * 2f)) * 0.03f, p.Z);
        }
    }

    public void Face(Vector3 target)
    {
        var d = target - Root.WorldPosition;
        Root.EulerAngles = new Vector3(0, MathF.Atan2(d.X, d.Z), 0);
    }

    public void Remove()
    {
        _player?.Stop();
        foreach (var clip in _clips.Values)
        {
            clip.Destroy();
        }

        _clips.Clear();
        Root.Remove();
    }
}
