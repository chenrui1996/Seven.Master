namespace Seven.Domain.Enums;

/// <summary>设备通讯协议</summary>
public enum CommProtocol
{
    /// <summary>西门子 Step7（以太网）</summary>
    Step7 = 1,

    /// <summary>Modbus TCP</summary>
    ModbusTcp = 2
}

/// <summary>连接运行态</summary>
public enum CommConnectionState
{
    Disconnected = 0,
    Connecting = 1,
    Connected = 2,
    Faulted = 3,
    Reconnecting = 4
}

/// <summary>点位数据类型</summary>
public enum CommDataType
{
    Bool = 1,
    Byte = 2,
    Int16 = 3,
    UInt16 = 4,
    Int32 = 5,
    UInt32 = 6,
    Float = 7,
    Double = 8,
    String = 9
}

/// <summary>Modbus 寄存器区</summary>
public enum CommModbusArea
{
    Coil = 1,
    DiscreteInput = 2,
    HoldingRegister = 3,
    InputRegister = 4
}
