using System;
using System.Globalization;
using Domain;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class BoundingBoxParser
{
    public static BoundingBox ParseBoundingBox(string bbs)
    {
        var customCulture = (CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
        customCulture.NumberFormat.NumberDecimalSeparator = ".";

        try
        {
            var bb = new double[4];
            var xy = bbs.Split(',');
            for (var i = 0; i <= 3; i++)
            {
                // 1/72 of an inch is how Point (typography) is defined, dot uses points for specifying coordinates
                bb[i] = Convert.ToDouble(xy[i], customCulture) / 72d;
            }

            return new BoundingBox(bb[0], bb[1], bb[2], bb[3]);
        }
        catch (Exception ex)
        {
            throw new DotVisioException("Page's bounding box '" + bbs + "' invalid", ex);
        }
    }
}