using R2API;
using RoR2;
using System;
using UnityEngine;

namespace PartialEclipse
{
    internal class PartialEclipse2Artifact
    {
        public static ArtifactDef artifact;

        public PartialEclipse2Artifact()
        {
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE2_NAME", "Artifact of Partial Eclipse 2");
            LanguageAPI.Add("PARTIALECLIPSE_PARTIALECLIPSE2_DESC", "Applies Eclipse 2 for people who select the artifact.");

            artifact = ScriptableObject.CreateInstance<ArtifactDef>();
            artifact.cachedName = "PartialEclipse2";
            artifact.nameToken = "PARTIALECLIPSE_PARTIALECLIPSE2_NAME";
            artifact.descriptionToken = "PARTIALECLIPSE_PARTIALECLIPSE2_DESC";
            artifact.smallIconSelectedSprite = CreateSpriteNew("E2_selected");
            artifact.smallIconDeselectedSprite = CreateSpriteNew("E2_deselected");
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
