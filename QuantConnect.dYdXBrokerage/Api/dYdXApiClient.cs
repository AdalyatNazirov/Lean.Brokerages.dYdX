namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXApiClient(string nodeEndpointRest, string nodeEndpointGrpc, string indexerEndpointRest)
{
    public dYdXIndexerClient Indexer { get; init; } = new(indexerEndpointRest);
    public dYdXNodeClient Node { get; init; } = new(nodeEndpointRest, nodeEndpointGrpc);
}