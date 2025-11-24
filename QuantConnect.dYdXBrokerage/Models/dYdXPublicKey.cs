using Newtonsoft.Json;

namespace QuantConnect.Brokerages.dYdX.Models;

public class dYdXPublicKey
{
    [JsonProperty("@type")] public string Type { get; set; }
    public string Key { get; set; }
}