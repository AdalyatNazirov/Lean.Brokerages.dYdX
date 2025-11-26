using System;
using System.Net.Http;
using Cosmos.Crypto.Secp256K1;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using QuantConnect.Brokerages.dYdX.Domain;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.dYdXBrokerage.Cosmos.Base.Tendermint.V1Beta1;
using QuantConnect.dYdXBrokerage.Cosmos.Tx;
using QuantConnect.dYdXBrokerage.Cosmos.Tx.Signing;
using QuantConnect.dYdXBrokerage.dYdXProtocol.Clob;
using Order = QuantConnect.dYdXBrokerage.dYdXProtocol.Clob.Order;
using TxService = QuantConnect.dYdXBrokerage.Cosmos.Tx.Service;
using TendermintService = QuantConnect.dYdXBrokerage.Cosmos.Base.Tendermint.V1Beta1.Service;

namespace QuantConnect.Brokerages.dYdX.Api;

public class dYdXNodeClient
{
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

    public uint GetLatestBlockHeight()
    {
        var uri = new Uri(_grpcUrl.TrimEnd('/'));
        var channel = GrpcChannel.ForAddress(uri, _grpcChannelOptions);
        var service = new TendermintService.ServiceClient(channel);

        return checked((uint)service.GetLatestBlock(new GetLatestBlockRequest()).Block.Header.Height);
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

    public bool PlaceOrder(Wallet wallet, Order order, ulong gasLimit)
    {
        var uri = new Uri(_grpcUrl.TrimEnd('/'));
        var channel = GrpcChannel.ForAddress(uri, _grpcChannelOptions);
        var service = new TxService.ServiceClient(channel);

        // place order
        var txBody = BuildOrderBodyTxBody(wallet, order);
        var authInfo = BuildAuthInfo(wallet, gasLimit);

        var txRaw = new TxRaw
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

        var response = service.BroadcastTx(new BroadcastTxRequest
        {
            TxBytes = txRaw.ToByteString(), Mode = BroadcastMode.Sync
        });

        return response.TxResponse.Code == 0;
    }

    private TxBody BuildOrderBodyTxBody(Wallet wallet, Order orderProto)
    {
        var txBody = new TxBody();
        var msgPlaceOrder = new MsgPlaceOrder { Order = orderProto };
        var msg = new Any { TypeUrl = "/dydxprotocol.clob.MsgPlaceOrder", Value = msgPlaceOrder.ToByteString() };
        txBody.Messages.Add(msg);
        return txBody;
    }

    private AuthInfo BuildAuthInfo(Wallet wallet, ulong gasLimit)
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
                GasLimit = gasLimit, // Set appropriate gas limit
                // If fees are required, add Coin objects to Amount
                // Amount = { new Coin { Denom = "adydx", Amount = "0" } }
            }
        };

        return authInfo;
    }
}