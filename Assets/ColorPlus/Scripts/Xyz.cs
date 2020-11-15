using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Xyz //Xyz color space is useful only for conversion between Rgb and Lab. 
{
    public float x;
    public float y;
    public float z;

    public Xyz(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public override string ToString()
    {
        return ("X = " + x + ", Y = " + y + ", Z = " + z);
    }
}