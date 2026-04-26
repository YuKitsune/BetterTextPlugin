namespace BetterTextPlugin.Composer;

public class InboxMessageViewModel
{
    public string Display { get; }

    public InboxMessageViewModel(DateTime timestamp, string callsign, string content)
    {
        Display = $"[{timestamp:HH:mm:ss}] {callsign}: {content}";
    }
}
