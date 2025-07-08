using R2API;
using RoR2;
using System;
using UnityEngine;

namespace PartialEclipse
{
    internal class PartialEclipse1Artifact
    {
        public static ArtifactDef artifact;

        public PartialEclipse1Artifact()
        {
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE1_NAME", "Artifact of Partial Eclipse 1");
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE1_DESC", "Applies Eclipse 1 for people who select the artifact.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "PartialEclipse1";
            artifact.nameToken = "PARTIALECLIPSE_PARTIALECLIPSE1_NAME";
            artifact.descriptionToken = "PARTIALECLIPSE_PARTIALECLIPSE1_DESC";
            artifact.smallIconSelectedSprite = CreateSpriteNew("E1_selected");
            artifact.smallIconDeselectedSprite = CreateSpriteNew("E1_deselected");
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
