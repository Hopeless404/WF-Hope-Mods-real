using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using Deadpan.Enums.Engine.Components.Modding;
using UnityEngine.Events;
using static WildfrostHopeMod.ConfigManager.PatchJournal;
using TMPro;
using UnityEngine.Localization.Components;
using static AvatarPart;
using static WildfrostHopeMod.ConfigManager;

namespace WildfrostHopeMod.Configs
{
    public class ConfigSection
    {
        public readonly WildfrostMod mod;
        public string title;
        public JournalPage section { get; private set; }
        public Dictionary<string, ConfigItem> items = new();

        public event UnityAction<ConfigItem, object> OnConfigChanged;
        public void InvokeConfigChanged(ConfigItem item, object value)
        {
            if (initialised)
                OnConfigChanged?.Invoke(item, value);
        }

        public ConfigSection(WildfrostMod mod)
        {
            this.mod = mod;
            this.title = mod.Title;
        }
        public ConfigSection UpdateInfo()
        {
            if (!PatchErrors.initialised)
                return this;

            this.section = CreateSection(mod);
            CreateTitle(section.transform, title);
            UpdateConfigs();

            items.Values.Update(ConfigItem.FixHeight);
            return this;
        }
        JournalPage CreateSection(WildfrostMod mod)
        {
            if (modSettingsPage.transform.FindRecursive(mod.GUID))
                return modSettingsPage.transform.FindRecursive(mod.GUID).GetComponent<JournalPage>();
            var section = templatePage.InstantiateKeepName();
            section.name = mod.GUID;
            section.transform.Normalise();
            section.transform.DestroyAllChildren();
            section.GetComponent<VerticalLayoutGroup>().spacing = 0;
            section.gameObject.SetActive(true);
            section.transform.SetParent(modContent);
            section.GetComponent<UINavigationLayer>().Destroy();
            //section.GetComponent<JournalPageMenu>().Destroy();
            section.GetComponent<BackButtonGamePadController>().Destroy();

            ConfigManager.OnModLoaded += OnModLoad;
            ConfigManager.OnModUnloaded += OnModUnload;
            return section;
        }
        public static void CreateTitle(Transform parent, string text)
        {
            if (parent.Find("Title")) return;

            var title = templateTitle.InstantiateKeepName();
            title.transform.SetParent(parent);
            title.transform.SetAsFirstSibling();
            title.transform.rotation = templateTitle.transform.rotation;
            title.transform.localScale = templateTitle.transform.localScale;

            var titleText = title.GetComponentInChildren<TextMeshProUGUI>();
            title.GetComponentsInChildren<LocalizeStringEvent>().Update(e => e.enabled = false);
            title.GetComponentsInChildren<FontSetter>().Update(e => e.enabled = false);
            titleText.text = text;
            titleText.maskable = true;

            titleText.enableWordWrapping = true;
            LayoutElement layout = title.AddComponent<LayoutElement>();
            layout.layoutPriority = 1;
            layout.preferredWidth = 6;
        }
        void CreateSpace(JournalPage section)
        {
            Transform space = section.transform.Find("Space") ?? templateSpace.InstantiateKeepName();
            space.SetParent(section.transform);
            space.SetAsLastSibling();
        }
        public void UpdateConfigs()
        {
            foreach (var con in mod.GetConfigs())
            {
                ConfigManager.conToMod[con] = mod;
                items.TryGetValue(con.field.Name, out ConfigItem configItem);
                items[con.field.Name] = configItem ??= new ConfigItem(this, con);
            }
            CreateSpace(section);
        }
        internal void FixHeightAll() => items.Values.Update(ConfigItem.FixHeight);
        internal void FixLayout() => section?.transform.Normalise();
        void OnModLoad(WildfrostMod mod)
        {
            if (mod != this.mod)
                return;
            ConfigManager.sections[mod].section.gameObject.SetActive(items.Any(i => i.Value.visible));
        }
        void OnModUnload(WildfrostMod mod)
        {
            if (mod != this.mod)
                return; 
            ConfigManager.sections[mod].section.gameObject.SetActive(false);
        }
        public static ConfigItem? TryGetItem(WildfrostMod mod, string fieldName)
        {
            if (!(ConfigManager.sections.ContainsKey(mod) && ConfigManager.sections[mod].items.ContainsKey(fieldName)))
                return null;
            return ConfigManager.sections[mod].items[fieldName];
        }
    }

}
