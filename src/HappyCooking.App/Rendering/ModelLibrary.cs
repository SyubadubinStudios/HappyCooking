using Avalonia.Platform;
using ThreeNet;

namespace HappyCooking.App.Rendering;

/// <summary>
/// Loads the bundled GLB models into a scene. The encoded bytes are read from the
/// app package once and kept (each file is 0.3-0.7 MB), so loading the same model
/// into a new scene never touches storage again. Static props are cloned within a
/// scene so their GPU geometry and textures are shared.
/// </summary>
public static class ModelLibrary
{
    private static readonly Dictionary<string, byte[]> s_bytes = [];
    private static readonly Lock s_gate = new();

    public static bool Exists(string name) =>
        AssetLoader.Exists(new Uri($"avares://HappyCooking.App/Assets/Models/{name}.glb"));

    private static byte[] Bytes(string name)
    {
        lock (s_gate)
        {
            if (!s_bytes.TryGetValue(name, out var bytes))
            {
                using var stream = AssetLoader.Open(new Uri($"avares://HappyCooking.App/Assets/Models/{name}.glb"));
                using var ms = new MemoryStream();
                stream.CopyTo(ms);
                bytes = ms.ToArray();
                s_bytes[name] = bytes;
            }

            return bytes;
        }
    }

    /// <summary>
    /// Loads a fresh copy with its own animation clips. Characters use this so each
    /// one can animate independently.
    /// </summary>
    public static (Node Root, IReadOnlyList<AnimationClip> Clips) LoadAnimated(Scene scene, string name, Node? parent = null)
    {
        var before = scene.Animations.Count;
        var result = scene.LoadGltf(Bytes(name), parent);
        var clips = new List<AnimationClip>();
        for (var i = before; i < scene.Animations.Count; i++)
        {
            clips.Add(scene.Animations[i]);
        }

        return (result.Root, clips);
    }

    /// <summary>A static model; repeats in the same scene are cheap clones.</summary>
    public static Node LoadStatic(Scene scene, string name, Node? parent = null)
    {
        var cache = s_sceneCache.GetOrCreateValue(scene);
        if (cache.TryGetValue(name, out var template))
        {
            var clone = template.Clone(parent);
            clone.Visible = true;
            return clone;
        }

        var root = scene.LoadGltf(Bytes(name), parent).Root;
        // Keep a hidden template so later requests clone instead of re-importing.
        var hidden = root.Clone();
        hidden.Visible = false;
        cache[name] = hidden;
        return root;
    }

    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<Scene, Dictionary<string, Node>> s_sceneCache = [];
}
