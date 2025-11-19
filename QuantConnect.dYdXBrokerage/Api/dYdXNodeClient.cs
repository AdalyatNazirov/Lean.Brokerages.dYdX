using System;
using System.Net.Http;
using Google.Protobuf;
using Grpc.Net.Client;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.dYdXBrokerage.Cosmos.Tx;
using QuantConnect.Orders;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXNodeClient
{
    private readonly Wallet _wallet;
    private readonly string _baseUrl;
    private readonly Lazy<dYdXRestClient> _lazyRestClient;
    private readonly GrpcChannelOptions _grpcChannelOptions;

    private dYdXRestClient _restClient => _lazyRestClient.Value;

    public dYdXNodeClient(Wallet wallet, string baseUrl)
    {
        _wallet = wallet;
        _baseUrl = baseUrl;
        _lazyRestClient = new(() => new dYdXRestClient(baseUrl.TrimEnd('/')));
        _grpcChannelOptions = new GrpcChannelOptions
        {
            HttpHandler = new SocketsHttpHandler
            {
                KeepAlivePingDelay = TimeSpan.FromSeconds(60),
                KeepAlivePingTimeout = TimeSpan.FromSeconds(30),
                EnableMultipleHttp2Connections = true
            }
        };
    }

    public dYdXAccountBalances GetCashBalance()
    {
        return _restClient.Get<dYdXAccountBalances>($"/cosmos/bank/v1beta1/balances/{_wallet.Address}");
    }

    public bool PlaceOrder(Order order)
    {
        var channel = GrpcChannel.ForAddress(_baseUrl, _grpcChannelOptions);
        var service = new Service.ServiceClient(channel);

        var tx = new Tx
        {
            Body = new TxBody
            {
            },
            Signatures =
            {
            }
        };
        service.BroadcastTx(new BroadcastTxRequest() { TxBytes = tx.ToByteString() });
        return true;
    }
}