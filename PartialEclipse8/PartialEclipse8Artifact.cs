using R2API;
using RoR2;
using System;
using UnityEngine;

namespace PartialEclipse8
{
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
            artifact.smallIconSelectedSprite = CreateSpriteNew("selected");
            artifact.smallIconDeselectedSprite = CreateSpriteNew("deselected");
            ContentAddition.AddArtifactDef(artifact);
        }

        public static Sprite CreateSpriteNew(String fileName)
        {
            return LoadResourceSprite(fileName);
        }

        public static Sprite LoadResourceSprite(string resName)
        {
            Texture2D tex = new Texture2D(128, 128, TextureFormat.RGBA32, false);
            Sprite sprite = null;

            try
            {
                byte[] resBytes = (byte[])Properties.Resources.ResourceManager.GetObject(resName);
                tex.LoadImage(resBytes, false);
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
}