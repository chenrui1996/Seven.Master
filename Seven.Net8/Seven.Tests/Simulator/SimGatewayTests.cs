using FluentAssertions;
using Seven.Infrastructure.Simulator.Gateway;

namespace Seven.Tests.Simulator;

public class SimGatewayMessageTests
{
    [Fact]
    public void FormatReceived_joins_connectionId_and_payload()
    {
        SimGatewayMessage.FormatReceived("conn-1", "hello").Should().Be("conn-1|hello");
    }

    [Theory]
    [InlineData("conn-1|hello", "conn-1", "hello")]
    [InlineData("tcp-42|{\"cmd\":\"move\"}", "tcp-42", "{\"cmd\":\"move\"}")]
    public void TryParseReceived_parses_framed_message(string framed, string expectedConn, string expectedPayload)
    {
        var ok = SimGatewayMessage.TryParseReceived(framed, out var connectionId, out var payload);

        ok.Should().BeTrue();
        connectionId.Should().Be(expectedConn);
        payload.Should().Be(expectedPayload);
    }

    [Theory]
    [InlineData("")]
    [InlineData("no-separator")]
    [InlineData("|missing-id")]
    public void TryParseReceived_rejects_invalid_frames(string framed)
    {
        SimGatewayMessage.TryParseReceived(framed, out _, out _).Should().BeFalse();
    }

    [Fact]
    public void BuildEchoReply_returns_same_line()
    {
        SimGatewayMessage.BuildEchoReply("ping").Should().Be("ping");
    }
}

public class SimWcsProxyHubTests
{
    [Fact]
    public void Hub_can_be_constructed()
    {
        var hub = Activator.CreateInstance<SimWcsProxyHub>();
        hub.Should().NotBeNull();
        SimWcsProxyHub.GroupName.Should().Be("SimWcsProxy");
    }
}
