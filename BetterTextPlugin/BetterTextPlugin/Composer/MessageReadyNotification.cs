using MediatR;

namespace BetterTextPlugin.Composer;

public record MessageReadyNotification(string Callsign, string Message) : INotification;
