using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using Newtonsoft.Json;
using QuantConnect.Brokerages.dYdX.Models;
using QuantConnect.Brokerages.Template.Api;
using QuantConnect.Logging;
using RestSharp;

namespace QuantConnect.Brokerages.dYdX;

public class dYdXNodeClient(string baseUrl)
{
    private readonly Lazy<dYdXRestClient> _lazyRestClient = new(() => new dYdXRestClient(baseUrl));
    private dYdXRestClient _restClient => _lazyRestClient.Value;

    public dYdXAccountBalances GetCashBalance(string address)
    {
        return _restClient.Get<dYdXAccountBalances>($"/cosmos/bank/v1beta1/balances/{address}");
    }
}