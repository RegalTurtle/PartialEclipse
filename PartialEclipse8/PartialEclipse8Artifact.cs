using IL.RoR2.Projectile;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Networking;

public class PartialEclipse8Artifact
{
    public static ArtifactDef artifact;

    private static global::System.Resources.ResourceManager resourceMan;
    private static global::System.Globalization.CultureInfo resourceCulture;

    public PartialEclipse8Artifact()
    {
        LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE8_NAME", "Artifact of Partial Eclipse");
        LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE8_DESC", "Applies Eclipse 8 for people who select the artifact.");

        artifact = ScriptableObject.CreateInstance<ArtifactDef>();
        artifact.cachedName = "PartialEclipse8";
        artifact.nameToken = "PARTIALECLIPSE_PARTIALECLIPSE8_NAME";
        artifact.descriptionToken = "PARTIALECLIPSE_PARTIALECLIPSE8_DESC";
        //artifact.smallIconSelectedSprite = CreateSprite(null, Color.magenta);
        //artifact.smallIconDeselectedSprite = CreateSprite(null, Color.gray);
        artifact.smallIconSelectedSprite = CreateSpriteNew("C:\\Users\\thysv\\source\\repos\\PartialEclipse\\PartialEclipse8\\selected.png");
        artifact.smallIconDeselectedSprite = CreateSpriteNew("C:\\Users\\thysv\\source\\repos\\PartialEclipse\\PartialEclipse8\\deselected.png");
        ContentAddition.AddArtifactDef(artifact);
    }

    public static Sprite CreateSpriteNew(String fileName)
    {
        //Texture2D img = Resources.Load(fileName) as Texture2D;
        byte[] resBytes = (byte[])

        //String deselected = @"~\deselected.png";
        //Debug.Log(deselected);
        //Texture2D img2 = LoadPNG(fileName);
        //Debug.Log(System.IO.Path.);
        //Texture2D img3 = Resources.Load("Assets/selected") as Texture2D;
        //Debug.Log(img3 != null);
        //byte[] resBytes = img.GetRawTextureData();
        //Image i = Image.FromFile(fileName);

        //var myIcon = Resources.selected;

        //Image i = null;

        byte[] byteArray = ((byte[])(obj));

        Texture2D imgFinal = new Texture2D(2, 2);
        imgFinal.LoadImage(byteArray);

        //Debug.Log(img2 == null);

        //var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        //Chat.AddMessage("2");
        //tex.LoadImage(resBytes, false);
        //tex.Apply();
        //CleanAlpha(tex);
        //Chat.AddMessage("3");

        //return CreateSprite(null, Color.magenta);
        return Sprite.Create(imgFinal, new Rect(0, 0, imgFinal.width, imgFinal.height), new Vector2(64, 64));
    }

    private static Texture2D LoadPNG(string filePath)
    {

        Texture2D tex = null;
        tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        byte[] fileData;
        //FillTexture(tex, Color.cyan);

        if (System.IO.File.Exists(filePath))
        {
            fileData = System.IO.File.ReadAllBytes(filePath);
            tex = new Texture2D(2, 2);
            tex.LoadImage(fileData); //..this will auto-resize the texture dimensions.
            Debug.Log(fileData.ToString());
        }
        return tex;
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

        //return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(31, 31));

        try
        {
            //byte[] resBytes = (byte[])Resources.ResourceManager.GetObject(resName);
            //tex.LoadImage(resBytes, false);
            tex.Apply();

            //sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(64, 64));
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }
        return null;
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

    public static Sprite LoadResourceSprite(string resName)
    {
        Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        Sprite sprite = null;

        try
        {
            //byte[] resBytes = (byte[])Properties.Resources.ResourceManager.GetObject(resName);
            //tex.LoadImage(resBytes, false);
            tex.Apply();

            sprite = Sprite.Create(tex, new Rect(0, 0, 128, 128), new Vector2(64, 64));
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
        }

        return sprite;
    }
}
