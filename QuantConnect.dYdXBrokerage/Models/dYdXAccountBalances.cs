using System.Collections.Generic;
using Newtonsoft.Json;

namespace QuantConnect.Brokerages.dYdX.Models;

public class dYdXAccountBalances
{
    /// <summary>
    /// https://docs.dydx.xyz/node-client/public#get-account-balances
    /// </summary>
    public IEnumerable<dYdXDenomBalance> Balances { get; set; } = [];
}