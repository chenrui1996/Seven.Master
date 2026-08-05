using FluentAssertions;
using Seven.Domain.Entities.DeviceComm;
using Seven.Domain.Enums;
using Seven.Infrastructure.DeviceComm.Drivers;

namespace Seven.Tests.Unit;

public class DeviceCommDriverTests
{
    [Theory]
    [InlineData("HR:0", CommModbusArea.HoldingRegister, (ushort)0)]
    [InlineData("Coil:10", CommModbusArea.Coil, (ushort)10)]
    [InlineData("IR:5", CommModbusArea.InputRegister, (ushort)5)]
    [InlineData("DI:1", CommModbusArea.DiscreteInput, (ushort)1)]
    public void Modbus_ParseAddress_Works(string address, CommModbusArea area, ushort addr)
    {
        var (a, ad, qty) = ModbusTcpPlcDriver.ParseAddress(address, 1);
        a.Should().Be(area);
        ad.Should().Be(addr);
        qty.Should().Be(1);
    }

    [Fact]
    public void Step7_ResolveAddress_FromStructuredFields()
    {
        var point = new CommPoint
        {
            Code = "Flag",
            DataType = CommDataType.Bool,
            DbNumber = 1,
            ByteOffset = 0,
            BitOffset = 2
        };
        Step7PlcDriver.ResolveAddress(point).Should().Be("DB1.DBX0.2");
    }

    [Fact]
    public void Step7_ResolveAddress_PrefersExplicitAddress()
    {
        var point = new CommPoint
        {
            Code = "X",
            Address = "DB10.DBW4",
            DbNumber = 1,
            ByteOffset = 0,
            DataType = CommDataType.Int16
        };
        Step7PlcDriver.ResolveAddress(point).Should().Be("DB10.DBW4");
    }

    [Fact]
    public void DisabledGateway_IsNotEnabled()
    {
        var g = new Seven.Infrastructure.DeviceComm.DisabledDeviceCommGateway();
        g.IsEnabled.Should().BeFalse();
        var act = () => g.ConnectAsync(1);
        act.Should().ThrowAsync<InvalidOperationException>();
    }
}
