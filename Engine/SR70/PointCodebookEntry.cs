namespace KdyPojedeVlak.Engine.SR70
{
    public class PointCodebookEntry
    {
        public string ID { get; set; }
        public string LongName { get; set; }
        public string ShortName { get; set; }
        public PointType Type { get; set; }
        public float? Latitude { get; set; }
        public float? Longitude { get; set; }
        public string WikidataItem { get; set; }

        public string FullIdentifier => "CZ:" + ID.Substring(0, ID.Length - 1);
    }

    public enum PointType
    {
        Unknown,
        Stop,
        Station,
        InnerBoundary,
        StateBoundary,
        Crossing,
        Siding,
        Point
    }
}
