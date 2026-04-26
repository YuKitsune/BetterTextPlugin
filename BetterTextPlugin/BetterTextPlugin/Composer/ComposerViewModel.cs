using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using MediatR;

namespace BetterTextPlugin.Composer;

public partial class ComposerViewModel : ObservableObject
{
    readonly ITemplateSearchService _search;
    readonly IMediator _mediator;
    readonly FdrSnapshot _fdr;
    readonly IReadOnlyList<Template> _templates;

    public IReadOnlyList<InboxMessageViewModel> InboxMessages { get; }
    public ObservableCollection<string> Buffer { get; } = [];

    [ObservableProperty] ComposerState _state = ComposerState.Searching;
    [ObservableProperty] string _searchQuery = string.Empty;
    [ObservableProperty] ObservableCollection<TemplateResultViewModel> _searchResults = [];
    [ObservableProperty] int _highlightedIndex = -1;
    [ObservableProperty] EditingViewModel? _editing;

    public event Action? CloseRequested;

    public ComposerViewModel(
        FdrSnapshot fdr,
        IReadOnlyList<Template> templates,
        IReadOnlyList<InboxMessageViewModel> inboxMessages,
        ITemplateSearchService search,
        IMediator mediator)
    {
        _fdr = fdr;
        _templates = templates;
        _search = search;
        _mediator = mediator;
        InboxMessages = inboxMessages;
        RefreshResults();
    }

    partial void OnSearchQueryChanged(string value) => RefreshResults();

    public void MoveUp()
    {
        if (State != ComposerState.Searching) return;
        HighlightedIndex = Math.Max(0, HighlightedIndex - 1);
    }

    public void MoveDown()
    {
        if (State != ComposerState.Searching) return;
        HighlightedIndex = Math.Min(SearchResults.Count - 1, HighlightedIndex + 1);
    }

    // Enter
    public void Confirm()
    {
        if (State == ComposerState.Searching)
            BeginOrSend(PendingAction.Send);
        else
            AdvanceEditing();
    }

    // Shift+Enter
    public void AddToBuffer()
    {
        if (State != ComposerState.Searching) return;
        BeginOrSend(PendingAction.AddToBuffer);
    }

    public void Close()
    {
        CloseRequested?.Invoke();
    }

    void BeginOrSend(PendingAction pendingAction)
    {
        if (SearchResults.Count == 0 || HighlightedIndex < 0) return;

        var result = SearchResults[HighlightedIndex];

        if (result.HasUnresolvedParams)
        {
            Editing = new EditingViewModel(result.ResolvedTemplate, pendingAction);
            State = ComposerState.Editing;
            return;
        }

        if (pendingAction == PendingAction.Send)
            Transmit(result.DisplayText);
        else
            AppendBuffer(result.DisplayText);
    }

    void AdvanceEditing()
    {
        if (Editing is null) return;

        bool hasMore = Editing.Advance();
        if (hasMore) return;

        var text = Editing.BuildText();
        var pending = Editing.PendingAction;

        Editing = null;
        State = ComposerState.Searching;

        if (pending == PendingAction.Send)
            Transmit(text);
        else
            AppendBuffer(text);
    }

    void AppendBuffer(string text)
    {
        Buffer.Add(text);
        SearchQuery = string.Empty;
    }

    void Transmit(string text)
    {
        var message = string.Join(". ", Buffer.Append(text)) + ".";
        _ = _mediator.Publish(new MessageReadyNotification(_fdr.Callsign, message));
        Close();
    }

    void RefreshResults()
    {
        var results = _search.Search(SearchQuery, _templates, _fdr);
        SearchResults.Clear();
        foreach (var r in results)
            SearchResults.Add(r);
        HighlightedIndex = SearchResults.Count > 0 ? 0 : -1;
    }
}
