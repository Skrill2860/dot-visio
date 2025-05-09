using System;
using System.Collections.Generic;
using System.Globalization;
using Common;
using Domain;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class SplineExtractor
{
    public static List<Coordinate> ExtractSpline(string pos)
    {
        var spline = new List<Coordinate>();
        if (string.IsNullOrEmpty(pos))
        {
            return spline;
        }

        try
        {
            // This can be a bit messy. Splines can look like this: 
            // s,416,197 e,477,168 424,193 436,188 449,182 462,176 462,176 462,176 463,176
            // S and E mark the start and end knots and can be anywhere. Remove them before parsing the rest
            pos = pos.CleanupPos() + " "; // add a space to ease case ".... e,123,456" (so that indexof always finds a space)
            var knot = "";
            var kp = pos.IndexOf("s,", StringComparison.Ordinal); // start point defined ?
            if (kp >= 0)
            {
                var sp = pos.IndexOf(" ", kp, StringComparison.Ordinal);
                knot = pos.Substring(kp + 2, sp - kp - 2); // extract start knot
                pos = pos.Remove(kp, sp - kp); // remove it from where it is
                pos = knot + " " + pos; // and add it at the beginning
            }

            kp = pos.IndexOf("e,", StringComparison.Ordinal); // end point defined ?
            if (kp >= 0)
            {
                var sp = pos.IndexOf(" ", kp, StringComparison.Ordinal);
                knot = pos.Substring(kp + 2, sp - kp - 2); // extract end knot
                pos = pos.Remove(kp, sp - kp); // remove it from where it is
                pos += knot; // and add it at the end (pos already has a trailing space)
            }

            // remove unwanted spaces before splitting
            pos = pos.Trim();
            kp = pos.IndexOf("  ", StringComparison.Ordinal);
            while (kp >= 0)
            {
                pos = pos.Replace("  ", " ");
                kp = pos.IndexOf("  ", StringComparison.Ordinal);
            }

            var coords = pos.Split(' ');
            for (int i = 0, loopTo = coords.GetUpperBound(0); i <= loopTo; i++)
            {
                var xy = coords[i].Split(',');
                var sp = new Coordinate();
                sp.X = Convert.ToDouble(xy[0], CultureInfo.InvariantCulture) / 72d;
                sp.Y = Convert.ToDouble(xy[1], CultureInfo.InvariantCulture) / 72d;
                if (i > 0) // Check for sucessive knots at identical locations
                {
                    if (sp.X == spline[spline.Count - 1].X && sp.Y == spline[spline.Count - 1].Y)
                    {
                        sp = null;
                    }
                }

                if (sp is not null)
                {
                    spline.Add(sp);
                }
            }

            if (spline.Count == 1)
            {
                // All knots have identical X,Y. This is a bug seen in NEATO when shapes are on top of each-other
                // Add a fake knot, just next to the first knot, to make the spline valid (nobody will notice).
                var sp = new Coordinate();
                sp.X = spline[0].X + 1.0d / 72.0d;
                sp.Y = spline[0].Y + 1.0d / 72.0d;
                spline.Add(sp);
            }
        }
        catch (Exception ex)
        {
            throw new DotVisioException("Spline '" + pos + "' is invalid, ignored (" + ex.Message + ")");
        }

        return spline;
    }
}