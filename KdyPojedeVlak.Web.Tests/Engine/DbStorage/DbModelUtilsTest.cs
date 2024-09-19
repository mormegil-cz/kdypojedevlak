using System;
using KdyPojedeVlak.Web.Engine.DbStorage;
using Xunit;

namespace KdyPojedeVlak.Web.Tests.Engine.DbStorage;

public class DbModelUtilsTest
{
    [Fact]
    public void TestParseEnumList()
    {
        Assert.Equal([], DbModelUtils.ParseEnumList<DayOfWeek>(""));
        Assert.Equal([DayOfWeek.Monday, DayOfWeek.Saturday], DbModelUtils.ParseEnumList<DayOfWeek>("Monday;Saturday"));
        Assert.Equal([DayOfWeek.Sunday, DayOfWeek.Friday, DayOfWeek.Monday], DbModelUtils.ParseEnumList<DayOfWeek>("0;Friday;1"));
    }

    [Fact]
    public void TestBuildEnumList()
    {
        Assert.Equal("", DbModelUtils.BuildEnumList<DayOfWeek>([]));
        Assert.Equal("1;6", DbModelUtils.BuildEnumList([DayOfWeek.Monday, DayOfWeek.Saturday]));
        Assert.Equal("0;5;1", DbModelUtils.BuildEnumList([DayOfWeek.Sunday, DayOfWeek.Friday, DayOfWeek.Monday]));
    }
}