using System.Windows;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using vatsys;

namespace BetterTextPlugin.Composer;

public class ComposerWindow : BaseForm
{
    readonly ComposerViewModel _viewModel;

    public ComposerWindow(ComposerViewModel viewModel, System.Drawing.Point location)
    {
        _viewModel = viewModel;
        _viewModel.CloseRequested += OnCloseRequested;

        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        Location = location;

        var view = new ComposerView { DataContext = viewModel };

        // Measure the WPF content to size the form to its initial content
        view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        view.Arrange(new Rect(view.DesiredSize));
        view.UpdateLayout();

        var desired = view.DesiredSize;
        var elementHost = new ElementHost
        {
            Child = view,
            Size = new System.Drawing.Size(
                (int)Math.Ceiling(desired.Width),
                (int)Math.Ceiling(desired.Height))
        };

        Controls.Add(elementHost);
        ClientSize = elementHost.Size;
        Padding = new Padding(0);
        Margin = new Padding(0);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        var centre = new System.Drawing.Point(
            Left + Width / 2,
            Top + Height / 2);
        Cursor.Position = centre;
    }

    void OnCloseRequested()
    {
        if (InvokeRequired)
            Invoke(new Action(OnCloseRequested));
        else
            Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _viewModel.CloseRequested -= OnCloseRequested;
        base.OnFormClosed(e);
    }
}
