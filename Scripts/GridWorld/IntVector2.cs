public class IntVector2
{
    public int x;
    public int y;

    public IntVector2(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    public override int GetHashCode()
    {
        int hash = 17;
        hash = hash * 31 + x.GetHashCode();
        hash = hash * 31 + y.GetHashCode();
        return hash;
    }
}
