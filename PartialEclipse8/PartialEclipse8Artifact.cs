using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;


public class PartialEclipse8Artifact
{
    public static ArtifactDef artifact;

    public PartialEclipse8Artifact()
    {
        LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE8_NAME", "Artifact of Partial Eclipse");
        LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE8_DESC", "Applies Eclipse 8 for people who select the artifact.");

        artifact = ScriptableObject.CreateInstance<ArtifactDef>();
        artifact.cachedName = "PartialEclipse8";
        artifact.nameToken = "PARTIALECLIPSE_PARTIALECLIPSE8_NAME";
        artifact.descriptionToken = "PARTIALECLIPSE_PARTIALECLIPSE8_DESC";
        artifact.smallIconSelectedSprite = CreateSprite(null, Color.magenta);
        artifact.smallIconDeselectedSprite = CreateSprite(null, Color.gray);
        ContentAddition.AddArtifactDef(artifact);
    }

    public static Sprite CreateSprite(byte[] resourceBytes, Color fallbackColor)
    {
        // Create a temporary texture, then load the texture onto it.
        var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        try
        {
            if (resourceBytes == null)
            {
                FillTexture(tex, fallbackColor);
            }
            else
            {
                tex.LoadImage(resourceBytes, false);
                tex.Apply();
                CleanAlpha(tex);
            }
        }
        catch (Exception e)
        {
            FillTexture(tex, fallbackColor);
        }

        return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(31, 31));
    }

    private static Texture2D FillTexture(Texture2D tex, Color color)
    {
        var pixels = tex.GetPixels();
        for (var i = 0; i < pixels.Length; ++i)
        {
            pixels[i] = color;
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return tex;
    }

    private static Texture2D CleanAlpha(Texture2D tex)
    {
        var pixels = tex.GetPixels();
        for (var i = 0; i < pixels.Length; ++i)
        {
            if (pixels[i].a < 0.05f)
            {
                pixels[i] = Color.clear;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return tex;
    }
}
