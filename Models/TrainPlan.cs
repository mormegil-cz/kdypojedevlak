using System.Collections.Generic;
using KdyPojedeVlak.Engine.DbStorage;
using KdyPojedeVlak.Engine.Uic;
using RoutingPoint = KdyPojedeVlak.Engine.DbStorage.RoutingPoint;

namespace KdyPojedeVlak.Models
{
    public class TrainPlan
    {
        public TrainTimetable Train { get; }
        public List<RoutingPoint> Points { get; }
        public List<List<Passage>> VariantRoutingPoints { get; }
        public List<bool> MajorPointFlags { get; }
        public string VagonWebCompanyID { get; }
        public List<CompanyCodebookEntry> CompanyCodebookEntries { get; }

        public TrainPlan(TrainTimetable train, List<RoutingPoint> points, List<List<Passage>> variantRoutingPoints, List<bool> majorPointFlags, List<CompanyCodebookEntry> companyCodebookEntries, string vagonWebCompanyId)
        {
            Train = train;
            Points = points;
            VariantRoutingPoints = variantRoutingPoints;
            MajorPointFlags = majorPointFlags;
            CompanyCodebookEntries = companyCodebookEntries;
            VagonWebCompanyID = vagonWebCompanyId;
        }
    }
}