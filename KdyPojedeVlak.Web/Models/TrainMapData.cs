using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Uic;

namespace KdyPojedeVlak.Web.Models;

public class TrainMapData(TrainTimetable train, string dataJson, List<CompanyCodebookEntry> companyCodebookEntries, string? vagonWebCompanyId)
{
    public TrainTimetable Train { get; } = train;
    public string DataJson { get; } = dataJson;
    public List<CompanyCodebookEntry> CompanyCodebookEntries { get; } = companyCodebookEntries;
    public string? VagonWebCompanyId { get; } = vagonWebCompanyId;
}