using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using WildfrostHopeMod.Utils;

namespace WildfrostHopeMod.VFX
{
    public static class SpriteAssetExt
    {
        public static void RegisterSpriteAsset(this TMP_SpriteAsset spriteAsset) =>
            TMP_Settings.defaultSpriteAsset.fallbackSpriteAssets.Add(spriteAsset);
    }
    public class SpriteAssetGenerator
    {
        public TMP_SpriteAsset spriteAsset;
        public SpriteAssetGenerator(string name, string directoryWithPNGs = null, Texture2D[] textures = default, Sprite[] sprites = null)
        {
            spriteAsset = HopeUtils.CreateSpriteAsset(name, directoryWithPNGs, textures, sprites);
        }
    }
}
