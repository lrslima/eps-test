namespace EPS.SignalR;

public class DiscountOptions
{
    public int MaxCodesPerRequest { get; set; } = 2000;
    public byte MinCodeLength { get; set; } = 7;
    public byte MaxCodeLength { get; set; } = 8;
    
    public string StoragePath { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), 
        "discountCodes.json");
}