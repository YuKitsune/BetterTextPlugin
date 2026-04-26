using MediatR;
using vatsys;

namespace BetterTextPlugin.Composer;

public class MessageReadyHandler : INotificationHandler<MessageReadyNotification>
{
    public Task Handle(MessageReadyNotification notification, CancellationToken cancellationToken)
    {
        Network.SendRadioMessage($"{notification.Callsign}, {notification.Message}");
        return Task.CompletedTask;
    }
}
