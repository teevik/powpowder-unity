using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Lab //Representation of CIELab colors with three components (l, a, b). To learn more about Lab Color Space visit https://en.wikipedia.org/wiki/Lab_color_space
{
    public float l;
    public float a;
    public float b;

    public Lab(float l, float a, float b)
    {
        this.l = l;
        this.a = a;
        this.b = b;
    }
    public override string ToString()
    {
        return ("L = " + l + ", A = " + a + ", B = " + b);
    }
}