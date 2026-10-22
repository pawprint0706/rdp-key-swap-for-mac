namespace RdpKeySwap.Keyboard;

public enum RemapOutcome
{
    /// <summary>The key is not part of the mapping and must be forwarded unchanged.</summary>
    NotMapped,

    /// <summary>The key was replaced by a synthesized event.</summary>
    Synthesized,

    /// <summary>The key must be dropped without producing anything.</summary>
    Swallowed,
}

/// <summary>
/// Remembers which source keys currently hold a synthesized key down, so that the matching
/// key-up is released with the same rule even if the apply condition changes in between.
/// </summary>
public sealed class RemapState
{
    private readonly Dictionary<uint, KeyEmission> _held = new();
    private readonly HashSet<uint> _swallowUp = new();

    public int HeldCount => _held.Count;

    public bool IsHolding(uint sourceKey) => _held.ContainsKey(sourceKey);

    /// <summary>True when the release of this key must be dropped instead of forwarded.</summary>
    public bool TryConsumeSwallowedUp(uint sourceKey) => _swallowUp.Remove(sourceKey);

    public RemapOutcome HandleDown(uint sourceKey, out KeyEmission emission)
    {
        emission = default;

        if (!KeyMap.TryMap(sourceKey, out var down))
        {
            return RemapOutcome.NotMapped;
        }

        // A new press supersedes any pending release of that key.
        _swallowUp.Remove(sourceKey);
        _held[sourceKey] = down;
        emission = down;
        return RemapOutcome.Synthesized;
    }

    public RemapOutcome HandleUp(uint sourceKey, out KeyEmission emission)
    {
        emission = default;

        if (_swallowUp.Remove(sourceKey))
        {
            return RemapOutcome.Swallowed;
        }

        if (_held.Remove(sourceKey, out var down))
        {
            emission = down.AsUp();
            return RemapOutcome.Synthesized;
        }

        // The press was never seen (the program started while the key was held): drop the
        // orphan release instead of leaking an unmatched key-up for a key it replaced.
        return KeyMap.IsMapped(sourceKey) ? RemapOutcome.Swallowed : RemapOutcome.NotMapped;
    }

    /// <summary>
    /// Releases every synthesized key that is still held and remembers the source keys so
    /// their eventual key-up is dropped.
    /// </summary>
    public IReadOnlyList<KeyEmission> ReleaseAll()
    {
        if (_held.Count == 0)
        {
            return Array.Empty<KeyEmission>();
        }

        var releases = new List<KeyEmission>(_held.Count);
        foreach (var (sourceKey, down) in _held)
        {
            _swallowUp.Add(sourceKey);
            releases.Add(down.AsUp());
        }

        _held.Clear();
        return releases;
    }
}
