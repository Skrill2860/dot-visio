namespace Domain;

public record BoundingBox(double MinX, double MinY, double MaxX, double MaxY)
{
    public double MinX { get; } = MinX;
    public double MinY { get; } = MinY;
    public double MaxX { get; } = MaxX;
    public double MaxY { get; } = MaxY;
}