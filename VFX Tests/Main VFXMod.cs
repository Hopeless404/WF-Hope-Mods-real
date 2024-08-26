using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using WildfrostHopeMod.Text;
using FMODUnity;
using WildfrostHopeMod.SFX;

namespace WildfrostHopeMod.VFX
{
    public class VFXMod : WildfrostMod
    {
        public static VFXMod Mod;

        public VFXMod(string modDirectory) : base(modDirectory)
        {
            Mod = this;
        }

        public override string GUID => "hope.wildfrost.vfx";
        public override string[] Depends => new string[] { };
        public override string Title => "VFX/SFX Tools";
        public override string Description => "A collection of hopefully helpful VFX/SFX tools for mods.\r\nYou can also turn on a funny on-death animation in configs.\r\n\r\n\r\n\r\nThe main ones are:\r\n[list]\r\n[*][b]Typewriter[/b]: Using the CardScriptAddComponentTypewriter class to add a TypewriterController, you can make its description appear/disappear as if it were talking (think Inscryption or the Balatro guy)\r\n[*][b]GIF Loader[/b]: Import .gif files into the game to use as apply effects, or damage effects. The importing is somewhat slow so don't overuse it (DM me if you can find a way to use import async)\r\n[*][b]SFX Loader[/b]: Import and play music files in the game. You can use this with the Typewriter for talking sounds (like Sans Undertale!?!?)\r\n[*][b]Sprite Asset Generator[/b]: This was honestly a last minute addition just to help people make SpriteAssets and register them.\r\n[/list]\r\n\r\nEach of these come with a [url=https://steamcommunity.com/sharedfiles/filedetails/?id=3154932293]Console Command[/url] to test with, those being \"talk\", \"create vfx\" and \"create sfx\". These last two also show base game ones.\r\n\r\nI intended to add a way to replace the ScriptableCardImage but it's a bit dumb. Maybe a simple example implementation in the future?";

        public static GameObject behaviour;

        public GIFLoader VFX;
        public SFXLoader SFX;

        [ConfigItem(false, null, "peepovanish")]
        public bool PeepoVanish;

        public GifCutsceneRenderer gifCutscene;

        // CAN USE GAMESYSTEM.SETSORTINGLAYER("INSPECT", 1)
        // This is used for the CinemaBarSystem

        // ALWAYS REMEMBER TO DO "DONTDESTROYONLOAD" FOR EVERY NEW GAMEOBJECT
        public override void Load()
        {
            Events.OnEntityKilled += OnEntityKilled;
            if (!Directory.Exists(ImagesDirectory))
                Directory.CreateDirectory(ImagesDirectory);
            VFX = new GIFLoader(ImagesDirectory, GIFLoader.PlayType.applyEffect, true, false);
            VFX.RegisterAllAsApplyEffect();

            SFX = new SFXLoader(ImagesDirectory);
            SFX.RegisterAllSoundsToGlobal();

            base.Load(); 
            
            behaviour = new GameObject(Title);
            GameObject.DontDestroyOnLoad(behaviour);
            behaviour.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontUnloadUnusedAsset |
                                  HideFlags.HideInInspector | HideFlags.NotEditable;

            gifCutscene = behaviour.AddComponent<GifCutsceneRenderer>();
        }

        public void OnEntityKilled(Entity entity, DeathType death)
        {
            if (death == DeathType.Sacrifice) return;
            if (PeepoVanish)
            {
                var transform = entity.transform;
                //Debug.Log(entity.data.title + death.ToString() + PeepoVanish);
                VFX.TryPlayEffect("vanish", transform.position, transform.lossyScale);
                SFX.TryPlaySound("sansfx");
            }
        }

        public override void Unload()
        {
            base.Unload();
            Events.OnEntityKilled -= OnEntityKilled;
        }
    }
}