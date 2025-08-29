using System;
using System.Linq;
using KdyPojedeVlak.Web.Engine.Algorithms;
using Xunit;

namespace KdyPojedeVlak.Web.Tests.Engine.Algorithms;

public class CalendarNamerTests
{
    [Theory, ClassData(typeof(DetectNameTestCases))]
    public void TestDetectName(string bitmap, DateTime validFrom, DateTime validTo, string expected)
    {
        Assert.Equal(expected, CalendarNamer.DetectName(bitmap.Select(c => c == '1').ToArray(), validFrom, validTo));
    }

    private class DetectNameTestCases : TheoryData<string, DateTime, DateTime, string>
    {
        public DetectNameTestCases()
        {
            AddAll(Dt(2021, 2, 1), 40, "jede denně");
            AddAll(Dt(2020, 12, 20), 365, "jede denně");
            AddAll(Dt(2020, 4, 30), 3, "jede 30.\u00A0IV.\u00A0– 2.\u00A0V.");
            Add("1001", Dt(2021, 4, 30), Dt(2021, 5, 3), "jede 30.\u00A0IV., 3.\u00A0V.");
            Add("1101", Dt(2021, 4, 30), Dt(2021, 5, 3), "jede 30.\u00A0IV., 1., 3.\u00A0V.");
            Add("1111100111110001", Dt(2021, 3, 1), Dt(2021, 3, 16), "jede ⚒\uFE0E, nejede 15.\u00A0III.");
            Add("1111100100010001", Dt(2021, 3, 1), Dt(2021, 3, 16), "jede ⚒\uFE0E, nejede 9.–11., 15.\u00A0III.");
            Add("11ee11wwweewwwwweewww11ee11111ee1H111ee11111ee11111eeH1111ee11111", Dt(2025, 9, 25), Dt(2025, 11, 28), "jede ⚒\uFE0E, nejede 1.–15.\u00A0X.");
            //   wweewwwwweewwwwweewwwwweewwwwweewHwwweewwwwweewwwwweeHwwwweewwwww
            //         [             ]
            //   TFSSMTWTFSSMTWTFSSMTWTFSSMTWTFSSMTWTFSSMTWTFSSMTWTFSSMTWTFSSMTWTF
            //   222223         1111111111222222222233         1111111111222222222
            //   56789012345678901234567890123456789011234567890123456789012345678
            //         11111111111111111111111111111111111111111111111111111111111
            //   99999900000000000000000000000000000001111111111111111111111111111
            Add("001100111111100011000", Dt(2025, 9, 25), Dt(2025, 10, 15), "jede ✝︎⑥ a 1.–7.\u00A0X.");
            //         [     ]
            //   wweewwwwweewwwwweewww
            //   TFSSMTWTFSSMTWTFSSMTW
            //   222223         111111
            //   567890123456789012345
            //         111111111111111
            //   999999000000000000000
        }

        private void AddAll(DateTime from, int days, string expected)
        {
            Add(new string('1', days), from, from.AddDays(days - 1), expected);
        }
            
        private static DateTime Dt(int year, int month, int day) => new(year, month, day);
    }
}