using System;

namespace KdyPojedeVlak.Web.Helpers;

public static class TimeSpanHelpers
{
    public static TimeSpan GetTimeOfDay(this TimeSpan timeSpan) => timeSpan.Days == 0 ? timeSpan : new TimeSpan(0, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
}
