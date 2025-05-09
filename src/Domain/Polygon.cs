using System;

namespace Domain;

public record Polygon
{
    public readonly Coordinate[] Point;
    public readonly int Sides;

    private Polygon(int nsides)
    {
        Sides = nsides;
        Point = new Coordinate[Sides];
        for (int i = 0, loopTo = Sides - 1; i <= loopTo; i++)
        {
            Point[i] = new Coordinate();
        }
    }

    public static Polygon CreatePolygon(int sides, double width, double height)
    {
        var polygon = new Polygon(sides);

        double theta;
        var incr = 2 * Math.PI / sides; // increment in radians
        if (sides % 2 == 0) // even number of sides = flat top
        {
            theta = Math.PI / 2d - incr / 2d;
            polygon.Point[0].X = width / 2d * Math.Cos(theta);
            polygon.Point[0].Y = height / 2d * Math.Sin(theta);
        }
        else // odd number of sides = vertex at top
        {
            theta = 90d * Math.PI / 180d;
            polygon.Point[0].X = 0d;
            polygon.Point[0].Y = height / 2d;
        }

        // Set first point vertically upwards
        // Last point is same as first (polygons are closed)
        polygon.Point[sides].X = polygon.Point[0].X;
        polygon.Point[sides].Y = polygon.Point[0].Y;
        for (int side = 1, loopTo = sides - 1; side <= loopTo; side++)
        {
            theta = theta - incr; // minus = draw clockwise
            polygon.Point[side].X = width / 2d * Math.Cos(theta);
            polygon.Point[side].Y = height / 2d * Math.Sin(theta);
        }

        return polygon;
    }
}