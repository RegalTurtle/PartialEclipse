using R2API;
using RoR2;
using System;
using UnityEngine;

namespace PartialEclipse
{
    internal class PartialEclipse5Artifact
    {
        public static ArtifactDef artifact;

        public PartialEclipse5Artifact()
        {
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE5_NAME", "Artifact of Partial Eclipse 5");
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE5_DESC", "Applies Eclipse 5 for people who select the artifact.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "PartialEclipse5";
            artifact.nameToken = "PARTIALECLIPSE_PARTIALECLIPSE5_NAME";
            artifact.descriptionToken = "PARTIALECLIPSE_PARTIALECLIPSE5_DESC";
            artifact.smallIconSelectedSprite = CreateSpriteNew("E5_selected");
            artifact.smallIconDeselectedSprite = CreateSpriteNew("E5_deselected");
            ContentAddition.AddArtifactDef(artifact);
        }

        public static Sprite CreateSpriteNew(string fileName)
        {
            return LoadResourceSprite(fileName);
        }

        public static Sprite LoadResourceSprite(string resName)
        {
            Texture2D tex = new(128, 128, TextureFormat.RGBA32, false);
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
