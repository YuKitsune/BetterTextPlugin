namespace BetterTextPlugin.Composer;

public class TemplateResultViewModel
{
    public string DisplayText { get; }
    public bool HasUnresolvedParams { get; }

    // Retained for ComposerViewModel to pass to EditingViewModel
    internal ResolvedTemplate ResolvedTemplate { get; }

    public TemplateResultViewModel(ResolvedTemplate resolved)
    {
        DisplayText = resolved.DisplayText;
        HasUnresolvedParams = resolved.HasUnresolvedParams;
        ResolvedTemplate = resolved;
    }
}
