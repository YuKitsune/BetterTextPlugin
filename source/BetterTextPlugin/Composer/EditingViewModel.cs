using System.Text;
using System.Text.RegularExpressions;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BetterTextPlugin.Composer;

public partial class EditingViewModel : ObservableObject
{
    static readonly Regex ParamPattern = new(@"\$([A-Z_]+)", RegexOptions.Compiled);

    readonly string _templateContent;
    readonly List<ParamSlot> _params;
    readonly List<int> _editableIndices; // indices into _params for unresolved params
    int _currentEditableIndex;

    public PendingAction PendingAction { get; }

    [ObservableProperty] string _text = string.Empty;
    [ObservableProperty] int _selectionStart;
    [ObservableProperty] int _selectionLength;

    public string CurrentParamName => HasCurrent ? $"${_params[CurrentParamIndex].Name}" : string.Empty;

    // Two-way bindable value for the currently active parameter.
    public string CurrentParamValue
    {
        get => HasCurrent ? _params[CurrentParamIndex].Value : string.Empty;
        set
        {
            if (!HasCurrent) return;
            _params[CurrentParamIndex] = _params[CurrentParamIndex] with { Value = value };
            Refresh();
            OnPropertyChanged();
        }
    }

    public EditingViewModel(ResolvedTemplate resolved, PendingAction pendingAction)
    {
        PendingAction = pendingAction;
        _templateContent = resolved.TemplateContent;

        _params = resolved.Params
            .Select(p => new ParamSlot(p.Name, p.Value ?? string.Empty))
            .ToList();

        _editableIndices = resolved.Params
            .Select((p, i) => (p, i))
            .Where(x => x.p.Value is null)
            .Select(x => x.i)
            .ToList();

        _currentEditableIndex = 0;
        Refresh();
    }

    bool HasCurrent => _currentEditableIndex < _editableIndices.Count;
    int CurrentParamIndex => HasCurrent ? _editableIndices[_currentEditableIndex] : -1;

    // Returns true if there are more params to fill; false when editing is complete.
    public bool Advance()
    {
        _currentEditableIndex++;
        Refresh();
        return _currentEditableIndex < _editableIndices.Count;
    }

    public string BuildText()
    {
        var (text, _, _) = Render();
        return text;
    }

    void Refresh()
    {
        var (text, selStart, selLen) = Render();
        Text = text;
        SelectionStart = selStart;
        SelectionLength = selLen;
        OnPropertyChanged(nameof(CurrentParamName));
        OnPropertyChanged(nameof(CurrentParamValue));
    }

    (string Text, int SelectionStart, int SelectionLength) Render()
    {
        var sb = new StringBuilder();
        int paramIdx = 0;
        int contentPos = 0;
        int selStart = 0, selLen = 0;

        foreach (Match match in ParamPattern.Matches(_templateContent))
        {
            sb.Append(_templateContent.Substring(contentPos, match.Index - contentPos));

            var slot = _params[paramIdx];
            var display = string.IsNullOrEmpty(slot.Value) ? match.Value : slot.Value;

            if (paramIdx == CurrentParamIndex)
            {
                selStart = sb.Length;
                selLen = display.Length;
            }

            sb.Append(display);
            paramIdx++;
            contentPos = match.Index + match.Length;
        }

        sb.Append(_templateContent.Substring(contentPos));

        return (sb.ToString(), selStart, selLen);
    }

    record ParamSlot(string Name, string Value);
}
