using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Lch //Representation of CIELCh colors with three components (l, c, h). 
{
    public float l;
    public float c;
    public float h;

    public Lch(float l, float c, float h)
    {
        this.l = l;
        this.c = c;
        this.h = h;
    }
    public override string ToString()
    {
        return ("L = " + l + ", C = " + c + ", H = " + h);
    }
}