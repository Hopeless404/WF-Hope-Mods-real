﻿using Deadpan.Enums.Engine.Components.Modding;
using FMODUnity;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;
using UnityExplorer.UI;
using static GameUpdateDisplayer;
using static InjurySystem;
using static System.Net.Mime.MediaTypeNames;

namespace WildfrostHopeMod.CommandsConsole
{
    public partial class ConsoleMod : WildfrostMod
    {
        public static ConsoleMod Mod;
        public ConsoleMod(string modDirectory) : base(modDirectory)
        {
            Mod = this;
        }
        public override string GUID => "hope.wildfrost.console";
        public override string[] Depends => new string[] { };
        public string[] Tags => new string[] { "Tools" };
        public override string Title => "Another Console mod";
        public override string Description => "Access to the commands console & extra commands (use ~ or F12 to open it).\r\nMake sure Unity Explorer is hidden before typing\r\n\r\nExtra commands: (See \"help\" if ingame)\r\n[*][b]inspect, inspect this[/b]: If Unity Explorer is loaded, opens the inspector for a DataFile object. \"this\" refers to a hovered/inspected card/charm.\r\n[*][b]trigger, trigger all[/b]: Trigger cards on board\r\n[*][b]add effect[/b]: Apply ANY effect in the game (aka the better add status)\r\n[*][b]add attackeffect[/b]: Give cards the \"Apply <effect>\" effect. Doesn't accept StatusEffectApply statuses to avoid confusion.\r\n[*][b]destroy[/b]: Remove cards from the deck or from existence (in battle)\r\n[*][b]export card <name?>, export all[/b]: Screenshot (the hovered/named) card against a blank background\r\n[*][b]goto <scene>[/b]: Go to a scene. Currently limited to \"Town\" and \"MainMenu\".\r\n[*][b]quick restart[/b]: End the current campaign as if you died, and start a new one.\r\n\r\n[*][b]publish[/b]: Publish a local mod to the workshop, optionally with tags\r\n\r\nWIP:\r\n[*][b]databuilder of <datatype> <name>[/b]: Print a possible DataBuilder that recreates the given object. Currently works for CardDataBuilders\r\n[*][b]map replace <name>[/b]: Replace the hovered event node\r\n[*][b]custom effect summon <name>[/b]: Add a \"Summon (Named Card)\" effect to the hovered card";

        public static GameObject behaviour = null;

        public static DataFile hover = null;

        [ConfigItem(true, "Pressing up or down keys when there are multiple autofill options will now scroll through them, instead of scrolling through the command log. Hold to scroll very fast!", "Scrollable commands")]
        public bool scrollConfig;
        [ConfigItem(true, "", "Card/charm title autocomplete")]
        public bool autocompleteConfig;

        [ConfigItem(nameof(KeyCode.F12), "(Besides Tilde (~) or BackQuote (`))\n//\n//Refer to docs.unity3d.com/ ScriptReference/ KeyCode.html", "Toggle KeyCode")]
        public string toggleConfig = "";
        public KeyCode toggleKey
        {
            get
            {
                if (typeof(KeyCode).GetEnumNames().Any(n => string.Equals(n, toggleConfig, StringComparison.CurrentCultureIgnoreCase)))
                    return (KeyCode)TypeDescriptor.GetConverter(typeof(KeyCode)).ConvertFromInvariantString(toggleConfig);
                return default;
            }
        }

        // Specifically the last update of the changelog
        public const int lastUpdate = 240629;
        public const string changelog = """
            <#ff0>Header</color>
            """;

