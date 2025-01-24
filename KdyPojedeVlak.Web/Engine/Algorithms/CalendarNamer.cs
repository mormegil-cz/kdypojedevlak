#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace KdyPojedeVlak.Web.Engine.Algorithms;

public static class CalendarNamer
{
    private enum DayClass
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday,

        All,
        Workday,
        Holiday,
        NonSaturdayHoliday,
        SaturdayHoliday,
        SaturdayNonHoliday
    }

    private static readonly string[] monthToRoman = ["", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII"];

    private static readonly HashSet<DateTime> holidays =
    [
        new(2017, 12, 24),
        new(2017, 12, 25),
        new(2017, 12, 26),

        new(2018, 01, 01),
        new(2018, 03, 30),
        new(2018, 04, 02),
        new(2018, 05, 01),
        new(2018, 05, 08),
        new(2018, 07, 05),
        new(2018, 07, 06),
        new(2018, 09, 28),
        new(2018, 10, 28),
        new(2018, 11, 17),
        new(2018, 12, 24),
        new(2018, 12, 25),
        new(2018, 12, 26),

        new(2019, 01, 01),
        new(2019, 04, 19),
        new(2019, 04, 22),
        new(2019, 05, 01),
        new(2019, 05, 08),
        new(2019, 07, 05),
        new(2019, 07, 06),
        new(2019, 09, 28),
        new(2019, 10, 28),
        new(2019, 11, 17),
        new(2019, 12, 24),
        new(2019, 12, 25),
        new(2019, 12, 26),

        new(2020, 01, 01),
        new(2020, 04, 10),
        new(2020, 04, 13),
        new(2020, 05, 01),
        new(2020, 05, 08),
        new(2020, 07, 05),
        new(2020, 07, 06),
        new(2020, 09, 28),
        new(2020, 10, 28),
        new(2020, 11, 17),
        new(2020, 12, 24),
        new(2020, 12, 25),
        new(2020, 12, 26),

        new(2021, 01, 01),
        new(2021, 04, 02),
        new(2021, 04, 05),
        new(2021, 05, 01),
        new(2021, 05, 08),
        new(2021, 07, 05),
        new(2021, 07, 06),
        new(2021, 09, 28),
        new(2021, 10, 28),
        new(2021, 11, 17),
        new(2021, 12, 24),
        new(2021, 12, 25),
        new(2021, 12, 26),

        new(2022, 01, 01),
        new(2022, 04, 15),
        new(2022, 04, 18),
        new(2022, 05, 01),
        new(2022, 05, 08),
        new(2022, 07, 05),
        new(2022, 07, 06),
        new(2022, 09, 28),
        new(2022, 10, 28),
        new(2022, 11, 17),
        new(2022, 12, 24),
        new(2022, 12, 25),
        new(2022, 12, 26),

        new(2023, 01, 01),
        new(2023, 04, 07),
        new(2023, 04, 10),
        new(2023, 05, 01),
        new(2023, 05, 08),
        new(2023, 07, 05),
        new(2023, 07, 06),
        new(2023, 09, 28),
        new(2023, 10, 28),
        new(2023, 11, 17),
        new(2023, 12, 24),
        new(2023, 12, 25),
        new(2023, 12, 26),

        new(2024, 01, 01),
        new(2024, 03, 29),
        new(2024, 04, 01),
        new(2024, 05, 01),
        new(2024, 05, 08),
        new(2024, 07, 05),
        new(2024, 07, 06),
        new(2024, 09, 28),
        new(2024, 10, 28),
        new(2024, 11, 17),
        new(2024, 12, 24),
        new(2024, 12, 25),
        new(2024, 12, 26),

        new(2025, 01, 01),
        new(2025, 04, 18),
        new(2025, 04, 21),
        new(2025, 05, 01),
        new(2025, 05, 08),
        new(2025, 07, 05),
        new(2025, 07, 06),
        new(2025, 09, 28),
        new(2025, 10, 28),
        new(2025, 11, 17),
        new(2025, 12, 24),
        new(2025, 12, 25),
        new(2025, 12, 26),

        new(2026, 01, 01),
        new(2026, 04, 03),
        new(2026, 04, 06),
        new(2026, 05, 01),
        new(2026, 05, 08),
        new(2026, 07, 05),
        new(2026, 07, 06),
        new(2026, 09, 28),
        new(2026, 10, 28),
        new(2026, 11, 17),
        new(2026, 12, 24),
        new(2026, 12, 25),
        new(2026, 12, 26),
    ];

    private static readonly Dictionary<DayClass, Predicate<DateTime>> classifiers = new(13)
    {
        { DayClass.Monday, MakeDayClassifier(DayOfWeek.Monday) },
        { DayClass.Tuesday, MakeDayClassifier(DayOfWeek.Tuesday) },
        { DayClass.Wednesday, MakeDayClassifier(DayOfWeek.Wednesday) },
        { DayClass.Thursday, MakeDayClassifier(DayOfWeek.Thursday) },
        { DayClass.Friday, MakeDayClassifier(DayOfWeek.Friday) },
        { DayClass.Saturday, MakeDayClassifier(DayOfWeek.Saturday) },
        { DayClass.Sunday, MakeDayClassifier(DayOfWeek.Sunday) },

        { DayClass.All, _ => true },
        { DayClass.Holiday, dt => dt.DayOfWeek == DayOfWeek.Sunday || holidays.Contains(dt) },
        { DayClass.Workday, dt => dt.DayOfWeek >= DayOfWeek.Monday && dt.DayOfWeek <= DayOfWeek.Friday && !holidays.Contains(dt) },
        { DayClass.SaturdayHoliday, dt => dt.DayOfWeek == DayOfWeek.Saturday && holidays.Contains(dt) },
        { DayClass.SaturdayNonHoliday, dt => dt.DayOfWeek == DayOfWeek.Saturday && !holidays.Contains(dt) },
        { DayClass.NonSaturdayHoliday, dt => dt.DayOfWeek != DayOfWeek.Saturday && holidays.Contains(dt) },
    };

    private class ClassPresence
    {
        public readonly SortedSet<DateTime> YesDates = [];
        public readonly SortedSet<DateTime> NoDates = [];
    }

    private record NamingResult(string Name, SortedSet<DateTime> ExceptionalGo, SortedSet<DateTime> ExceptionalNoGo);

    private static readonly List<Func<Dictionary<DayClass, ClassPresence>, NamingResult>> namingStrategies =
    [
        EverydayStrategy,
        WorkdaysStrategy,
        WorkdaysAndSaturdaysStrategy,
        HolidaysStrategy,
        HolidaysAndSaturdaysStrategy,
        DayOfWeekStrategy
    ];

    private static NamingResult DayOfWeekStrategy(Dictionary<DayClass, ClassPresence> classPresences)
    {
        var resultName = new StringBuilder(7);
        var exceptionalGo = new SortedSet<DateTime>();
        var exceptionalNoGo = new SortedSet<DateTime>();
        for (DayClass cls = DayClass.Monday; cls <= DayClass.Sunday; ++cls)
        {
            var presence = classPresences[cls];
            if (presence.YesDates.Count >= presence.NoDates.Count)
            {
                // U+2460 = ①
                resultName.Append((char) (((int) cls) - ((int) DayClass.Monday) + 0x2460));
                exceptionalNoGo.AddAll(presence.NoDates);
            }
            else
            {
                exceptionalGo.AddAll(presence.YesDates);
            }
        }

        return new NamingResult(
            resultName.ToString(),
            exceptionalGo,
            exceptionalNoGo
        );
    }

    private static NamingResult EverydayStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new(
        "denně",
        Sets<DateTime>.EmptySortedSet,
        classPresences[DayClass.All].NoDates
    );

    private static NamingResult WorkdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new(
        "⚒\uFE0E",
        new SortedSet<DateTime>(classPresences[DayClass.Holiday].YesDates.Concat(classPresences[DayClass.Saturday].YesDates)),
        classPresences[DayClass.Workday].NoDates
    );

    private static NamingResult WorkdaysAndSaturdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new(
        "⚒\uFE0E⑥",
        new SortedSet<DateTime>(classPresences[DayClass.Sunday].YesDates.Concat(classPresences[DayClass.NonSaturdayHoliday].YesDates)),
        new SortedSet<DateTime>(classPresences[DayClass.Workday].NoDates.Concat(classPresences[DayClass.Saturday].NoDates))
    );

    private static NamingResult HolidaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new(
        "✝\uFE0E",
        new SortedSet<DateTime>(classPresences[DayClass.Workday].YesDates.Concat(classPresences[DayClass.SaturdayNonHoliday].YesDates)),
        new SortedSet<DateTime>(classPresences[DayClass.Holiday].NoDates)
    );

    private static NamingResult HolidaysAndSaturdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new(
        "✝\uFE0E⑥",
        new SortedSet<DateTime>(classPresences[DayClass.Workday].YesDates),
        new SortedSet<DateTime>(classPresences[DayClass.Holiday].NoDates.Concat(classPresences[DayClass.SaturdayNonHoliday].NoDates))
    );

    private static Predicate<DateTime> MakeDayClassifier(DayOfWeek dayOfWeek)
    {
        return dt => dt.DayOfWeek == dayOfWeek;
    }

    public static string DetectName(bool[] calendarBitmap, DateTime validFrom, DateTime validTo)
    {
        // TODO: realStartDate, realEndDate

        var activeCount = calendarBitmap.Count(value => value);

        if (activeCount == 0)
        {
            return "jede pp";
        }

        if (activeCount <= 5)
        {
            return AppendListOfDays(new StringBuilder("jede "),
                new SortedSet<DateTime>(
                    calendarBitmap
                        .Select((active, dayIndex) => (Active: active, Date: validFrom.AddDays(dayIndex)))
                        .Where(r => r.Active)
                        .Select(r => r.Date)
                )
            ).ToString();
        }

        if (activeCount == calendarBitmap.Length)
        {
            return "jede denně";
        }

        var classes = new Dictionary<DayClass, ClassPresence>(Enum.GetValues(typeof(DayClass)).Length);

        var dayCount = (int) ((validTo - validFrom).TotalDays) + 1;
        DateTime? firstGoDate = null;
        DateTime? lastGoDate = null;
        for (int dayIndex = 0; dayIndex < dayCount; ++dayIndex)
        {
            var day = validFrom.AddDays(dayIndex);
            var bitmapValue = calendarBitmap[dayIndex];

            if (bitmapValue)
            {
                if (firstGoDate == null) firstGoDate = day;
                lastGoDate = day;
            }

            foreach (var classifier in classifiers)
            {
                if (!classes.TryGetValue(classifier.Key, out var cls))
                {
                    cls = new ClassPresence();
                    classes.Add(classifier.Key, cls);
                }

                if (classifier.Value(day))
                {
                    if (bitmapValue) cls.YesDates.Add(day);
                    else cls.NoDates.Add(day);
                }
            }
        }

        Debug.Assert(firstGoDate != null);
        Debug.Assert(lastGoDate != null);

        NamingResult? bestNaming = null;
        var bestScore = Int32.MaxValue;
        var bestSuspendedStart = false;
        foreach (var strategy in namingStrategies)
        {
            var strategyResult = strategy(classes);

            bool hasSuspendedStart = false;
            while (strategyResult.ExceptionalNoGo.Count > 0)
            {
                var firstNoGo = strategyResult.ExceptionalNoGo.First();
                if (firstNoGo < firstGoDate)
                {
                    hasSuspendedStart = true;
                    strategyResult.ExceptionalNoGo.Remove(firstNoGo);
                }
                else
                {
                    break;
                }
            }

            var score = strategyResult.ExceptionalGo.Count + strategyResult.ExceptionalNoGo.Count;
            if (score < bestScore)
            {
                bestScore = score;
                bestNaming = strategyResult;
                bestSuspendedStart = hasSuspendedStart;
            }
        }

        Debug.Assert(bestNaming != null);

        var result = new StringBuilder();
        if (bestNaming.Name.Length > 0 || bestNaming.ExceptionalGo.Count > 0)
        {
            result.Append("jede ");
        }

        if (firstGoDate.GetValueOrDefault().Equals(lastGoDate.GetValueOrDefault()))
        {
            result.Append(firstGoDate.GetValueOrDefault().ToShortDateString());
        }
        else
        {
            result.Append(bestNaming.Name);
            if (bestSuspendedStart)
            {
                result.AppendFormat(" od {0:d} do {1:d}", firstGoDate, lastGoDate);
            }
        }

        if (bestNaming.ExceptionalGo.Count > 0)
        {
            if (bestNaming.Name.Length > 0)
            {
                result.Append(" a ");
            }

            AppendListOfDays(result, bestNaming.ExceptionalGo);
        }

        if (bestNaming.ExceptionalNoGo.Count > 0)
        {
            result.Append(", nejede ");
            AppendListOfDays(result, bestNaming.ExceptionalNoGo);
        }

        if (bestScore > 10)
        {
            DebugLog.LogProblem("Suspicious calendar: " + result);
        }

        return result.ToString();
    }

    private static StringBuilder AppendListOfDays(StringBuilder result, SortedSet<DateTime> dates)
    {
        var currStart = DateTime.MinValue;
        var prevDate = DateTime.MinValue;
        var currMonth = -1;
        var first = true;

        // TODO: Day ranges
        foreach (var date in dates)
        {
            if (prevDate == date.AddDays(-1))
            {
                // continuing the previous run
                prevDate = date;
            }
            else
            {
                AppendDateRange(result, currStart, prevDate, currMonth, first);

                currMonth = currStart <= DateTime.MinValue ? -1 : prevDate.Month;
                first = currStart <= DateTime.MinValue;
                currStart = date;
                prevDate = date;
            }
        }

        AppendDateRange(result, currStart, prevDate, currMonth, first);

        result.Append('\u00A0');
        result.Append(monthToRoman[prevDate.Month]);
        result.Append('.');

        return result;
    }

    private static void AppendDateRange(StringBuilder result, DateTime start, DateTime end, int currMonth, bool first)
    {
        if (start <= DateTime.MinValue) return;

        if (!first)
        {
            if (currMonth > 0 && start.Month != currMonth)
            {
                result.Append('\u00A0');
                result.Append(monthToRoman[currMonth]);
                result.Append('.');
            }

            result.Append(", ");
        }

        result.Append(start.Day);
        result.Append('.');
        if (start.Month != end.Month)
        {
            result.Append('\u00A0');
            result.Append(monthToRoman[start.Month]);
            result.Append('.');
        }

        if (end != start)
        {
            if ((end - start).TotalDays >= 2)
            {
                if (start.Month == end.Month)
                {
                    result.Append('–');
                }
                else
                {
                    result.Append("\u00A0– ");
                }
            }
            else
            {
                result.Append(", ");
            }

            result.Append(end.Day);
            result.Append('.');
        }
    }
}