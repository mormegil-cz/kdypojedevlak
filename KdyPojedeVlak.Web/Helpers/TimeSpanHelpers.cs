using System;
using System.Diagnostics.Contracts;

namespace KdyPojedeVlak.Web.Helpers;

public static class TimeSpanHelpers
{
    [Pure]
    public static TimeSpan GetTimeOfDay(this TimeSpan timeSpan) => timeSpan.Days == 0 ? timeSpan : new TimeSpan(0, timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);

    [Pure]
    public static string MinutesToString(this TimeSpan timeSpan) => $"{(int) timeSpan.TotalMinutes}:{timeSpan.Seconds:D2}";
}