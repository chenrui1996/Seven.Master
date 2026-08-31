using System.Text.Json;
using FluentAssertions;
using Seven.Application.Simulator;

namespace Seven.Tests.Simulator;

public class SimProjectDtoTests
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    [Fact]
    public void Deserialize_MinimalJson_WithNodesOnly_Succeeds()
    {
        const string json = """
            {
              "version": 1,
              "meta": {
                "name": "Demo",
                "features": {
                  "wms": true,
                  "orchestrationBus": true,
                  "hotStore": false,
                  "deviceComm": false,
                  "simulator": true,
                  "wcsPacks": { "stacker": true, "fourWay": false, "boxSort": false }
                },
                "runtimeMode": "Simulation"
              },
              "map": {
                "packId": "stacker",
                "nodes": [{ "id": "1", "code": "Stk.A-01", "x": 0, "y": 0 }]
              }
            }
            """;

        var project = JsonSerializer.Deserialize<SimProjectDto>(json, JsonOptions);

        project.Should().NotBeNull();
        project!.Version.Should().Be(1);
        project.Meta.Name.Should().Be("Demo");
        project.Meta.SimCommsMode.Should().Be("Trigger");
        project.Map.Nodes.Should().ContainSingle().Which.Code.Should().Be("Stk.A-01");
        project.Map.RequestPoints.Should().BeNull();
        project.Scada.Should().BeNull();
        project.Promote.Should().BeNull();
    }

    [Fact]
    public void Deserialize_WithRequestPoints_HasNonEmptyRequestPoints()
    {
        const string json = """
            {
              "version": 1,
              "meta": {
                "name": "Demo",
                "features": {
                  "wms": true,
                  "orchestrationBus": true,
                  "hotStore": false,
                  "deviceComm": false,
                  "simulator": true,
                  "wcsPacks": { "stacker": true, "fourWay": false, "boxSort": false }
                },
                "runtimeMode": "Simulation",
                "simCommsMode": "Gateway"
              },
              "map": {
                "packId": "stacker",
                "nodes": [{ "id": "1", "code": "Stk.A-01", "x": 0, "y": 0 }],
                "requestPoints": [
                  { "code": "Stk.RP_IN_01", "mappedLocationCode": "Stk.A-01" }
                ]
              },
              "scada": { "views": [] },
              "promote": { "devices": [] }
            }
            """;

        var project = JsonSerializer.Deserialize<SimProjectDto>(json, JsonOptions);

        project.Should().NotBeNull();
        project!.Meta.SimCommsMode.Should().Be("Gateway");
        project.Map.RequestPoints.Should().NotBeNull().And.NotBeEmpty();
        project.Map.RequestPoints!.Single().Code.Should().Be("Stk.RP_IN_01");
        project.Map.RequestPoints.Single().MappedLocationCode.Should().Be("Stk.A-01");
        project.Scada!.Views.Should().NotBeNull().And.BeEmpty();
        project.Promote!.Devices.Should().NotBeNull().And.BeEmpty();
    }
}
