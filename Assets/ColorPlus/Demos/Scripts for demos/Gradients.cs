using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Gradients : MonoBehaviour {
    public Color from, to;
    public Image[] firstRow, secondRow, thirdRow, fourthRow;
	
	// Update is called once per frame
	void Update () {
        for (int f = 0; f < firstRow.Length; f++)
        {
            float t = (float)f / (float)(firstRow.Length-1);

            firstRow[f].color = Color.Lerp(from, to, t);
            secondRow[f].color = ColorPlus.LerpInLab(from, to, t);
            thirdRow[f].color = ColorPlus.LerpInLch(from, to, t, LerpMode.ShorterWay);
            fourthRow[f].color = ColorPlus.LerpInLch(from, to, t, LerpMode.LongerWay);
        }
	}
}
