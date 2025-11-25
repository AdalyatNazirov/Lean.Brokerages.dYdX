using QuantConnect.Brokerages.dYdX.Domain;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXApiClient
{
    private readonly dYdXIndexerClient _indexer;
    private readonly dYdXNodeClient _node;

    public dYdXApiClient(string nodeEndpointRest, string nodeEndpointGrpc, string indexerEndpointRest)
    {
        _indexer = new dYdXIndexerClient(indexerEndpointRest);
        _node = new dYdXNodeClient(nodeEndpointRest, nodeEndpointGrpc);
    }

    public dYdXAccount GetAccount(string address)
    {
        return _node.GetAccount(address);
    }

    public dYdXAccountBalances GetCashBalance(Wallet wallet)
    {
        return _node.GetCashBalance(wallet);
    }

    public dYdXPerpetualPositionsResponse GetOpenPerpetualPositions(Wallet wallet)
    {
        return _indexer.GetPerpetualPositions(wallet, "OPEN");
    }

    public bool PlaceOrder(Wallet wallet, Order order)
    {
        return _node.PlaceOrder(wallet, order);
    }
}