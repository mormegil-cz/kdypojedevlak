using JetBrains.Annotations;
using KdyPojedeVlak.Web.Engine.Algorithms;
using Xunit;

namespace KdyPojedeVlak.Web.Tests.Engine.Algorithms;

[TestSubject(typeof(ListMerger))]
public class ListMergerTest
{
    [Fact]
    public void MergeLists()
    {
        Assert.Equal([0, 2, 4, 1, 3, 5, 6, 7, 8, 9, 10, 12, 14, 11, 13], ListMerger.MergeLists([[1, 3, 5, 6, 7, 8, 9, 11, 13], [0, 2, 4, 6, 8, 9, 10, 12, 14]]));

        Assert.Equal([1, 2, 3, 4, 5, 6, 5, 4, 7, 8, 9], ListMerger.MergeLists([[1, 2, 3, 4, 5, 6, 7, 8, 9], [1, 2, 3, 6, 5, 4, 7, 8, 9]]));
    }
}