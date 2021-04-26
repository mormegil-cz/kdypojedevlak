using KdyPojedeVlak.Web.Engine.DbStorage;

namespace KdyPojedeVlak.Web.Models
{
    public class TrainMapData
    {
        public TrainTimetable Train { get; }
        public string DataJson { get; }

        public TrainMapData(TrainTimetable train, string dataJson)
        {
            Train = train;
            DataJson = dataJson;
        }
    }
}