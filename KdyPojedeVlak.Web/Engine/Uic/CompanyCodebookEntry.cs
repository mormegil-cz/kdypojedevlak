namespace KdyPojedeVlak.Web.Engine.Uic;

public class CompanyCodebookEntry
{
    public required string ID { get; init; }
    public required string ShortName { get; init; }
    public required string LongName { get; init; }
    public string? Country { get; init; }
    public string? Web { get; init; }
}