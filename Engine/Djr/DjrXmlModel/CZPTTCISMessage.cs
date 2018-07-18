using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace KdyPojedeVlak.Engine.Djr.DjrXmlModel
{
    public class CZPTTCISMessage
    {
        public Identifiers Identifiers { get; set; }
        public DateTime CZPTTCreation { get; set; }
        public CZPTTInformation CZPTTInformation { get; set; }
    }

    public class Identifiers
    {
        [XmlElement]
        public List<PlannedTransportIdentifiers> PlannedTransportIdentifiers { get; set; }
    }

    public class PlannedTransportIdentifiers
    {
        public string ObjectType { get; set; }
        public string Company { get; set; }
        public string Core { get; set; }
        public string Variant { get; set; }
        public string TimetableYear { get; set; }
    }

    public class CZPTTInformation
    {
        public PlannedCalendar PlannedCalendar { get; set; }
        [XmlElement]
        public List<CZPTTLocation> CZPTTLocation { get; set; }
    }

    public class PlannedCalendar
    {
        public string BitmapDays { get; set; }
        public ValidityPeriod ValidityPeriod { get; set; }
    }

    public class ValidityPeriod
    {
        public string StartDateTime { get; set; }
        public string EndDateTime { get; set; }
    }

    public class CZPTTLocation
    {
        [XmlAttribute]
        public string JourneyLocationTypeCode { get; set; }

        public string CountryCodeISO { get; set; }
        public string LocationPrimaryCode { get; set; }
        public LocationSubsidiaryIdentification LocationSubsidiaryIdentification { get; set; }
        public TimingAtLocation TimingAtLocation { get; set; }
        public string PrimaryLocationName { get; set; }
        public string ResponsibleRU { get; set; }
        public string ResponsibleIM { get; set; }
        public string TrainType { get; set; }
        public string TrafficType { get; set; }
        public string CommercialTrafficType { get; set; }
        public string OperationalTrainNumber { get; set; }
        [XmlElement]
        public List<TrainActivity> TrainActivity { get; set; }
    }

    public class LocationSubsidiaryIdentification
    {
        public LocationSubsidiaryCode LocationSubsidiaryCode { get; set; }
        public string AllocationCompany { get; set; }
    }

    public class LocationSubsidiaryCode
    {
        [XmlAttribute]
        public string LocationSubsidiaryTypeCode { get; set; }

        [XmlText]
        public string Code { get; set; }
    }

    public class TimingAtLocation
    {
        [XmlElement]
        public List<Timing> Timing { get; set; }
    }

    public class Timing
    {
        [XmlAttribute]
        public string TimingQualifierCode { get; set; }

        public DateTimeOffset Time { get; set; }
        public int Offset { get; set; }
    }

    public class TrainActivity
    {
        [XmlElement]
        public List<string> TrainActivityType { get; set; }
    }
}