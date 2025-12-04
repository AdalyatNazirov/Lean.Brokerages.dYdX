using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.Brokerages.dYdX.Models.WebSockets;
using QuantConnect.Logging;
using QuantConnect.Orders;
using QuantConnect.Orders.Fees;

namespace QuantConnect.Brokerages.dYdX;

public partial class dYdXBrokerage
{
    /// <summary>
    /// Wss message handler
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected override void OnMessage(object sender, WebSocketMessage e)
    {
        _messageHandler.HandleNewMessage(e);
    }

    /// <summary>
    /// Processes WSS messages from the private user data streams
    /// </summary>
    /// <param name="webSocketMessage">The message to process</param>
    private void OnUserMessage(WebSocketMessage webSocketMessage)
    {
        var e = (WebSocketClientWrapper.TextMessage)webSocketMessage.Data;
        try
        {
            if (Log.DebuggingEnabled)
            {
                Log.Debug($"{nameof(dYdXBrokerage)}.{nameof(OnUserMessage)}(): {e.Message}");
            }

            var jObj = JObject.Parse(e.Message);
            var topic = jObj.Value<string>("type");
            if (topic.Equals("connected", StringComparison.InvariantCultureIgnoreCase))
            {
                OnConnected(jObj.ToObject<ConnectedResponseSchema>());
                return;
            }

            // TODO: send "pong" response

            var channel = jObj.Value<string>("channel");
            switch (channel)
            {
                case "v4_markets":
                    OnMarketUpdate(jObj);
                    break;
                case "v4_subaccounts":
                    OnSubaccountUpdate(jObj);
                    break;
            }
        }
        catch (Exception exception)
        {
            OnMessage(new BrokerageMessageEvent(BrokerageMessageType.Error, -1,
                $"Parsing wss message failed. Data: {e.Message} Exception: {exception}"));
            throw;
        }
    }

    private void OnConnected(ConnectedResponseSchema _)
    {
        _connectionConfirmedEvent.Set();
    }

    /// <summary>
    /// Handles market updates from v4_markets channel.
    /// Supports both 'subscribed' (snapshot) and 'channel_batch_data' (updates) types.
    /// </summary>
    private void OnMarketUpdate(JObject jObj)
    {
        var contents = jObj["contents"];
        if (contents == null) return;

        // 'contents' can be an Object (in 'subscribed') or an Array (in 'channel_batch_data')
        switch (jObj.Value<string>("type"))
        {
            case "subscribed":
                var initialData = jObj.ToObject<DataResponseSchema<ExchangeInfo>>();
                if (initialData?.Contents != null)
                {
                    RefreshMarkets(initialData.Contents);
                }

                break;

            case "channel_batch_data":
                var oraclePrices = jObj.ToObject<BatchDataResponseSchema<OraclePricesMarketUpdate>>();
                if (oraclePrices?.Contents != null)
                {
                    UpdateOraclePrice(oraclePrices.Contents);
                }

                break;
        }
    }

    private void OnSubaccountUpdate(JObject jObj)
    {
        var contents = jObj["contents"];
        if (contents == null) return;

        // 'contents' can be an Object (in 'subscribed') or an Array (in 'channel_batch_data')
        switch (jObj.Value<string>("type"))
        {
            case "subscribed":
                // do nothing as we fetched subaccount initial data
                break;

            case "channel_data":
                var subaccountUpdate = jObj.ToObject<DataResponseSchema<SubaccountsUpdateMessage>>();
                if (subaccountUpdate.Contents?.BlockHeight > 0)
                {
                    UpdateBlockHeight(subaccountUpdate.Contents.BlockHeight.Value);
                }

                if (subaccountUpdate.Contents?.Orders?.Any() == true)
                {
                    HandleOrders(subaccountUpdate.Contents.Orders);
                }

                if (subaccountUpdate.Contents?.Fills?.Any() == true)
                {
                    HandleFills(subaccountUpdate.Contents.Fills);
                }

                break;
        }
    }

    private void HandleOrders(List<OrderSubaccountMessage> contentsOrders)
    {
        foreach (var dydxOrder in contentsOrders)
        {
            if (_pendingOrders.TryRemove(dydxOrder.ClientId, out var tuple))
            {
                var (resetEvent, leanOrder) = tuple;
                leanOrder.BrokerId.Add(dydxOrder.Id);
                _orderBrokerIdToClientIdMap.TryAdd(dydxOrder.Id, dydxOrder.ClientId);
                OnOrderEvent(new OrderEvent(leanOrder, DateTime.UtcNow, OrderFee.Zero, "dYdX Order Event")
                {
                    Status = OrderStatus.Submitted
                });
                resetEvent.Set();
            }
        }
    }

    private void HandleFills(List<FillSubaccountMessage> fills)
    {
        string fillsStr = string.Join(Environment.NewLine,
            fills.Select(f => JsonConvert.SerializeObject(f, Formatting.Indented)));
        Log.Debug($"Received {fills.Count} fills from subaccount");
        Log.Debug(fillsStr);
        // throw new NotImplementedException();
    }

    private void UpdateBlockHeight(uint blockHeight)
    {
        _market.UpdateBlockHeigh(blockHeight);
    }

    private void RefreshMarkets(ExchangeInfo exchangeInfo)
    {
        // Implementation to refresh markets using exchangeInfo.Markets
        if (exchangeInfo == null || !exchangeInfo.Symbols.Any())
        {
            return;
        }

        _market.RefreshMarkets(exchangeInfo.Symbols.Values);
    }

    private void UpdateOraclePrice(IEnumerable<OraclePricesMarketUpdate> updates)
    {
        foreach (var update in updates)
        {
            if (update.OraclePrices == null || !update.OraclePrices.Any())
            {
                continue;
            }

            foreach (var priceKvp in update.OraclePrices)
            {
                _market.UpdateOraclePrice(priceKvp.Key, priceKvp.Value.OraclePrice);
            }
        }
    }
}