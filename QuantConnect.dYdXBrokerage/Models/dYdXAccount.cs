using Newtonsoft.Json;

namespace QuantConnect.Brokerages.dYdX.Models;

public class dYdXAccount
{
    [JsonProperty("account_number")] public uint AccountNumber { get; set; }
    [JsonProperty("sequence")] public uint Sequence { get; set; }
    [JsonProperty("pub_key")] public dYdXPublicKey PublicKey { get; set; }
}