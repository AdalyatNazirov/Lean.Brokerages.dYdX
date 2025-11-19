using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXApiClient
{
    private readonly Wallet _wallet;

    private readonly dYdXIndexerClient _indexer;
    private readonly dYdXNodeClient _node;

    public dYdXApiClient(Wallet wallet, string nodeApiUrl, string indexerApiUrl)
    {
        _wallet = wallet;
        _indexer = new dYdXIndexerClient(indexerApiUrl);
        _node = new dYdXNodeClient(_wallet, nodeApiUrl);
    }

    public dYdXAccountBalances GetCashBalance()
    {
        return _node.GetCashBalance();
    }

    public dYdXPerpetualPositionsResponse GetOpenPerpetualPositions(int subaccountNumber)
    {
        return _indexer.GetPerpetualPositions(_wallet.Address, subaccountNumber, "OPEN");
    }

    public bool PlaceOrder(Order order)
    {
        return _node.PlaceOrder(order);
    }
}