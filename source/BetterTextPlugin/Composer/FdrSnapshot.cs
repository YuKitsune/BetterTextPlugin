using System.Diagnostics.CodeAnalysis;

namespace BetterTextPlugin.Composer;

// Captures a point-in-time view of the FDR fields needed for parameter resolution.
// Add new fields by extending the dictionary passed to the constructor.
public class FdrSnapshot
{
    public string Callsign { get; }

    readonly IReadOnlyDictionary<string, string?> _params;

    public FdrSnapshot(string callsign, IReadOnlyDictionary<string, string?> parameters)
    {
        Callsign = callsign;
        _params = parameters;
    }

    public bool TryResolve(string name, out string? value) =>
        _params.TryGetValue(name, out value);
}
