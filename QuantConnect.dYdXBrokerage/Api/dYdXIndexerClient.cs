using System;
using QuantConnect.Brokerages.dYdX.Models;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXIndexerClient(string baseUrl)
{
    private readonly Lazy<dYdXRestClient> _lazyRestClient = new(() => new dYdXRestClient(baseUrl.TrimEnd('/')));
    private dYdXRestClient _restClient => _lazyRestClient.Value;

    /// <summary>
    /// Calls indexer to get perpetual positions, see https://docs.dydx.xyz/indexer-client/http#list-positions
    /// </summary>
    /// <param name="wallet">Wallet to retrieve positions for</param>
    /// <param name="status">Filter to retrieve positions with a specific status. If not provided, all positions will be returned regardless of status. Defaults to "OPEN".</param>
    /// <returns></returns>
    public dYdXPerpetualPositionsResponse GetPerpetualPositions(
        Wallet wallet,
        string status = "OPEN")
    {
        var path =
            $"/v4/perpetualPositions?address={Uri.EscapeDataString(wallet.Address)}&subaccountNumber={wallet.SubaccountNumber}&status={Uri.EscapeDataString(status)}";
        return _restClient.Get<dYdXPerpetualPositionsResponse>(path);
    }
}