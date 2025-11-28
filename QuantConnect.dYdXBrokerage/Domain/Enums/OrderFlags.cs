namespace QuantConnect.Brokerages.dYdX.Domain.Enums;

public enum OrderFlags : uint
{
    ShortTerm = 0u,
    Conditional = 32u,
    LongTerm = 64u
}