using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization.Infrastructure;

namespace KdyPojedeVlak.Engine.Algorithms
{
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

        private static readonly string[] monthToRoman = { "", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX", "X", "XI", "XII" };

        private static readonly HashSet<DateTime> holidays = new HashSet<DateTime>
        {
            new DateTime(2017, 12, 24),
            new DateTime(2017, 12, 25),
            new DateTime(2017, 12, 26),
            new DateTime(2018, 01, 01),
            new DateTime(2018, 03, 30),
            new DateTime(2018, 04, 02),
            new DateTime(2018, 05, 01),
            new DateTime(2018, 05, 08),
            new DateTime(2018, 07, 05),
            new DateTime(2018, 07, 06),
            new DateTime(2018, 09, 28),
            new DateTime(2018, 10, 28),
            new DateTime(2018, 11, 17),
            new DateTime(2018, 12, 24),
            new DateTime(2018, 12, 25),
            new DateTime(2018, 12, 26)
        };

        private static readonly Dictionary<DayClass, Predicate<DateTime>> classifiers = new Dictionary<DayClass, Predicate<DateTime>>(7)
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
            public SortedSet<DateTime> YesDates = new SortedSet<DateTime>();
            public SortedSet<DateTime> NoDates = new SortedSet<DateTime>();
        }

        private class NamingResult
        {
            public string Name;
            public SortedSet<DateTime> ExceptionalGo;
            public SortedSet<DateTime> ExceptionalNoGo;
        }

        private static readonly List<Func<Dictionary<DayClass, ClassPresence>, NamingResult>> namingStrategies = new List<Func<Dictionary<DayClass, ClassPresence>, NamingResult>>
        {
            EverydayStrategy,
            WorkdaysStrategy,
            WorkdaysAndSaturdaysStrategy,
            HolidaysStrategy,
            HolidaysAndSaturdaysStrategy,
            DayOfWeekStrategy,
        };

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
                    AddAll(exceptionalNoGo, presence.NoDates);
                }
                else
                {
                    AddAll(exceptionalGo, presence.YesDates);
                }
            }
            return new NamingResult
            {
                Name = resultName.ToString(),
                ExceptionalGo = exceptionalGo,
                ExceptionalNoGo = exceptionalNoGo
            };
        }

        private static NamingResult EverydayStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new NamingResult
        {
            Name = "denně",
            ExceptionalGo = Sets<DateTime>.EmptySortedSet,
            ExceptionalNoGo = classPresences[DayClass.All].NoDates
        };

        private static NamingResult WorkdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new NamingResult
        {
            Name = "⚒",
            ExceptionalGo = new SortedSet<DateTime>(classPresences[DayClass.Holiday].YesDates.Concat(classPresences[DayClass.Saturday].YesDates)),
            ExceptionalNoGo = classPresences[DayClass.Workday].NoDates
        };

        private static NamingResult WorkdaysAndSaturdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new NamingResult
        {
            Name = "⚒⑥",
            ExceptionalGo = new SortedSet<DateTime>(classPresences[DayClass.Sunday].YesDates.Concat(classPresences[DayClass.NonSaturdayHoliday].YesDates)),
            ExceptionalNoGo = new SortedSet<DateTime>(classPresences[DayClass.Workday].NoDates.Concat(classPresences[DayClass.Saturday].NoDates))
        };

        private static NamingResult HolidaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new NamingResult
        {
            Name = "✝",
            ExceptionalGo = new SortedSet<DateTime>(classPresences[DayClass.Workday].YesDates.Concat(classPresences[DayClass.SaturdayNonHoliday].YesDates)),
            ExceptionalNoGo = new SortedSet<DateTime>(classPresences[DayClass.Holiday].NoDates)
        };

        private static NamingResult HolidaysAndSaturdaysStrategy(Dictionary<DayClass, ClassPresence> classPresences) => new NamingResult
        {
            Name = "✝⑥",
            ExceptionalGo = new SortedSet<DateTime>(classPresences[DayClass.Workday].YesDates),
            ExceptionalNoGo = new SortedSet<DateTime>(classPresences[DayClass.Holiday].NoDates.Concat(classPresences[DayClass.SaturdayNonHoliday].NoDates))
        };

        private static Predicate<DateTime> MakeDayClassifier(DayOfWeek dayOfWeek)
        {
            return dt => dt.DayOfWeek == dayOfWeek;
        }

        private static void AddAll<T>(ISet<T> set, IEnumerable<T> elems)
        {
            foreach (var elem in elems)
            {
                set.Add(elem);
            }
        }

        public static string DetectName(BitArray calendarBitmap, DateTime validFrom, DateTime validTo)
        {
            // TODO: realStartDate, realEndDate

            if (calendarBitmap.Cast<bool>().All(value => !value))
            {
                return "jede pp";
            }

            if (calendarBitmap.Cast<bool>().All(value => value))
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

            NamingResult bestNaming = null;
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

        private static void AppendListOfDays(StringBuilder result, SortedSet<DateTime> dates)
        {
            int lastMonth = -1;
            foreach (var exception in dates)
            {
                if (exception.Month != lastMonth && lastMonth > 0)
                {
                    result.Append(' ');
                    result.Append(monthToRoman[lastMonth]);
                    result.Append('.');
                }

                if (lastMonth > 0) result.Append(", ");
                result.Append(exception.Day);
                result.Append(".");
                lastMonth = exception.Month;
            }
            if (lastMonth > 0)
            {
                result.Append(' ');
                result.Append(monthToRoman[lastMonth]);
                result.Append('.');
            }
        }
    }
}