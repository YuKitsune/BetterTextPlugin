using vatsys;
using vatsys.Plugin;
using BetterTextPlugin.Composer;

namespace BetterTextPlugin;

public class ColourCache
{
    public required CustomColour UnreadMessageColour { get; set; }
}

public class VoiceCapabilityLabelItemProvider(
    ColourCache colourCache,
    IComposerFactory composerFactory,
    IErrorReporter errorReporter)
{
    public CustomLabelItem? GetLabelItem(string itemType, FDP2.FDR fdr)
    {
        // vatSys bug: Custom background colours can't be drawn selectively.
        // vatSys won't draw the custom background if the original colour (specified in the Labels.xml file) is transparent (or empty).
        // To work around this, we define two label items. One with the background, and one without.
        // If we need to draw a custom background colour, we return `null` for the one without the background.

        var radioMessages = Network.GetRadioMessages;
        var lastTextMessage = radioMessages?
            .LastOrDefault(r => r.Address == fdr.Callsign && !r.Acknowledged);

        var text = " ";
        var backgroundColour = lastTextMessage is not null
            ? colourCache.UnreadMessageColour
            : null;

        if (fdr.TextOnly)
            text = "T";
        else if (fdr.ReceiveOnly)
            text = "R";
        else if (lastTextMessage is not null)
            text = "V";

        // vatSys bug: custom background colours can't be drawn selectively.
        // To work around this, we define two label items. One with the background, and one without.
        if (backgroundColour is not null && itemType != "BTP_LABEL_BG")
            return null;

        if (backgroundColour is null && itemType != "BTP_LABEL")
            return null;

        var labelItem = new CustomLabelItem
        {
            Type = itemType,
            Text = text,
            Border = string.IsNullOrWhiteSpace(text) ? BorderFlags.None : BorderFlags.All,
        };

        if (backgroundColour is not null)
        {
            labelItem.BackColourIdentity = Colours.Identities.Custom;
            labelItem.CustomBackColour = backgroundColour;
        }

        labelItem.OnMouseClick = args =>
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
                errorReporter.ReportError(ex, "Failed to handle label item click");
            }
        };

        return labelItem;
    }
}
