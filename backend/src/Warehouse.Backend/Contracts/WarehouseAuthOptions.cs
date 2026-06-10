namespace Warehouse.Backend.Contracts;

public sealed class WarehouseAuthOptions
{
    public const string SectionName = "WarehouseAuth";

    public string SigningKey { get; set; } = "vrcoresched-demo-signing-key";
    public string Issuer { get; set; } = "vR-CoreSched";
    public string Audience { get; set; } = "vR-CoreSched-Web";
}
