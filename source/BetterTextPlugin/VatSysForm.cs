using System.Windows;
using System.Windows.Forms.Integration;
using System.Windows.Forms;
using vatsys;

namespace BetterTextPlugin;

public class VatSysForm : BaseForm
{
    public event FormClosingEventHandler? CustomFormClosing;

    bool _forceClosing;

    public void ForceClose()
    {
        _forceClosing = true;
        Close();
    }

    public VatSysForm(string title, Func<UIElement> childFactory, bool shrinkToContent)
    {
        MiddleClickClose = true;
        Text = title;

        var child = childFactory();
        var elementHost = new ElementHost();

        if (shrinkToContent)
        {
            child.Measure(new Size(1000, double.PositiveInfinity));
            child.Arrange(new Rect(child.DesiredSize));
            child.UpdateLayout();

            var desired = child.DesiredSize;
            elementHost.Child = child;
            elementHost.Size = new System.Drawing.Size((int)Math.Ceiling(desired.Width), (int)Math.Ceiling(desired.Height));
            elementHost.Location = new System.Drawing.Point(0, 0);

            FormBorderStyle = FormBorderStyle.FixedDialog;
            ClientSize = elementHost.Size;
        }
        else
        {
            elementHost.Child = child;
            elementHost.Dock = DockStyle.Fill;
        }

        Controls.Add(elementHost);
        Padding = new Padding(0);
        Margin = new Padding(0);
        PerformLayout();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (!_forceClosing && CustomFormClosing != null)
        {
            CustomFormClosing.Invoke(this, e);
            if (e.Cancel)
                return;
        }

        base.OnFormClosing(e);
    }
}
