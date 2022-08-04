using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Uic;

namespace KdyPojedeVlak.Web.Models
{
    public class TrainMapData
    {
        public TrainTimetable Train { get; }
        public string DataJson { get; }
        public List<CompanyCodebookEntry> CompanyCodebookEntries { get; }
        public string VagonWebCompanyID { get; }

        public TrainMapData(TrainTimetable train, string dataJson, List<CompanyCodebookEntry> companyCodebookEntries, string vagonWebCompanyId)
        {
            Train = train;
            DataJson = dataJson;
            CompanyCodebookEntries = companyCodebookEntries;
            VagonWebCompanyID = vagonWebCompanyId;
        }
    }
}