namespace RdpKeySwap.Settings;

/// <summary>
/// Compares the RDP client name against the registered Mac names. Names are trimmed,
/// compared case-insensitively, matched exactly, and de-duplicated.
/// </summary>
public static class ClientNameMatcher
{
    public static IReadOnlyList<string> Normalize(IEnumerable<string?>? names)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var raw in names ?? Enumerable.Empty<string?>())
        {
            var name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (seen.Add(name))
            {
                result.Add(name);
            }
        }

        return result;
    }

    public static bool Matches(string? clientName, IEnumerable<string?>? names)
    {
        var target = clientName?.Trim();
        if (string.IsNullOrEmpty(target))
        {
            return false;
        }

        foreach (var raw in names ?? Enumerable.Empty<string?>())
        {
            var name = raw?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (string.Equals(name, target, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
