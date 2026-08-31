using FluentAssertions;
using Seven.Application.Simulator;
using Seven.Infrastructure.Simulator;

namespace Seven.Tests.Simulator;

public class SimulationImportServiceTests
{
    private readonly SimulationImportService _svc = new();

    [Fact]
    public void ImportGrid_ValidCells_ReturnsNodes()
    {
        var result = _svc.ImportGrid(new ImportGridRequest(
            "fourway",
            new[]
            {
                new ImportGridCellDto("Fw.A-01", 0, 0),
                new ImportGridCellDto("Fw.A-02", 80, 0),
            }));

        result.Map.PackId.Should().Be("fourway");
        result.Map.Nodes.Should().HaveCount(2);
        result.Map.Nodes.Select(n => n.Code).Should().BeEquivalentTo("Fw.A-01", "Fw.A-02");
        result.Warnings.Should().BeEmpty();
    }

    [Fact]
    public void ImportGrid_DuplicateCodes_SkipsDuplicatesWithWarning()
    {
        var result = _svc.ImportGrid(new ImportGridRequest(
            "stacker",
            new[]
            {
                new ImportGridCellDto("Stk.A-01", 0, 0),
                new ImportGridCellDto("Stk.A-01", 80, 0),
            }));

        result.Map.Nodes.Should().ContainSingle();
        result.Warnings.Should().Contain(w => w.Contains("Duplicate"));
    }

    [Fact]
    public void PreviewRouteGroups_TwoComponents_SuggestsTwoGroups()
    {
        var nodes = new[]
        {
            new SimMapNodeDto("n1", "A", 0, 0),
            new SimMapNodeDto("n2", "B", 80, 0),
            new SimMapNodeDto("n3", "C", 160, 0),
            new SimMapNodeDto("n4", "D", 0, 80),
        };
        var edges = new[]
        {
            new SimMapEdgeDto("e1", "n1", "n2"),
            new SimMapEdgeDto("e2", "n2", "n3"),
        };

        var result = _svc.PreviewRouteGroups(new RouteGroupsPreviewRequest("fourway", nodes, edges));

        result.Suggested.Should().HaveCount(2);
        result.Suggested[0].NodeIds.Should().HaveCount(3);
        result.Suggested[1].NodeIds.Should().ContainSingle().Which.Should().Be("n4");
        result.Warnings.Should().Contain(w => w.Contains("isolated"));
    }

    [Fact]
    public void PreviewRouteGroups_Cycle_AddsCycleWarning()
    {
        var nodes = new[]
        {
            new SimMapNodeDto("n1", "A", 0, 0),
            new SimMapNodeDto("n2", "B", 80, 0),
            new SimMapNodeDto("n3", "C", 40, 80),
        };
        var edges = new[]
        {
            new SimMapEdgeDto("e1", "n1", "n2"),
            new SimMapEdgeDto("e2", "n2", "n3"),
            new SimMapEdgeDto("e3", "n3", "n1"),
        };

        var result = _svc.PreviewRouteGroups(new RouteGroupsPreviewRequest("fourway", nodes, edges));

        result.Suggested.Should().ContainSingle();
        result.Warnings.Should().Contain(w => w.Contains("cycle"));
    }
}
