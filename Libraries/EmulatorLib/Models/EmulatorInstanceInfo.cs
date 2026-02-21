using EmulatorLib.Abstractions;

namespace EmulatorLib.Models;

public sealed record EmulatorInstanceInfo(
    string InstanceId,
    string Name,
    int? AdbPort,
    EmulatorState State,
    string RawLine
);
