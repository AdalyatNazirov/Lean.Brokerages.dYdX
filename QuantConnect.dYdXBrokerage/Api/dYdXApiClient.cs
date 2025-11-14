using System.Collections.Generic;
using QuantConnect.Brokerages.dYdX.Models;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXApiClient
{
    private readonly string _address;

    private readonly dYdXIndexerClient _indexer;
    private readonly dYdXNodeClient _node;

    public dYdXApiClient(string address, string nodeApiUrl)
    {
        _address = address;
        _indexer = new dYdXIndexerClient();
        _node = new dYdXNodeClient(nodeApiUrl);
    }

    public dYdXAccountBalances GetCashBalance()
    {
        return _node.GetCashBalance(_address);
    }
}