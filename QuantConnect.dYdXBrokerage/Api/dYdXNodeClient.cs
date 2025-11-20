using System;
using System.Net.Http;
using System.Threading;
using Google.Protobuf;
using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.dYdXBrokerage.Cosmos.Tx;
using QuantConnect.dYdXBrokerage.dYdXProtocol.Clob;
using QuantConnect.dYdXBrokerage.dYdXProtocol.Subaccounts;
using Order = QuantConnect.Orders.Order;

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

        // place order

        var txBody = BuildOrderBodyTxBody();

        var txRaw = new TxRaw()
        {
            BodyBytes = txBody.ToByteString(),
            AuthInfoBytes =
                ByteString.FromBase64(
                    "ClEKRgofL2Nvc21vcy5jcnlwdG8uc2VjcDI1NmsxLlB1YktleRIjCiED8L52P3gbW1nrw31yG+2pExSKU5QluqcguX1IIPZS7XUSBAoCCAEY53MSBBDAhD0=")
        };
        txRaw.Signatures.Add(ByteString.FromBase64(
            "O9dM9FwltZiAXq3yA5A67cQ3uf3QktcfpNGDxcEu1xBcpvGWGSt01uqYTfZ+2eg/T+xvbob4nEPFXYg/Llw+kw=="));

        var response = service.BroadcastTx(new BroadcastTxRequest()
        {
            TxBytes = txRaw.ToByteString(), Mode = BroadcastMode.Sync
        });

        return true;
    }

    private TxBody BuildOrderBodyTxBody()
    {
        var txBody = new TxBody();
        var orderProto = new QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order
        {
            OrderId = new OrderId()
            {
                SubaccountId = new SubaccountId { Owner = _wallet.Address, Number = 0 },
                ClientId = 90651682,
                OrderFlags = 64,
                ClobPairId = 1
            },
            Side = QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order.Types.Side.Sell,
            Quantums = 10000000,
            Subticks = 40000000000,
            GoodTilBlockTime = 1763671053,
            TimeInForce = QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order.Types.TimeInForce.Unspecified,
            ReduceOnly = false,
            ClientMetadata = 0,
            ConditionType = QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order.Types.ConditionType.Unspecified,
            ConditionalOrderTriggerSubticks = 0
        };
        var msgPlaceOrder = new MsgPlaceOrder { Order = orderProto };
        var msg = new Any { TypeUrl = "/dydxprotocol.clob.MsgPlaceOrder", Value = msgPlaceOrder.ToByteString() };
        txBody.Messages.Add(msg);
        return txBody;
    }
}