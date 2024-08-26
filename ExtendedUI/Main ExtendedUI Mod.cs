using Deadpan.Enums.Engine.Components.Modding;
using ExtendedUI.Helpers;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace ExtendedUI
{
    public static class GameModeHelper
    {
        public static GameModifierDataBuilder AsHardModeModifier(
            this GameModifierDataBuilder modifierData,
            int stormPoints)
        {
            var hardModeModifier = ScriptableObject.CreateInstance<HardModeModifierData>();
            hardModeModifier.name = modifierData._data.name;
            modifierData = modifierData.WithLinkedStormBell(hardModeModifier);

            return modifierData.SubscribeToBuildEvent(data =>
            {
                hardModeModifier.modifierData = modifierData._data;
                hardModeModifier.stormPoints = stormPoints;
                hardModeModifier.unlockedByDefault = true;

                References.instance.hardModeModifiers = References.instance.hardModeModifiers.With(hardModeModifier);
            });
        }
    }

    public partial class ExtendedUIMod : WildfrostMod
    {
        public static ExtendedUIMod instance;
        public ExtendedUIMod(string modDirectory) : base(modDirectory)
        {
            instance = this;
        }
        public override string GUID => "hope.wildfrost.extendedui";
        public override string[] Depends => new string[] { };
        public override string Title => "Extended UI";
        public override string Description => "Mod intended to add more/extendible UI. Currently makes the storm bells scrollable (for Minus Ascension mod), and adds a class UIFactory to help with certain UI elements."
            + "\r\n\r\nCurrently the UIFactory allows modders to create card grids as well as a way to populate them, add scroll view (like the Mod Configs), add scrollers (like most ingame scroll) ((haven't added scrollbar yet though))"
            + "\r\n\r\nI'll tackle making card lanes and card slots later. I'll also soon include the fixes for making the tribe flags scrollable as well as card grids";
        public override TMP_SpriteAsset SpriteAsset => base.SpriteAsset;
        public static GameObject behaviour;

        public override void Load()
        {
            /*if (!assets.Any())
                for (int i = 0; i < 30; i++)
                //foreach (var node in AddressableLoader.GetGroup<CampaignNodeType>("CampaignNodeType").Where(n => n?.mapNodePrefab?.spriteOptions?.Any() ?? false))
                {
                    assets.Add(new GameModifierDataBuilder(this)
                        //.Create(node.name)
                        .Create(i.ToString())
                        .WithBellSprite("Nothing.png")
                        .WithDingerSprite("Nothing.png")
                        .WithTitle($"<{i}> Bell")
                        .WithDescription($"Adds a <{i}> node to each tier")
                        .WithVisible()
                        .WithValue(+25)
                        .WithSetupScripts() // mandatory
                        .WithSystemsToAdd() // mandatory
                        .WithStartScripts(*//*new Func<Script[]>(() =>
                        {
                            Script[] scripts = new Script[0];
                            for (int i = 0; i < 7; i++)
                            {
                                var script = ScriptableObject.CreateInstance<ScriptAddCampaignNodes>();
                                script.events = new ScriptAddCampaignNodes.Event[]
                                {
                                new ScriptAddCampaignNodes.Event()
                                {
                                    tierRange = new Vector2Int(i,i),
                                    type = node
                                }
                                };
                                scripts = scripts.With(script);
                            }
                            return scripts;
                        })()*//*)
                        .WithRingSfxEvent(FMODUnity.RuntimeManager.PathToEventReference("event:/sfx/town/stormbell_activate"))
                        .AsHardModeModifier(10)
                        );
                }*/
            base.Load();

            CardLane grid;
            if (GameObject.Find("Canvas/Safe Area/Menu") && (grid = UIFactory.CreateCardLane(GameObject.Find("Canvas/Safe Area/Menu").transform.parent)))
            {
                var array = AddressableLoader.GetGroup<ClassData>("ClassData").SelectMany(c => c.rewardPools.SelectMany(p => p.list)).ToArray();
                CoroutineManager.Start(grid.Populate(array.RandomItems(10).ToArrayOfNames()));
            }

            behaviour = new GameObject(Title);
            GameObject.DontDestroyOnLoad(behaviour);
            behaviour.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontUnloadUnusedAsset |
                                  HideFlags.HideInInspector | HideFlags.NotEditable;

            var e = behaviour.AddComponent<ExtendedUIModBehaviour>();
        }

        public override void Unload()
        {
            //References.instance.hardModeModifiers = References.instance.hardModeModifiers.Where(h => !h.modifierData.name.StartsWith(GUID)).ToArray();
            base.Unload();
            GameObject.Destroy(behaviour);
            behaviour = null;
        }
    }
}