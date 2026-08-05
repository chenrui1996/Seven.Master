using Seven.Domain.Enums;

namespace Seven.Infrastructure.DeviceComm.Drivers;

public sealed class PlcDriverFactory : IPlcDriverFactory
{
    public IPlcDriver Create(CommProtocol protocol) => protocol switch
    {
        CommProtocol.Step7 => new Step7PlcDriver(),
        CommProtocol.ModbusTcp => new ModbusTcpPlcDriver(),
        _ => throw new NotSupportedException($"不支持的协议: {protocol}")
    };
}
