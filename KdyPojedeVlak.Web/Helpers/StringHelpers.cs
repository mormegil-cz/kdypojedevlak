using System;

namespace KdyPojedeVlak.Web.Helpers
{
    public static class StringHelpers
    {
        public static string Quote(string str, string quotes)
        {
            return String.IsNullOrEmpty(str) ? null : quotes[0] + str + quotes[1];
        }
    }
}