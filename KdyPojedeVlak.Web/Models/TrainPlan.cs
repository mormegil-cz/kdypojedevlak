using System.Collections.Generic;
using KdyPojedeVlak.Web.Engine.DbStorage;
using KdyPojedeVlak.Web.Engine.Uic;
using RoutingPoint = KdyPojedeVlak.Web.Engine.DbStorage.RoutingPoint;

namespace KdyPojedeVlak.Web.Models;

public class TrainPlan(
    TrainTimetable train,
    List<TrainTimetableVariant> trainVariants,
    List<RoutingPoint> points,
    List<List<Passage?>> variantRoutingPoints,
    List<bool> majorPointFlags,
    List<CompanyCodebookEntry> companyCodebookEntries,
    string? vagonWebCompanyId,
    bool isFiltered,
    bool canFilter)
{
    public TrainTimetable Train { get; } = train;
    public List<TrainTimetableVariant> TrainVariants { get; } = trainVariants;
    public List<RoutingPoint> Points { get; } = points;
    public List<List<Passage?>> VariantRoutingPoints { get; } = variantRoutingPoints;
    public List<bool> MajorPointFlags { get; } = majorPointFlags;
    public string? VagonWebCompanyId { get; } = vagonWebCompanyId;
    public List<CompanyCodebookEntry> CompanyCodebookEntries { get; } = companyCodebookEntries;
    public bool IsFiltered { get; } = isFiltered;
    public bool CanFilter { get; } = canFilter;
}