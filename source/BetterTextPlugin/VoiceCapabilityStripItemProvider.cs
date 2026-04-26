using vatsys;
using vatsys.Plugin;
using BetterTextPlugin.Composer;

namespace BetterTextPlugin;

public class VoiceCapabilityStripItemProvider(
    IComposerFactory composerFactory,
    IErrorReporter errorReporter)
{
    public CustomStripItem? GetStripItem(string itemType, FDP2.FDR fdr)
    {
        var radioMessages = Network.GetRadioMessages;
        var lastTextMessage = radioMessages?
            .LastOrDefault(r => r.Address == fdr.Callsign && !r.Acknowledged);

        var text = " ";

        if (fdr.TextOnly)
            text = "T";
        else if (fdr.ReceiveOnly)
            text = "R";
        else if (lastTextMessage is not null)
            text = "V";

        var stripItem = new CustomStripItem
        {
            Text = text,
            Border = string.IsNullOrWhiteSpace(text) ? BorderFlags.None : BorderFlags.All,
        };

        stripItem.OnMouseClick = args =>
        {
            try
            {
                if (args.Button != CustomLabelItemMouseButton.Left)
                    return;

                composerFactory.Open(fdr);
                args.Handled = true;
            }
            catch (Exception ex)
            {
                errorReporter.ReportError(ex, "Failed to handle strip item click");
            }
        };

        return stripItem;
    }
}
