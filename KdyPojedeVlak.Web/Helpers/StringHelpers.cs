using System;
using JetBrains.Annotations;

namespace KdyPojedeVlak.Web.Helpers;

public static class StringHelpers
{
    [ContractAnnotation("str: null, quotes: notnull => null; str: notnull, quotes: notnull => notnull")]
    public static string? Quote(string? str, string quotes) => String.IsNullOrEmpty(str) ? null : quotes[0] + str + quotes[1];
}