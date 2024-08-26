using Deadpan.Enums.Engine.Components.Modding;
using FMODUnity;
using HarmonyLib;
using Steamworks;
using Steamworks.Ugc;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;
using UnityEngine.UI;
using static UnityEngine.UI.Button;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace WildfrostHopeMod.ModUploader
{
    [HarmonyPatch(typeof(ModsSceneManager), nameof(ModsSceneManager.Start))]
    public class ModUploader : WildfrostMod
    {
        public ModUploader(string modDirectory) : base(modDirectory) { }
        public override string GUID => "hope.wildfrost.moduploader";
        public override string[] Depends => new string[] { };
        public override string Title => "Mod Uploader";
        public override string Description => "Override the default publish button with nicer UI. Makes it a bit easier to set/reuse tags and visibility";
        static bool shouldPatch = false; 
        public static readonly string[] tagsAll = [
                "Cards",
                "Bosses",
                "Pets",
                "QoL",
                "Tools",
                "Leaders",
                "Tribes",
                "Expansion",
                "Battles",
                "Balance",
                "English",
                "Simplified Chinese",
                "Traditional Chinese",
                "Korean",
                "Japanese",
                "French",
                "German",
                "Russian",
                "Spanish",
                "Portuguese",
                "Public",
                "FriendsOnly",
                "Private",
                "Unlisted"
            ];
        public static readonly string[] visibilityTags = [
                "Public",
                "FriendsOnly",
                "Private",
                "Unlisted"
            ];
        public override void Load()
        {
            shouldPatch = true;
            base.Load();
            if (SceneManager.IsLoaded("Mods"))
                CoroutineManager.Start(PatchPublishButtons(null, GameObject.FindObjectOfType<ModsSceneManager>(true)));
        }
        public override void Unload()
        {
            shouldPatch = false;
            base.Unload();
            if (SceneManager.IsLoaded("Mods"))
                CoroutineManager.Start(PatchPublishButtons(null, GameObject.FindObjectOfType<ModsSceneManager>(true)));
        }

        public static List<Item> PublishedItems = [];


        [HarmonyPostfix]
        public static IEnumerator PatchPublishButtons(IEnumerator __result, ModsSceneManager __instance)
        {
            yield return __result;

            foreach (var modTransform in __instance.Content.transform.GetAllChildren())
            {
                var modHolder = modTransform.GetComponent<ModHolder>(); 
                Button button = modHolder.PublishButton.GetComponentInChildren<ButtonAnimator>().button as Button;

                button.onClick = new ButtonClickedEvent();
                if (shouldPatch) button.onClick.AddListener(() => OnClick(modHolder));
                else button.onClick.AddListener(() => modHolder.DrawingUpload = true);
            }
        }
        public static async void OnClick(ModHolder modHolder)
        {
            WildfrostMod mod = modHolder.Mod; 
            string path = Path.Combine(mod.ModDirectory, "tags.txt");
            string[] tagsProvided = await GetTags(mod);
            string[] tagsCustom = [.. tagsProvided.Where(t => !tagsAll.Contains(t))];
            string[] tagsPrevious = (await GetTags(mod, fromWorkshop: true)).Where(s => !visibilityTags.Select(t => t.ToLower()).Contains(s.ToLower())).ToArray();

            var visibility = visibilityTags.FirstOrDefault(tag => tagsProvided.Select(t => t.ToLower()).Contains(tag.ToLower()));
            tagsProvided = tagsProvided.Where(s => !visibilityTags.Select(t => t.ToLower()).Contains(s.ToLower())).ToArray();

            var newTags = tagsProvided.Where(tag => tagsPrevious.All(t => t.ToLower() != tag.ToLower()));
            var oldTags = tagsPrevious.Where(tag => tagsProvided.All(t => t.ToLower() != tag.ToLower()));

            string desc = $"Publish mod [{mod.GUID}]?"
                + (tagsProvided.Any() ?     $"|... with tag(s) [{string.Join(", ", tagsProvided)}]?" : "|")
                + (visibility != "Public" ? $"\nwith visibility [{visibility}]" : "")
                + (newTags.Any() && tagsPrevious.Any(t => t != "Mod") ? 
                                            $"\nThis will add the tags [{string.Join(", ", newTags)}]" : "")
                + (oldTags.Any() ?          $"\nThis will remove the tags [{string.Join(", ", oldTags)}]" : "")
                + (tagsCustom.Any() ?       $"|Custom tags used: [{string.Join(", ", tagsCustom)}]" : "|Note: You can use any custom tags too!")
                +                           "\nSet non-public visibility with FriendsOnly, Private, or Unlisted"
                +                           "\n\"Edit tags\" will create/open the tags.txt in the mod folder";

            if (SceneManager.IsLoaded("Mods"))
                CoroutineManager.Start(SceneManager.Unload("Mods"));
            HelpPanelSystem.instance.OnDisable();
            HelpPanelSystem.Show(CreateString(desc));
            HelpPanelSystem.SetEmote(Prompt.Emote.Type.Basic);
            HelpPanelSystem.SetBackButtonActive(false);
            HelpPanelSystem.AddButton(HelpPanelSystem.ButtonType.Negative, CreateString("Cancel"), "Back", null);
            HelpPanelSystem.AddButton(HelpPanelSystem.ButtonType.Positive, CreateString("Edit tags"), "Options", () => 
                EditTags(path, tagsPrevious));
            HelpPanelSystem.AddButton(HelpPanelSystem.ButtonType.Positive, CreateString("Confirm"), "Select", () =>
                UpdateOrPublishToWorkshop(mod, tagsProvided, visibility)
            );
        }
        static string explanation = "[Delete this later] Write new tags on new lines";
        public static void EditTags(string path, string[] tagsPrevious)
        {
            GameUpdateDisplayer displayer = Resources.FindObjectsOfTypeAll<GameUpdateDisplayer>()
                                                     .First(g => g.gameObject.scene.name == "PauseScreen");
            var inputField = displayer.gameObject.GetOrAdd<TMP_InputField>();
            inputField.textComponent = displayer.scrollRect.content.Find("Body").GetComponent<TMP_Text>();
            inputField.textViewport = displayer.scrollRect.viewport;
            displayer.gameObject.SetActive(true);
            CoroutineManager.Start(displayer.ShowRoutine(new()
            {
                titleRef = CreateString("Title text"),
                bodyRef = CreateString("Body text"),
                panelHeight = 7
            }));
/*

            if (!File.Exists(path))
            {
                var file = File.CreateText(path);
                if (tagsPrevious.Length <= 1)
                    file.WriteLine(explanation);
                foreach (var tag in tagsPrevious)
                    file.WriteLine(tag);
                file.Close();
            }
            Application.OpenURL(path);
            CoroutineManager.Start(SceneManager.Load("Mods", SceneType.Temporary));*/
        }

        public static LocalizedString CreateString(string text)
        {
            StringTable collection = LocalizationHelper.GetCollection("UI Text", LocalizationSettings.Instance.GetSelectedLocale().Identifier);
            collection.SetString("hope." + text, text);
            return collection.GetString("hope." + text);
        }

        
        public static async Task<List<Item>> GetPublishedItems()
        {
            if (PublishedItems.Count <= 0 && SteamManager.init)
            {
                var ResultPage = await new Query(UgcType.Items, UserUGCList.Published, UserUGCListSortOrder.CreationOrderDesc, SteamClient.SteamId)
                    .WithMetadata(b: true).WithTag("Mod").GetPageAsync(1);
                PublishedItems = ResultPage.Value.Entries.ToList().FindAll((Item a) => a.Result != Result.FileNotFound);
            }
            return PublishedItems;
        }
        public static async Task<string[]> GetTags(WildfrostMod mod, bool fromWorkshop = false)
        {
            var tagsPath = Path.Combine(mod.ModDirectory, "tags.txt");
            string[] tags = File.Exists(tagsPath) ? File.ReadAllLines(tagsPath) : [];
            if (!File.Exists(tagsPath) || fromWorkshop)
            {
                var workshopItem = (await GetPublishedItems()).Find(item => item.Metadata == mod.GUID);
                if (!workshopItem.Equals(default(Item)))
                {
                    string visibility = "Public";
                    if (workshopItem.IsPrivate) visibility = "Private";
                    if (workshopItem.IsFriendsOnly) visibility = "FriendsOnly";
                    tags = workshopItem.Tags.With(visibility);
                }

            };
            var result = tags.Select(tag =>
            {
                var newTag = tagsAll.FirstOrDefault(t => t.ToLower() == tag.ToLower());
                return newTag == default ? tag : newTag;
            });
            if (!result.Intersect(["Public", "Private", "FriendsOnly", "Unlisted"]).Any())
                result = result.AddItem("Public");
            return result.Except(["Mod", "mod", explanation]).Distinct().ToArray();
        }
        protected static async void UpdateOrPublishToWorkshop(WildfrostMod mod, string[] tagsProvided, string visbility = "Public")
        {
            string iconPath = mod.IconPath;
            if (File.Exists(Path.Combine(mod.ModDirectory, "icon.gif")))
                iconPath = Path.Combine(mod.ModDirectory, "icon.gif");

            Item curItem = (await GetPublishedItems()).Find((Item a) => a.Metadata == mod.GUID);
            Editor editor;
            if (PublishedItems.Count == 0 || curItem.Equals(default(Item)))
                editor = Editor.NewCommunityFile;
            else
                editor = new Editor(curItem.Id);

            editor = editor.WithTitle(mod.Title);
            editor = editor.WithDescription(mod.Description);
            editor = editor.WithTag("Mod");
            foreach (var tag in tagsProvided)
                editor = editor.WithTag(tag);
            editor = editor.ForAppId(SteamClient.AppId);
            editor = visbility switch
            {
                "FriendsOnly" => editor.WithFriendsOnlyVisibility(),
                "Private" => editor.WithPrivateVisibility(),
                "Unlisted" => editor.WithUnlistedVisibility(),
                _ => editor.WithPublicVisibility()
            };
            editor = editor.WithPreviewFile(iconPath);
            editor = editor.WithContent(mod.ModDirectory);
            editor = editor.WithMetaData(mod.GUID);
            var result = await editor.SubmitAsync();
            Steamworks.Ugc.Item? async = await Steamworks.Ugc.Item.GetAsync(result.FileId);
            foreach (string depend2 in mod.Depends)
            {
                string depend = depend2;
                Steamworks.Ugc.Item obj = PublishedItems.Find((Predicate<Steamworks.Ugc.Item>)(a => a.Metadata == depend));
                if (!curItem.Equals(new Steamworks.Ugc.Item()) && async.HasValue)
                    async.GetValueOrDefault().AddDependency(obj.Id);
            }
            Debug.Log("Update result " + result.Result);
            if (result.Success)
            {
                PromptSystem.Create(Prompt.Anchor.Left, 0, 0, 5, Prompt.Emote.Type.Happy, Prompt.Emote.Position.Above);
                PromptSystem.instance.prompt.SetText(result.Result.ToString() + "!\nIt'll take a moment to update the mod page");

            }
            else
            {
                PromptSystem.Create(Prompt.Anchor.Left, 0, 0, 5, Prompt.Emote.Type.Scared, Prompt.Emote.Position.Above);
                PromptSystem.instance.prompt.SetText($"Uh oh...\nUpdate result: {result.Result}\n" + FailedPublishResultText(result.Result));
            }
            await Task.Delay(4000);
            PromptSystem.Hide();
            PublishedItems.Clear();
        }

        public static string FailedPublishResultText(Result value)
            => value switch
            {
                Result.Fail => "A more specific error code couldn't be determined",
                Result.NoConnection => "No/Failed network connection",
                Result.FileNotFound => "File was not found",
                Result.DuplicateName => "Name is not unique",
                Result.Timeout => "Operation timed out",
                Result.Banned => "You are VAC2 banned",
                Result.ServiceUnavailable => "The requested service is temporarily unavailable",
                Result.Pending => "Request is pending (may be in process, or waiting on third party)",
                Result.EncryptionFailure => "Encryption or Decryption failed",
                Result.Revoked => "Guest pass access has been revoked",
                Result.Expired => "Guest pass access has expired",
                Result.DuplicateRequest => "The request is a duplicate and the action has already occurred in the past, ignored this time",

                Result.LimitExceeded => "Icon filesize cannot exceed 1MB",
                _ => "",
            };
    }

}