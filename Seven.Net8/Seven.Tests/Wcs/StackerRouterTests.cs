using FluentAssertions;
using Seven.Infrastructure.Wcs.Packs.Stacker;

namespace Seven.Tests.Wcs;

public class StackerRouterTests
{
    [Fact]
    public void FindPath_ShouldPreferLowerWeight_AndSkipFullCapacity()
    {
        var edges = new List<StackerRouteEdge>
        {
            new(1, "A", "B", 1, "CV", 1, 0),
            new(2, "B", "C", 1, "CV", 1, 0),
            new(3, "A", "C", 10, "SRM", 1, 0),
            new(4, "B", "X", 1, "CV", 1, 1), // full
        };

        var path = StackerRouter.FindPathRouteIds("A", "C", edges);
        path.Should().Equal(1, 2);
    }

    [Fact]
    public void FindPath_SamePoint_ShouldBeEmpty()
    {
        StackerRouter.FindPathRouteIds("A", "A", []).Should().BeEmpty();
    }
}
