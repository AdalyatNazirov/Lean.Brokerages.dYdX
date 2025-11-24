using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using Cosmos.Crypto.Secp256K1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.dYdXBrokerage.Cosmos.Tx;
using QuantConnect.dYdXBrokerage.Cosmos.Tx.Signing;
using QuantConnect.dYdXBrokerage.dYdXProtocol.Clob;
using QuantConnect.dYdXBrokerage.dYdXProtocol.Subaccounts;
using QuantConnect.Orders;
using Order = QuantConnect.Orders.Order;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXNodeClient
{
    private const ulong DefaultGasLimit = 1;

    private readonly string _restUrl;
    private readonly string _grpcUrl;
    private readonly Lazy<dYdXRestClient> _lazyRestClient;
    private readonly GrpcChannelOptions _grpcChannelOptions;

    private dYdXRestClient RestClient => _lazyRestClient.Value;

    public dYdXNodeClient(string restUrl, string grpcUrl)
    {
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

    public dYdXAccount GetAccount(string address)
    {
        var accountResponse = RestClient.Get<dYdXAccountResponse>($"/cosmos/auth/v1beta1/accounts/{address}");
        return accountResponse.Account;
    }

    public dYdXAccountBalances GetCashBalance(Wallet wallet)
    {
        return RestClient.Get<dYdXAccountBalances>($"/cosmos/bank/v1beta1/balances/{wallet.Address}");
    }

    public bool PlaceOrder(Wallet wallet, Order order)
    {
        var uri = new Uri(_grpcUrl.TrimEnd('/'));
        var channel = GrpcChannel.ForAddress(uri, _grpcChannelOptions);
        var service = new Service.ServiceClient(channel);

        // place order
        var txBody = BuildOrderBodyTxBody(wallet);
        var authInfo = BuildAuthInfo(wallet, (order.Properties as dYdXOrderProperties)?.GazLimit ?? DefaultGasLimit);

        var txRaw = new TxRaw()
        {
            BodyBytes = txBody.ToByteString(),
            AuthInfoBytes = authInfo.ToByteString()
        };

        var signdoc = new SignDoc
        {
            BodyBytes = txBody.ToByteString(),
            AuthInfoBytes = authInfo.ToByteString(),
            AccountNumber = wallet.AccountNumber,
            ChainId = wallet.ChainId
        };

        byte[] signatureBytes = wallet.Sign(signdoc.ToByteArray());

        txRaw.Signatures.Add(ByteString.CopyFrom(signatureBytes));

        var response = service.BroadcastTx(new BroadcastTxRequest()
        {
            TxBytes = txRaw.ToByteString(), Mode = BroadcastMode.Sync
        });

        return response.TxResponse.Code == 0;
    }

    // TODO: use Symbol Properties for price and size calculations
    // _symbolPropertiesDatabase.GetSymbolProperties(symbol.ID.Market, symbol, symbol.SecurityType, Currencies.USD);
    // "ETH-USD": {
    //     "clobPairId": "1",
    //     "ticker": "ETH-USD",
    //     "status": "ACTIVE",
    //     "oraclePrice": "2891.482555",
    //     "priceChange24H": "100.695859",
    //     "volume24H": "983.4040",
    //     "trades24H": 48,
    //     "nextFundingRate": "0",
    //     "initialMarginFraction": "0.02",
    //     "maintenanceMarginFraction": "0.012",
    //     "openInterest": "900.052",
    //     "atomicResolution": -9,
    //     "quantumConversionExponent": -9,
    //     "tickSize": "0.1",
    //     "stepSize": "0.001",
    //     "stepBaseQuantums": 1000000,
    //     "subticksPerTick": 100000,
    //     "marketType": "CROSS",
    //     "openInterestLowerCap": "0",
    //     "openInterestUpperCap": "0",
    //     "baseOpenInterest": "965.58",
    //     "defaultFundingRate1H": "0"
    // }

    private TxBody BuildOrderBodyTxBody(Wallet wallet)
    {
        var txBody = new TxBody();
        var futureExpiration = (uint)DateTimeOffset.UtcNow.AddDays(5).ToUnixTimeSeconds();

        var orderProto = new QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order
        {
            OrderId = new OrderId()
            {
                SubaccountId = new SubaccountId { Owner = wallet.Address, Number = wallet.SubaccountNumber },
                ClientId = Convert.ToUInt32(new Random().Next()),
                OrderFlags = 64,
                ClobPairId = 1
            },
            Side = QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order.Types.Side.Sell,
            Quantums = 10000000,
            Subticks = 40000000000,
            GoodTilBlockTime = futureExpiration,
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

    private AuthInfo BuildAuthInfo(Wallet wallet, ulong gazLimit)
    {
        // This constructs the "signer info" which tells the chain
        // "I am using this Public Key to sign, and this is my Sequence number"
        var pubKey = new PubKey
        {
            // Assuming _wallet.PublicKey is the raw compressed 33-byte public key
            Key = ByteString.FromBase64(wallet.PublicKey)
        };

        var signerInfo = new SignerInfo
        {
            PublicKey = new Any
            {
                TypeUrl = wallet.PublicKeyType,
                Value = pubKey.ToByteString()
            },
            ModeInfo = new ModeInfo
            {
                Single = new ModeInfo.Types.Single { Mode = SignMode.Direct }
            },
            Sequence = wallet.Sequence
        };

        var authInfo = new AuthInfo
        {
            SignerInfos = { signerInfo },
            Fee = new Fee
            {
                GasLimit = gazLimit, // Set appropriate gas limit
                // If fees are required, add Coin objects to Amount
                // Amount = { new Coin { Denom = "adydx", Amount = "0" } }
            }
        };

        return authInfo;
    }
}