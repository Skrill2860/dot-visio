using System;
using System.Globalization;
using Domain;

namespace GUI.VisioConversion.DotToVisioConversionHelpers;

public static class LabelPositionParser
{
    public static double[] ParseLabelPosition(string lps)
    {
        try
        {
            var lp = new double[2];
            var xy = lps.Split(',');
            for (var i = 0; i <= 1; i++)
            {
                lp[i] = Convert.ToDouble(xy[i], CultureInfo.InvariantCulture) / 72d;
            }

            return lp;
        }
        catch
        {
            throw new DotVisioException("Label position '" + lps + "' invalid");
        }
    }
}