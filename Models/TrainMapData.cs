using KdyPojedeVlak.Engine.DbStorage;

namespace KdyPojedeVlak.Models
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