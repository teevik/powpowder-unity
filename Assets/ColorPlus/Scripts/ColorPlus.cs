using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ColorPlus
{
    #region Public Color Difference Methods
    // Calculates color difference difference by CIEDE2000 Delta E formula https://en.wikipedia.org/wiki/Color_difference
    public static float ColorDifference(Color color1, Color color2)
    {
        return CalculateColorDifference(ToLab(color1), ToLab(color2));
    }

    public static float ColorDifference(Lab color1, Lab color2)
    {
        return CalculateColorDifference(color1, color2);
    }
    public static float ColorDifference(Lch color1, Lch color2)
    {
        return CalculateColorDifference(ToLab(color1), ToLab(color2));
    }
    #endregion

    #region Public Conversion Methods
    //Converts Color/Lab/Lch to Color/Lab/Lch
    public static Lab ToLab(Color color)
    {
        return XyzToLab(RgbToXyz(color));
    }
    public static Lab ToLab(Lch color)
    {
        return LchToLab(color);
    }
    public static Color ToRgb(Lab color)
    {
        return XyzToRgb((LabToXyz(color)));
    }
    public static Color ToRgb(Lch color)
    {
        return (XyzToRgb(LabToXyz(LchToLab(color))));
    }
    public static Lch ToLch(Color color)
    {
        return LabToLch(XyzToLab(RgbToXyz(color)));
    }
    public static Lch ToLch(Lab color)
    {
        return LabToLch(color);
    }
    #endregion

    #region Public Lerp Methods
    //Linearly interpolates between colors from and to by t in Lab/Lch color space.
    public static Color LerpInLab(Color from, Color to, float t)
    {
        return ToRgb(LabLerp(ToLab(from), ToLab(to), t));
    }
    public static Color LerpInLab(Lab from, Lab to, float t)
    {
        return ToRgb(LabLerp(from, to, t));
    }
    public static Color LerpInLab(Lch from, Lch to, float t)
    {
        return ToRgb(LabLerp(ToLab(from), ToLab(to), t));
    }

    public static Color LerpInLch(Color from, Color to, float t, LerpMode lerpMode)
    {
        return ToRgb(LchLerp(ToLch(from), ToLch(to), t, lerpMode));
    }

    public static Color LerpInLch(Lab from, Lab to, float t, LerpMode lerpMode)
    {
        return ToRgb(LchLerp(ToLch(from), ToLch(to), t, lerpMode));
    }

    public static Color LerpInLch(Lch from, Lch to, float t, LerpMode lerpMode)
    {
        return ToRgb(LchLerp(from, to, t, lerpMode));
    }

    public static Color LerpInLch(Color from, Color to, float t)
    {
        return ToRgb(LchLerp(ToLch(from), ToLch(to), t, LerpMode.ShorterWay));
    }

    public static Color LerpInLch(Lab from, Lab to, float t)
    {
        return ToRgb(LchLerp(ToLch(from), ToLch(to), t, LerpMode.ShorterWay));
    }

    public static Color LerpInLch(Lch from, Lch to, float t)
    {
        return ToRgb(LchLerp(from, to, t, LerpMode.ShorterWay));
    }
    #endregion

    #region Private Color Difference Methods
    private static float CalculateColorDifference(Lab lab1, Lab lab2)
    {
        Lch lch1 = LabToLch(lab1);
        Lch lch2 = LabToLch(lab2);

        float deltaL = lch2.l - lch1.l;
        float averageL = (lch1.l + lch2.l) / 2;
        float averageC = (lch1.c + lch2.c) / 2;

        float c7 = averageC * averageC * averageC * averageC * averageC * averageC * averageC;
        float gamma = 1 - Mathf.Sqrt(c7 / (c7 + 6103515625));

        float a1 = lab1.a + (lab1.a / 2) * gamma;
        float a2 = lab2.a + (lab2.a / 2) * gamma;

        float c1 = Mathf.Sqrt(a1 * a1 + lab1.b * lab1.b);
        float c2 = Mathf.Sqrt(a2 * a2 + lab2.b * lab2.b);

        float deltaC = c2 - c1;
        float _averageC = (c1 + c2) / 2;

        float h1 = ((Mathf.Rad2Deg * Mathf.Atan2(lab1.b, a1)) + 360) % 360;
        float h2 = ((Mathf.Rad2Deg * Mathf.Atan2(lab2.b, a2)) + 360) % 360;

        float deltaH = CalculateDeltaH(h1, h2);

        float _deltaH = 2 * Mathf.Sqrt(c1 * c2) * Mathf.Sin(Mathf.Deg2Rad * deltaH / 2);

        float _H = Calculate_H(h1, h2);

        float T = 1 -
            0.17f * Mathf.Cos(Mathf.Deg2Rad * (1 * _H - 30)) +
            0.24f * Mathf.Cos(Mathf.Deg2Rad * (2 * _H + 0)) +
            0.32f * Mathf.Cos(Mathf.Deg2Rad * (3 * _H + 6)) -
            0.20f * Mathf.Cos(Mathf.Deg2Rad * (4 * _H - 63));

        float lambda = (averageL - 50) * (averageL - 50);

        float Sl = 1 + (0.015f * lambda / Mathf.Sqrt(20 + lambda));
        float Sc = 1 + 0.045f * _averageC;
        float Sh = 1 + 0.015f * _averageC * T;

        float eta = ((_H - 275) / 25) * ((_H - 275) / 25);

        float _c7 = _averageC * _averageC * _averageC * _averageC * _averageC * _averageC * _averageC;
        float _gamma = 1 - Mathf.Sqrt(_c7 / (_c7 + 6103515625));

        float Rt = -2 * _gamma * Mathf.Sin(60 * Mathf.Exp(-Mathf.Deg2Rad * eta));

        float deltaE = Mathf.Sqrt(
            (deltaL / Sl) * (deltaL / Sl) +
            (deltaC / Sc) * (deltaC / Sc) +
            (_deltaH / Sh) * (_deltaH / Sh) +
            Rt * (deltaC / Sc) * (_deltaH / Sh)
            );
        return deltaE;
    }
    private static float Calculate_H(float h1, float h2)
    {
        float difference = Mathf.Abs(h1 - h2);
        if (difference <= 180)
        {
            return (h1 + h2) / 2;
        }
        else if (h1 + h2 < 360)
        {
            return (h1 + h2 + 360) / 2;
        }
        else
        {
            return (h1 + h2 - 360) / 2;
        }
    }
    private static float CalculateDeltaH(float h1, float h2)
    {
        float difference = Mathf.Abs(h1 - h2);
        if (difference <= 180)
        {
            return h2 - h1;
        }
        else if (h2 <= h1)
        {
            return h2 - h1 + 360;
        }
        else
        {
            return h2 - h1 - 360;
        }
    }
    #endregion

    #region Private Lab and Lch Lerping Methods
    private static Lab LabLerp(Lab Color1, Lab Color2, float lerpFactor)
    {
        float l = Color1.l + lerpFactor * (Color2.l - Color1.l);
        float a = Color1.a + lerpFactor * (Color2.a - Color1.a);
        float b = Color1.b + lerpFactor * (Color2.b - Color1.b);
        return new Lab(l, a, b);
    }

    private static Lch LchLerp(Lch Color1, Lch Color2, float lerpFactor, LerpMode lerpMode)
    {
        float difference = Mathf.Abs(Color1.h - Color2.h);
        float h1 = Color1.h;
        float h2 = Color2.h;

        switch (lerpMode)
        {
            case LerpMode.ShorterWay:
                if (difference > 180)
                {
                    if (Color2.h > Color1.h)
                    {
                        h1 += 360;
                    }
                    else
                    {
                        h2 += 360;
                    }
                }
                break;

            case LerpMode.LongerWay:
                if (difference < 180)
                {
                    if (Color2.h > Color1.h)
                    {
                        h1 += 360;
                    }
                    else
                    {
                        h2 += 360;
                    }
                }
                break;

            case LerpMode.Clockwise:
                if (h2 > h1)
                {
                    h2 -= 360;
                }
                break;

            case LerpMode.CounterClockwise:
                if (h1 > h2)
                {
                    h2 += 360;
                }
                break;

        }

        float l = Color1.l + lerpFactor * (Color2.l - Color1.l);
        float c = Color1.c + lerpFactor * (Color2.c - Color1.c);
        float h = (h1 + lerpFactor * (h2 - h1)) % 360;
        return new Lch(l, c, h);
    }
    #endregion

    #region Private Lab and Lch Conversion Methods
    private static Lch LabToLch(Lab color)
    {
        float l = color.l;
        float c = Mathf.Sqrt(color.a * color.a + color.b * color.b);
        float h = NormalizeAngle(Mathf.Rad2Deg * Mathf.Atan2(color.b, color.a));
        return new Lch(l, c, h);
    }

    private static float NormalizeAngle(float degrees)
    {

        if (degrees % 360 >= 0)
            return degrees % 360;
        else
            return degrees + 360;

    }

    private static Lab LchToLab(Lch color)
    {
        float l = color.l;
        float a = color.c * Mathf.Cos(Mathf.Deg2Rad * color.h);
        float b = color.c * Mathf.Sin(Mathf.Deg2Rad * color.h);
        return new Lab(l, a, b);
    }
    #endregion

    #region Private Xyz and Lab Conversion Methods
    private static float FowardTransformation(float t)
    {
        if (t > 0.008856452f)
        {
            return Mathf.Pow(t, 0.3333333f);
        }
        else
        {
            return t / 0.1284185f + 0.1379310f;
        }
    }
    private static float ReverseTransformation(float t)
    {
        if (t > 0.2068965f)
        {
            return Mathf.Pow(t, 3f);
        }
        else
        {
            return 0.1284185f * (t - 0.1379310f);
        }
    }
    private static Lab XyzToLab(Xyz color)
    {
        float x = FowardTransformation(color.x / IlluminantD65.x);
        float y = FowardTransformation(color.y / IlluminantD65.y);
        float z = FowardTransformation(color.z / IlluminantD65.z);

        float l = 116 * y - 16;
        float a = 500 * (x - y);
        float b = 200 * (y - z);
        return new Lab(l, a, b);
    }

    private static Xyz LabToXyz(Lab color)
    {
        float x = IlluminantD65.x * ReverseTransformation((color.l + 16) / 116 + color.a / 500);
        float y = IlluminantD65.y * ReverseTransformation((color.l + 16) / 116);
        float z = IlluminantD65.z * ReverseTransformation((color.l + 16) / 116 - color.b / 200);
        return new Xyz(x, y, z);
    }
    private static Xyz IlluminantD65 = new Xyz
    (
        0.95047f,
        1f,
        1.08883f
    );
    #endregion

    #region Private Rgb and Xyz Conversions Methods
    private static Xyz RgbToXyz(Color color)
    {
        float r = StandardToLinear(color.r);
        float g = StandardToLinear(color.g);
        float b = StandardToLinear(color.b);

        float x = r * 0.4124f + g * 0.3576f + b * 0.1805f;
        float y = r * 0.2126f + g * 0.7152f + b * 0.0722f;
        float z = r * 0.0193f + g * 0.1192f + b * 0.9505f;
        return new Xyz(x, y, z);
    }
    private static Color XyzToRgb(Xyz color)
    {

        float r = color.x * 3.2406f + color.y * -1.5372f + color.z * -0.4986f;
        float g = color.x * -0.9689f + color.y * 1.8758f + color.z * 0.0415f;
        float b = color.x * 0.0557f + color.y * -0.2040f + color.z * 1.0570f;

        float R = LinearToStandard(r);
        float G = LinearToStandard(g);
        float B = LinearToStandard(b);

        return new Color(R, G, B);
    }

    private static float StandardToLinear(float c)
    {
        if (c <= 0.04045f)
        {
            return c / 12.92f;
        }
        else
        {
            return Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);
        }
    }

    private static float LinearToStandard(float c)
    {
        if (c <= 0.0031308f)
        {
            return c * 12.92f;
        }
        else
        {
            return 1.055f * Mathf.Pow(c, 0.4166667f) - 0.055f;
        }
    }
    #endregion
}

#region Public LerpMode Enum

public enum LerpMode { ShorterWay, LongerWay, Clockwise, CounterClockwise };

/*LerpMode is used as parameter in ColorPlus.LerpInLch. 
Because h component in Lch is angle, it can always be interpolated in two ways. (From 0 to 180 or from 360 to 180.)

ShorterWay		Always interpolates in the shorter way.
LongerWay		Always interpolates in the longer way.
Clockwise		Always interpolates clockwise.
CounterClockwise	Always interpolates counterclockwise.
*/
#endregion