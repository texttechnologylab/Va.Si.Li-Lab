using System.Threading;
using System.Threading.Tasks;

namespace VaSiLi.Janus.TextRoom
{
    /// <summary>
    /// Strongly-typed handle for janus.plugin.textroom.
    /// The actual chat traffic flows over a WebRTC data channel (see <see cref="WebRTC.JanusTextRoomClient"/>).
    /// This handle only owns the SDP-setup leg: 'setup' -> server offers DC; 'ack' -> client posts SDP answer.
    /// </summary>
    public class TextRoomHandle : JanusHandle
    {
        public const string Plugin = "janus.plugin.textroom";

        public TextRoomHandle()
        {
            PluginName = Plugin;
        }

        /// <summary>Asks the server to send us a JSEP offer establishing the textroom data channel.</summary>
        public async Task<Jsep> SetupAsync(CancellationToken cancellation = default)
        {
            var resp = await SendMessageAsync(new TextRoomSetupRequest(), null, cancellation).ConfigureAwait(false);
            if (resp.Jsep == null)
                throw new JanusException("TextRoom setup did not return a JSEP offer");
            return resp.Jsep;
        }

        /// <summary>Posts the client's SDP answer back to complete the handshake.</summary>
        public Task AckAsync(Jsep answer, CancellationToken cancellation = default)
            => SendMessageAsync(new TextRoomAckRequest(), answer, cancellation);
    }
}