        static GameUpdateDisplayer d = null;
        public static bool instantiated = false;
        public override void Load()
        {
            base.Load();
            CoroutineManager.Start(Instantiate());
            Events.OnModLoaded += PatchToggleCloseUE.RetoggleConsoleAfterUELoaded;

            // Fixes for inspect command
            Events.OnInspect += Hover;
            Events.OnInspectEnd += UnHover;
            Events.OnEntityUnHover += UnHover;
            Events.OnUpgradeHover += HoverUpgrade;

            // To show ActionQueue displayer
            Patches.PatchTrackFpsDrawer.active = Settings.Load<bool>("ShowFps", false);

            // Fixes for reroll command
            Events.OnCampaignGenerated += PatchCampaignDataWithEvents.SaveCampaignDatas;
            Events.OnCampaignLoaded += PatchCampaignDataWithEvents.LoadCampaignDatas;
        }
        public override void Unload()
        {
            base.Unload();
            CoroutineManager.Start(SceneManager.Unload("Console"));
            Events.OnModLoaded -= PatchToggleCloseUE.RetoggleConsoleAfterUELoaded;
            Events.OnInspect -= Hover;
            Events.OnInspectEnd -= UnHover;
            Events.OnEntityUnHover -= UnHover;
            Events.OnUpgradeHover -= HoverUpgrade;
            Events.OnCampaignGenerated -= PatchCampaignDataWithEvents.SaveCampaignDatas;
            Events.OnCampaignLoaded -= PatchCampaignDataWithEvents.LoadCampaignDatas;
        }
        public static void HoverUpgrade(UpgradeDisplay display) => ConsoleMod.hover = display.data;
        public static void Hover(Entity entity) => Console.hover = entity;
        public static void UnHover(Entity entity) => Console.hover = null;
        


        static IEnumerator Instantiate()
        {
            yield return SceneManager.Load("Console", SceneType.Persistent);
            yield return new WaitUntil(() => Console.instance != null);
            Console.instance.savedCommands = [.. Console.instance.savedCommands.Select(s => s = s.IsNullOrWhitespace() ? "" : s)];
            Console.instance.argsDisplay.commands = [Console.commands.FirstOrDefault()];
            Console.instance.argsDisplay.current = ["help"];
            Console.instance.canvas.sortingOrder = 1;
            Console.instance.input.gameObject.GetOrAdd<InputFieldKeepFocus>();

            Console.instance.toggle = Console.instance.toggle.Without(KeyCode.F12);
            KeyCode configKey = TypeDescriptor.GetConverter(typeof(KeyCode)).ConvertFromInvariantString(Mod.toggleConfig) as KeyCode? ?? default;
            if (configKey != default)
                Console.instance.toggle = Console.instance.toggle.With(configKey);


            PatchTitleAsPrediction.Run();

            yield return AddHelpText();

            d ??= Resources.FindObjectsOfTypeAll<GameUpdateDisplayer>().FirstOrDefault();
            behaviour ??= new GameObject("AC-Update Displayer");
            GameObject.DontDestroyOnLoad(behaviour);
            behaviour.hideFlags = HideFlags.HideInHierarchy | HideFlags.DontUnloadUnusedAsset |
                                  HideFlags.HideInInspector | HideFlags.NotEditable;

            behaviour.AddComponent<UpdateDisplayer>();
            instantiated = true;

            yield return new WaitUntil(() => SceneManager.IsLoaded("MainMenu"));
        }

        static IEnumerator AddHelpText()
        {
            yield return new WaitUntil(() => SceneManager.IsLoaded("Console"));
            var otherHelpWindow = Console.instance.helpWindow.transform.FindRecursive("SavingCommands");
            //Debug.Log(otherHelpWindow.GetComponentInChildren<TextMeshProUGUI>().text);
            otherHelpWindow.GetComponentInChildren<TextMeshProUGUI>().text += '\n' + """
                    <#ff0><size=48>Combining Multiple Commands:</size></color>
                    Run multiple commands in one 
                    by separating them with a<#ff0> ; </color>(semicolon).

                    e.g. "gain blings 1; repeat 10" will give 1+10 blings.

                    <#ff0><size=48>Using Predicted Arguments:</size></color>
                    Press <#ff0>Tab</color> to autofill the predicted argument.

                    The <rotate=90><sprite name=knockback></rotate> <#ff0>Up</color> and <rotate=-90><sprite name=knockback></rotate> <#ff0>Down</color> arrow keys will
                    scroll the predictions if there's multiple.
                    
                    Otherwise, it will scroll the command log.
                    """;
        }

        

    }
}