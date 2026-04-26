using System.Diagnostics;
using MediatR;

namespace BetterTextPlugin.Composer;

public class MessageReadyHandler : INotificationHandler<MessageReadyNotification>
{
    public Task Handle(MessageReadyNotification notification, CancellationToken cancellationToken)
    {
        Debug.WriteLine($"Sending message \"{notification.Callsign}, {notification.Message}\"");
        // TODO: Network.SendRadioMessage($"{notification.Callsign}, {notification.Message}");
        return Task.CompletedTask;
    }
}
