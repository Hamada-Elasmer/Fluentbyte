public sealed class CastleState 
{
    public int Level { get; private set; }

    public CastleState(int level = 0)
    {
        Level = level;
    }

    public void SetLevel(int level) => Level = level;
}