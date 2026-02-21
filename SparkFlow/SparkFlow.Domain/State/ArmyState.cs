namespace SparkFlow.Domain.State;

public sealed class ArmyState
{
    public int Power { get; private set; }

    public ArmyState(int power = 0)
    {
        Power = power;
    }

    public void SetPower(int power) => Power = power;
}