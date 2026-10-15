using System;
using System.Threading;
using System.Threading.Tasks;

namespace VaSiLi.Janus
{
    public enum JanusTransportState
    {
        Closed,
        Connecting,
        Open,
        Closing
    }

    public interface IJanusTransport
    {
        JanusTransportState State { get; }
        event EventHandler Opened;
        event EventHandler<string> Message;
        event EventHandler<string> Error;
        event EventHandler<string> Closed;

        Task OpenAsync(CancellationToken cancellation = default);
        Task SendAsync(string text, CancellationToken cancellation = default);
        Task CloseAsync(CancellationToken cancellation = default);
    }
}
