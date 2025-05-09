namespace GoldParser;

public class Position
{
    public int Column;
    public int Line;

    public Position()
    {
        Line = 0;
        Column = 0;
    }

    public void Copy(Position Pos)
    {
        Column = Pos.Column;
        Line = Pos.Line;
    }
}