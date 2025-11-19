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
    private readonly string _restUrl;
    private readonly string _grpcUrl;
    private readonly Lazy<dYdXRestClient> _lazyRestClient;
    private readonly GrpcChannelOptions _grpcChannelOptions;

    private dYdXRestClient RestClient => _lazyRestClient.Value;

    public dYdXNodeClient(Wallet wallet, string restUrl, string grpcUrl)
    {
        _wallet = wallet;
        _restUrl = restUrl;
        _grpcUrl = grpcUrl;
        _lazyRestClient = new(() => new dYdXRestClient(_restUrl.TrimEnd('/')));
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
        return RestClient.Get<dYdXAccountBalances>($"/cosmos/bank/v1beta1/balances/{_wallet.Address}");
    }

    public bool PlaceOrder(Order order)
    {
        var uri = new Uri(_grpcUrl.TrimEnd('/'));
        var channel = GrpcChannel.ForAddress(uri, _grpcChannelOptions);
        var service = new Service.ServiceClient(channel);

        var txBytes = ByteString.FromBase64(
            "CrMBCnMKIC9keWR4cHJvdG9jb2wuY2xvYi5Nc2dQbGFjZU9yZGVyEk8KTQo4Ci0KK2R5ZHgxNHp6dWVhemVoMGhqNjdjZ2hoZjlqeXBzbGNmOXNoMm41azZhcnQVRfCukxhAIAEQAhiAreIEIICgvoGVATW1NR5pEg5DbGllbnQgRXhhbXBsZfp/KwolL2R5ZHhwcm90b2NvbC5hY2NvdW50cGx1cy5UeEV4dGVuc2lvbhICCgASWQpRCkYKHy9jb3Ntb3MuY3J5cHRvLnNlY3AyNTZrMS5QdWJLZXkSIwohA/C+dj94G1tZ68N9chvtqRMUilOUJbqnILl9SCD2Uu11EgQKAggBGNlzEgQQwIQ9GkDKXoLiTm9zKOHtDnXKNY/Y6mi8jxfaopo2PhyMK7xwXg4DnG9hxMIqFQWXOFfJ5m/qzemTnoLOcjFq9zVfknXd");

        var response = service.BroadcastTx(new BroadcastTxRequest()
        {
            TxBytes = txBytes, Mode = BroadcastMode.Sync
        });
        return true;
    }
}