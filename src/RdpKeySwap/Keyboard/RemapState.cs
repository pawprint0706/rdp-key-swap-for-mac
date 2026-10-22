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
/// A press is only recorded after its replacement was actually delivered.
/// </summary>
public sealed class RemapState
{
    private readonly Dictionary<uint, KeyEmission> _held = new();
    private readonly HashSet<uint> _swallowUp = new();

    public int HeldCount => _held.Count;

    public bool IsHolding(uint sourceKey) => _held.ContainsKey(sourceKey);

    /// <summary>The replacement a press would produce, before anything is delivered.</summary>
    public static bool TryGetDown(uint sourceKey, out KeyEmission down) => KeyMap.TryMap(sourceKey, out down);

    /// <summary>True when the release of this key must be dropped instead of forwarded.</summary>
    public bool TryConsumeSwallowedUp(uint sourceKey) => _swallowUp.Remove(sourceKey);

    /// <summary>Records a press whose replacement reached the system, so its release is replaced too.</summary>
    public void MarkDown(uint sourceKey, KeyEmission down)
    {
        // A new press supersedes any pending release of that key.
        _swallowUp.Remove(sourceKey);
        _held[sourceKey] = down;
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

        // No tracked press: either the replacement was never delivered (the original press was
        // forwarded) or the program started while the key was held. The system saw the original
        // key go down, so the release must reach it unchanged.
        return RemapOutcome.NotMapped;
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
