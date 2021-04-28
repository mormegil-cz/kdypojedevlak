using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Uic;
using RoutingPoint = KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint;

namespace KdyPojedeVlak.Web.Models
{
    public class TrainPlan
    {
        public TrainTimetable Train { get; }
        public List<TrainTimetableVariant> TrainVariants { get; }
        public List<RoutingPoint> Points { get; }
        public List<List<Passage>> VariantRoutingPoints { get; }
        public List<bool> MajorPointFlags { get; }
        public string VagonWebCompanyID { get; }
        public List<CompanyCodebookEntry> CompanyCodebookEntries { get; }
        public bool IsFiltered { get; }
        public bool CanFilter { get; }

        public TrainPlan(TrainTimetable train, List<TrainTimetableVariant> trainVariants, List<RoutingPoint> points, List<List<Passage>> variantRoutingPoints, List<bool> majorPointFlags, List<CompanyCodebookEntry> companyCodebookEntries, string vagonWebCompanyId, bool isFiltered, bool canFilter)
        {
            Train = train;
            TrainVariants = trainVariants;
            Points = points;
            VariantRoutingPoints = variantRoutingPoints;
            MajorPointFlags = majorPointFlags;
            CompanyCodebookEntries = companyCodebookEntries;
            VagonWebCompanyID = vagonWebCompanyId;
            IsFiltered = isFiltered;
            CanFilter = canFilter;
        }
    }
}