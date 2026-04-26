using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace BetterTextPlugin.Composer;

public partial class ComposerView : UserControl
{
    ComposerViewModel? ViewModel => DataContext as ComposerViewModel;

    public ComposerView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    void OnLoaded(object sender, RoutedEventArgs e)
    {
        SearchBox.Focus();
        UpdateInboxVisibility();
        UpdateBufferVisibility();
    }

    void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ComposerViewModel old)
        {
            old.PropertyChanged -= OnViewModelPropertyChanged;
            old.Buffer.CollectionChanged -= OnBufferChanged;
        }

        if (e.NewValue is ComposerViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            vm.Buffer.CollectionChanged += OnBufferChanged;
            UpdateInboxVisibility();
            UpdateBufferVisibility();
            UpdatePanelVisibility();
        }
    }

    void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        Dispatcher.Invoke(() =>
        {
            switch (e.PropertyName)
            {
                case nameof(ComposerViewModel.State):
                    UpdatePanelVisibility();
                    break;
                case nameof(ComposerViewModel.HighlightedIndex):
                    ScrollToHighlighted();
                    break;
            }
        });
    }

    void OnBufferChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        Dispatcher.Invoke(UpdateBufferVisibility);

    void UpdateInboxVisibility()
    {
        InboxSection.Visibility = ViewModel?.InboxMessages.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    void UpdateBufferVisibility()
    {
        BufferSection.Visibility = ViewModel?.Buffer.Count > 0
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    void UpdatePanelVisibility()
    {
        var isEditing = ViewModel?.State == ComposerState.Editing;

        SearchPanel.Visibility = isEditing ? Visibility.Collapsed : Visibility.Visible;
        EditPanel.Visibility   = isEditing ? Visibility.Visible   : Visibility.Collapsed;

        var focusTarget = isEditing ? (UIElement)ParamInput : SearchBox;
        Dispatcher.BeginInvoke(DispatcherPriority.Input, new Action(() => focusTarget.Focus()));
    }

    void ScrollToHighlighted()
    {
        var idx = ViewModel?.HighlightedIndex ?? -1;
        if (idx >= 0 && idx < ResultsList.Items.Count)
            ResultsList.ScrollIntoView(ResultsList.Items[idx]);
    }

    void OnMouseLeave(object sender, MouseEventArgs e)
    {
        ViewModel?.Close();
    }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        var vm = ViewModel;
        if (vm is null) return;

        var shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;

        switch (e.Key)
        {
            case Key.Up:
                vm.MoveUp();
                e.Handled = true;
                break;

            case Key.Down:
                vm.MoveDown();
                e.Handled = true;
                break;

            case Key.Enter when shift:
                vm.AddToBuffer();
                e.Handled = true;
                break;

            case Key.Enter:
                vm.Confirm();
                e.Handled = true;
                break;

            // Tab advances params in edit mode; normal focus navigation otherwise
            case Key.Tab when vm.State == ComposerState.Editing:
                vm.Confirm();
                e.Handled = true;
                break;

            case Key.Escape:
                vm.Close();
                e.Handled = true;
                break;
        }
    }
}
