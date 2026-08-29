using FluentAssertions;
using Seven.Application.Wcs;
using Seven.Infrastructure.Wcs.Triggers;
using Xunit;

namespace Seven.Tests.Wcs;

public class EquipmentTriggerPortTests
{
    [Fact]
    public async Task DestinationRequested_ShouldInvoke_Subscriber()
    {
        var port = new InMemoryEquipmentTriggerPort();
        DestinationRequestTrigger? got = null;
        port.DestinationRequested += t => { got = t; return Task.CompletedTask; };
        await port.SimulateDestinationRequestAsync(new DestinationRequestTrigger(
            "TP001", "RP_IN_01", 1, 1, "OK"));
        got!.ContainerCode.Should().Be("TP001");
    }

    [Fact]
    public async Task DispatchDestinationAsync_ShouldRecord_Command()
    {
        var port = new InMemoryEquipmentTriggerPort();
        var cmd = new DispatchDestinationCommand("TP001", "RP_OUT_01");
        await port.DispatchDestinationAsync(cmd);
        port.DispatchedDestinations.Should().Contain(cmd);
    }
}
