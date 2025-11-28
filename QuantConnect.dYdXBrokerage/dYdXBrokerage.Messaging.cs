using QuantConnect.Logging;

namespace QuantConnect.Brokerages.dYdX;

public partial class dYdXBrokerage
{
    /// <summary>
    /// Wss message handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnMessage(object sender, WebSocketMessage e)
    {
        _messageHandler.HandleNewMessage(e);
    }

    private void OnMessageReceived(WebSocketMessage message)
    {
        Log.Debug(message.ToString());
    }
}