namespace BetterTextPlugin.Composer;

// Result of resolving a Template against an FdrSnapshot.
// Params is ordered as they appear in the template content;
// a null Value means the param could not be resolved from the FDR.
public record ResolvedTemplate(
    string DisplayText,
    bool HasUnresolvedParams,
    IReadOnlyList<(string Name, string? Value)> Params,
    string TemplateContent);
