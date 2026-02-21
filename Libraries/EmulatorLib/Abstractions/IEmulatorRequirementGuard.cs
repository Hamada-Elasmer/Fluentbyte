namespace EmulatorLib.Abstractions;

public interface IEmulatorRequirementGuard
{
    EmulatorRequirementResult Validate();
}
