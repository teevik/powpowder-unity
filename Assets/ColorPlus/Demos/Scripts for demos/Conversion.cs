using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Conversion : MonoBehaviour
{
    public Color rgb;
    public Lab lab;
    public Lch lch;
    [HideInInspector]
    public Slider R, G, B, L, A, B2, L2, C, H;
    [HideInInspector]
    public Text rText, gText, bText, lText, aText, b2Text, l2Text, cText, hText;
    [HideInInspector]
    public Image rgbImage, labImage, lchImage;

    // Update is called once per frame
    void Update()
    {
        rText.text = " R: " + Mathf.RoundToInt(rgb.r * 255);
        gText.text = " G: " + Mathf.RoundToInt(rgb.g * 255);
        bText.text = " B: " + Mathf.RoundToInt(rgb.b * 255);

        lText.text = " L: " + Mathf.RoundToInt(lab.l);
        aText.text = " A: " + Mathf.RoundToInt(lab.a);
        b2Text.text = " B: " + Mathf.RoundToInt(lab.b);

        l2Text.text = " L: " + Mathf.RoundToInt(lch.l);
        cText.text = " C: " + Mathf.RoundToInt(lch.c);
        hText.text = " H: " + Mathf.RoundToInt(lch.h);

        if (isColorVisible(rgb))
        {
            rgbImage.transform.GetChild(0).gameObject.SetActive(false);
            labImage.transform.GetChild(0).gameObject.SetActive(false);
            lchImage.transform.GetChild(0).gameObject.SetActive(false);

            rgbImage.color = rgb;
            labImage.color = ColorPlus.ToRgb(lab);
            lchImage.color = ColorPlus.ToRgb(lch);
        }
        else
        {
            rgbImage.transform.GetChild(0).gameObject.SetActive(true);
            labImage.transform.GetChild(0).gameObject.SetActive(true);
            lchImage.transform.GetChild(0).gameObject.SetActive(true);

            rgbImage.color = Color.black;
            labImage.color = Color.black;
            lchImage.color = Color.black;
        }
    }

    private void SetRgb()
    {
        lab = ColorPlus.ToLab(rgb);
        lch = ColorPlus.ToLch(rgb);

        SetLabSliders();
        SetLchSliders();

    }
    private void SetLab()
    {
        rgb = ColorPlus.ToRgb(lab);
        lch = ColorPlus.ToLch(lab);

        SetRgbSliders();
        SetLchSliders();
    }
    private void SetLch()
    {
        rgb = ColorPlus.ToRgb(lch);
        lab = ColorPlus.ToLab(lch);

        SetRgbSliders();
        SetLabSliders();
    }
    private void SetRgbSliders()
    {
        R.interactable = false;
        G.interactable = false;
        B.interactable = false;

        R.value = rgb.r * 255;
        G.value = rgb.g * 255;
        B.value = rgb.b * 255;

        R.interactable = true;
        G.interactable = true;
        B.interactable = true;
    }

    private void SetLabSliders()
    {
        L.interactable = false;
        A.interactable = false;
        B2.interactable = false;

        L.value = lab.l;
        A.value = lab.a;
        B2.value = lab.b;

        L.interactable = true;
        A.interactable = true;
        B2.interactable = true;
    }
    private void SetLchSliders()
    {
        L2.interactable = false;
        C.interactable = false;
        H.interactable = false;

        L2.value = lch.l;
        C.value = lch.c;
        H.value = lch.h;

        L2.interactable = true;
        C.interactable = true;
        H.interactable = true;
    }

    public void OnRChange(Slider s)
    {
        if (s.IsInteractable())
        {
            rgb.r = (float)s.value / 255f;
            SetRgb();
        }
    }
    public void OnGChange(Slider s)
    {
        if (s.IsInteractable())
        {
            rgb.g = (float)s.value / 255f;
            SetRgb();
        }
    }
    public void OnBChange(Slider s)
    {
        if (s.IsInteractable())
        {
            rgb.b = (float)s.value / 255f;
            SetRgb();
        }
    }
    public void OnLChange(Slider s)
    {
        if (s.IsInteractable())
        {
            lab.l = (float)s.value;
            SetLab();
        }
    }
    public void OnAChange(Slider s)
    {
        if (s.IsInteractable())
        {
            lab.a = (float)s.value;
            SetLab();
        }
    }
    public void OnB2Change(Slider s)
    {
        if (s.IsInteractable())
        {
            lab.b = (float)s.value;
            SetLab();
        }
    }
    public void OnL2Change(Slider s)
    {
        if (s.IsInteractable())
        {
            lch.l = (float)s.value;
            SetLch();
        }
    }
    public void OnCChange(Slider s)
    {
        if (s.IsInteractable())
        {
            lch.c = (float)s.value;
            SetLch();
        }
    }
    public void OnHChange(Slider s)
    {
        if (s.IsInteractable())
        {
            lch.h = (float)s.value;
            SetLch();
        }
    }
    private static bool isColorVisible(Color c)
    {
        if (c.r < 0 || c.r > 1 || c.g < 0 || c.g > 1 || c.b < 0 || c.b > 1)
            return false;
        else
            return true;
    }

}
