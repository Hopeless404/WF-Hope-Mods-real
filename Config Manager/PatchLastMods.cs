using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WildfrostHopeMod.Configs;

namespace WildfrostHopeMod
{
    public partial class ConfigManager
    {
        [HarmonyPatch(typeof(WildfrostMod), nameof(WildfrostMod.SetLastMods))]
        static class PatchLastMods
        {
            public static WildfrostMod[] mods;
            static void Postfix(WildfrostMod[] enabled)
            {
                if (PatchJournal.modSettingsPage)
                {
                    Enable(enabled.Except(mods));
                    Disable(mods.Except(enabled));
                    CoroutineManager.Start(PatchJournal.FixLayout());
                }
                mods = enabled;
            }

            static void Enable(IEnumerable<WildfrostMod> enabled)
            {
                foreach (var mod in enabled)
                {
                    bool flag = PatchJournal.CreateConfigs(mod) && sections[mod].section.gameObject.activeSelf;
                    OnModLoaded.Invoke(mod);
                    Debug.LogWarning($"Enabled {mod.GUID} config?: {flag}");
                }
            }
            static void Disable(IEnumerable<WildfrostMod> disabled)
            {
                foreach (var mod in disabled)
                {
                    OnModUnloaded.Invoke(mod);
                    Debug.LogWarning("Disabled " + mod.GUID + " config?: " + 
                        !sections[mod].section.gameObject.activeSelf);
                }
            }
        }

        public static void SaveConfigsToSettingsJSON(WildfrostMod mod)
        {
            Configs.ConfigSection section = GetConfigSection(mod);
            if (section == null)
            {
                Debug.LogError($"[Configs] Couldn't find the configs for [{mod.GUID}]");
                return;
            }

            foreach (var item in section.items)
            {
                var info = new ConfigInfo(item.Value);
                SaveToSettingsJson(Extensions.PrefixGUID(item.Key, mod), info, echo:false);
            }
        }
        public static void SaveToSettingsJson<T>(string key, T value, bool echo = true)
        {
            if (echo)
                Settings.Save(key, value);
            else
            {
                try
                {
                    ES3.Save<T>(key, value, Settings.settings);
                }
                catch (FormatException ex1)
                {
                    Debug.LogWarning((object)ex1);
                    ES3.DeleteFile(Settings.settings);
                    try
                    {
                        ES3.Save<T>(key, value, Settings.settings);
                    }
                    catch (Exception ex2)
                    {
                        Debug.LogError((object)string.Format("ES3 Failed to save Settings even after deleting file.\n{0}", (object)ex2));
                    }
                }
                Events.InvokeSettingChanged(key, (object)value);
            }
        }

        internal struct ConfigInfo
        {
            public string Title;
            public string Description;
            public object Value;

            public ConfigInfo(ConfigItem item)
            {
                Title = item.con.atr.Title;
                if (!item.con.atr.comment.IsNullOrEmpty())
                    Description = item.con.atr.comment;
                Value = item.currentValue;
            }
            /*public static bool operator ==(ConfigInfo one, ConfigInfo two)
            {
                return true;
            }
            public static bool operator !=(ConfigInfo one, ConfigInfo two)
            {
                return true;
            }
            public override bool Equals(object obj)
            {
                if (obj is ConfigInfo info)
                {
                    return Title == info.Title;
                }
                return false;
            }*/
            public override string ToString()
            {
                return Value.ToString();
            }
        }
    }

}