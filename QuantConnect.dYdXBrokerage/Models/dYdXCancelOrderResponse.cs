namespace QuantConnect.Brokerages.dYdX.Models;

public class dYdXCancelOrderResponse
{
    public uint Code { get; init; }
    public uint OrderId { get; init; }
    public string Message { get; init; }
    public string TxHash { get; init; }
}