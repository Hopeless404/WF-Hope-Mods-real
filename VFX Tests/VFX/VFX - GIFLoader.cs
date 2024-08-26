using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Dir = System.IO.Directory;
using WildfrostHopeMod.Utils;
using WildfrostHopeMod.Utils.mgGIF;
using UnityEngine.UI;
using UnityEngine.Localization.SmartFormat.Extensions;
using UnityEngine.Networking;
using TMPro;
using System.Threading;

namespace WildfrostHopeMod.VFX;

public class GIFLoader
{
    public enum PlayType
    {
        applyEffect, // play once, destroy on end, register to VfxStatusSystem.Profiles
        damageEffect, // play once, destroy on end, register to VfxStatusSystem.DamageProfiles
        loopingAnimation, // play forever, don't destroy on end
        oneshotAnimation, // play once, don't destroy on end
    }
    public GIFLoader(string directory, PlayType playType = PlayType.applyEffect, bool lookThroughSubfolders = true, bool initialize = true)
    {
        Debug.LogWarning("GIF LOADER " + initialize);
        Directory = directory;
        LookThroughSubfolders = lookThroughSubfolders;
        if (initialize) Initialize(playType);// asTask);
    }
    public string Directory { get; }
    public bool LookThroughSubfolders { get; }

    public static event Action<GameObject> OnEffectPlayed;

    /// <summary>
    /// Automatically filled whenever any LoadGIF..() is called
    /// </summary>
    public readonly Dictionary<string, GameObject> prefabs = new();

    public async void Initialize(PlayType playType = PlayType.applyEffect)
    {
        Debug.Log("[VFX Tools] VFX Init");
        SearchOption s = LookThroughSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        //CoroutineManager.Start(TextureFromPath(VFXMod.Mod.ImagesDirectory + "muse-dash.gif"));
        //LoadGIFsFromPaths(Dir.GetFiles(Directory, "*.gif", s), PlayType.applyEffect);

        //await LoadGIFsFromPathsAsync(Dir.GetFiles(Directory, "*.gif", s), PlayType.applyEffect);
        LoadGIFsFromPaths(Dir.GetFiles(Directory, "*.gif", s), PlayType.applyEffect);
        Debug.LogWarning($"[VFX Tools] VFX Init result: Count = {prefabs.Count}, Keys = {string.Join(", ", prefabs.Keys)}");
    }
    static async Task LogAll(int n)
    {
        var tasks = new Task[2*n];
        for (int i = 0; i < n; i++)
        {
            tasks[2 * i + 1] = Task.Delay(1000);
            tasks[2*i] = Logger(i + 1);
        }
        await Task.WhenAll(tasks);
    }
    static Task Logger(int i)
    {
        Debug.LogWarning(i);
        return Task.Run(() => new Task(() => Debug.LogWarning($"{i}")).RunSynchronously());
    }


    Texture2D _tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
    IEnumerator TextureFromPath(string path)
    {
        using (var uwr = UnityWebRequest.Get(path))
        {
            yield return uwr.SendWebRequest();
            if (string.IsNullOrEmpty(uwr.error))
                _tex.LoadImage(uwr.downloadHandler.data);
            else Debug.LogError(uwr.error);
        }
        Debug.LogError("WEEWOOWEEWOO");
        Debug.LogError(_tex.width);
    }

    /// <param name="paths"></param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    public async Task LoadGIFsFromPathsAsync(string[] paths, PlayType playType = PlayType.applyEffect)
    {
        Debug.LogError("[VFX Tools] Starting LOAD async");
        await Task.WhenAll(paths.Select(path => LoadGIFFromPathAsync(path)));
        Debug.LogError("[VFX Tools] Ending LOAD async");
    }

    public async Task<GameObject> LoadGIFFromPathAsync(string path, string name = null, PlayType playType = PlayType.applyEffect)
    {
        Debug.LogError("[VFX Tools] Starting LOADGIF async");
        bool destroyOnEnd = playType == PlayType.applyEffect || playType == PlayType.damageEffect;
        int loops = playType == PlayType.loopingAnimation ? -1 : 1;
        GameObject prefab = await CreateGifPrefabAsync(path, loops, name, destroyOnEnd);
        Debug.LogError("[VFX Tools] Ending LOADGIF async");
        if (prefab == null)
        {
            Debug.LogWarning($"[VFX Tools] Could not load {prefab} from path {path}");
            return null;
        }
        name ??= Path.GetFileNameWithoutExtension(path);
        prefabs[name] = prefab;
        Debug.Log($"[VFX Tools] Async Loaded {prefab} from path {path}");
        return prefab;
    }
    public static async Task<GameObject> CreateGifPrefabAsync(string path, int loops, string name = null, bool destroyOnEnd = true)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"[VFX Tools] {path} doesn't exist! Make sure to include \".gif\"");
        name ??= Path.GetFileNameWithoutExtension(path);

        Debug.LogError("Load vs GIF async");

        byte[] data = new byte[0];
        GameObject prefab = null;
        GIFAnimator gifAnimator = null;
        new Task(new Action(() =>
        {
            StopWatch.Start();
            data = File.ReadAllBytes(path);

            prefab = new GameObject(name, typeof(SpriteRenderer));
            GameObject.DontDestroyOnLoad(prefab);
            prefab.SetLayerRecursively(8);
            prefab.GetComponent<SpriteRenderer>().sortingLayerID = -2147482037;
            gifAnimator = prefab.AddComponent<GIFAnimator>();
            gifAnimator.frames = new Sprite[0];
            gifAnimator.originalID = prefab.GetInstanceID();
            gifAnimator.loops = loops;
            gifAnimator.destroyOnEnd = destroyOnEnd;
            Debug.LogError("load v gif");
        })).RunSynchronously();

        List<Sprite> frames = new();
        List<float> delays = new();
        List<Utils.mgGIF.Image> images = new();

        new Task(() => Debug.LogWarning("frame method " + name)).RunSynchronously();
        using (var decoder = new Utils.mgGIF.Decoder(data))
        {
            float delay = 1 / 24;
            var img = decoder.NextImage();
            while (img != null)
            {
                images.Add(img);
                delays.Add(delay = img.Delay / 1000f);
                img = decoder.NextImage();
            }
        };

        var textures = new List<Texture2D>();
        new Task(() =>
        {
            foreach (var image in images)
            {
                Texture2D texture = image.CreateTexture();
                frames.Add(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect));
            }
            gifAnimator.frames = frames.ToArray();
            gifAnimator.delays = delays.ToArray();
        }).RunSynchronously();
        
        Debug.LogWarning($"[VFX Tools] Created prefab: [{name}]??");
        
        if (gifAnimator.frames.Length == 0)
        {
            Debug.LogError($"[VFX Tools] {path} cannot be read!");
            prefab.Destroy();
            return null;
        }
        gifAnimator.enabled = true;
        Debug.LogWarning($"[VFX Tools] Created prefab: [{name}] with ID {prefab.GetInstanceID()}!");
        return prefab;
    }













    public void RegisterAllAsApplyEffect() =>
        prefabs.Values.RegisterAsApplyEffectMany();
    public void RegisterAllAsDamageEffect() =>
        prefabs.Select(p => p.Value.RegisterAsDamageEffect(p.Key));

    /// <summary>
    /// 
    /// </summary>
    /// <param name="dirPath"></param>
    /// <param name="extensions">without the dot, i.e. "wav"</param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    public List<GameObject> LoadGIFsFromDir(string dirPath, PlayType playType = PlayType.applyEffect) =>
        LoadGIFsFromPaths(Dir.GetFiles(dirPath, "*.gif", SearchOption.TopDirectoryOnly)).ToList();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="paths"></param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    public List<GameObject> LoadGIFsFromPaths(string[] paths, PlayType playType = PlayType.applyEffect) => 
        paths.Select(path => LoadGIFFromPath(path)).ToList();

    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    /// <exception cref="Exception">The FMOD.Result of createSound</exception>
    public GameObject LoadGIFFromPath(string path, string name = null, PlayType playType = PlayType.applyEffect)
    {
        bool destroyOnEnd = playType == PlayType.applyEffect || playType == PlayType.damageEffect;
        int loops = playType == PlayType.loopingAnimation ? -1 : 1;
        var result = CreateGifPrefab(path, loops, out GameObject prefab, name, destroyOnEnd);
        if (prefab == null || !result)
        {
            Debug.LogWarning($"[VFX Tools] Could not load {prefab} from path {path}");
            return null;
        }
        name ??= Path.GetFileNameWithoutExtension(path);
        prefabs[name] = prefab;
        Debug.Log($"[VFX Tools] Loaded {prefab} from path {path}");
        return prefab;
    }
    public GameObject LoadGIF(string name, PlayType playType = PlayType.applyEffect)
    {
        var path = Dir.GetFiles(Directory, $"{name}.*").First();
        return LoadGIFFromPath(path, name, playType);
    }

    public static bool CreateGifPrefab(string path, int loops, out GameObject prefab, string name = null, bool destroyOnEnd = true)
    {
        if (path == null || !File.Exists(path))
            throw new FileNotFoundException($"[VFX Tools] {path} doesn't exist! Make sure to include \".gif\"");
        name ??= Path.GetFileNameWithoutExtension(path);

        Debug.LogError("Load vs GIF: " + path);
        
        byte[] data = new byte[0];
        new Action(() => data = File.ReadAllBytes(path)).TimedInvoke();

        prefab = new GameObject(name, typeof(SpriteRenderer));
        GameObject.DontDestroyOnLoad(prefab);
        prefab.SetLayerRecursively(8);
        prefab.GetComponent<SpriteRenderer>().sortingLayerID = -2147482037;
        var gifAnimator = prefab.AddComponent<GIFAnimator>();
        gifAnimator.frames = new Sprite[0];
        gifAnimator.originalID = prefab.GetInstanceID();
        gifAnimator.loops = loops;
        gifAnimator.destroyOnEnd = destroyOnEnd;

        using (var decoder = new Utils.mgGIF.Decoder(data))
        {
            List<Sprite> frames = new();
            List<float> delays = new();
            float delay = 1 / 24;
            var img = decoder.NextImage();
            var allimg = new List<Utils.mgGIF.Image>();

            bool framesDebugMethod = false;
            while (img != null)
            {
                if (!framesDebugMethod)
                {
                    Texture2D texture = img.CreateTexture();
                    frames.Add(Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect));
                }
                allimg.Add(img);
                delays.Add(delay = img.Delay / 1000f);
                img = decoder.NextImage();
            }
            if (framesDebugMethod) frames = ImagesToFrames(allimg);
            gifAnimator.frames = frames.ToArray();
            gifAnimator.delays = delays.ToArray();
        }


        if (gifAnimator.frames.Length == 0)
        {
            Debug.LogError($"[VFX Tools] {path} cannot be read!");
            prefab.Destroy();
            return false;
        }
        gifAnimator.enabled = true;
        Debug.LogWarning($"[VFX Tools] Created prefab: [{name}] with ID {prefab.GetInstanceID()}!");
        return true;
    }

    static List<Sprite> ImagesToFrames(List<Utils.mgGIF.Image> images)
    {
        var img = images.First();
        var raw = new List<Color32>();
        Texture2D texture = new Utils.mgGIF.Image()
        {
            Width = img.Width,
            Height = img.Height * images.Count,
            RawImage = raw.ToArray()
        }.CreateTexture();

        var frames = new List<Sprite>();
        int offset = 0;
        foreach (var image in images)
        {
            frames.Add(
                Sprite.Create(texture, new Rect(0, offset, image.Width, image.Height), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect)
                );
            offset += image.Height;
        }

        return frames;
    }








    public GameObject TryPlayEffectFromPath(string path)
    {
        if (!prefabs.ContainsKey(Path.GetFileNameWithoutExtension(path)))
            LoadGIFFromPath(path);
        return TryPlayEffect(Path.GetFileNameWithoutExtension(path));
    }

    /// <summary>
    /// Plays the sound from SFXLoader.sounds if it exists
    /// </summary>
    /// <param name="key">The filename without extension</param>
    /// <returns>true if OK; otherwise false</returns>
    public GameObject TryPlayEffect(string key, Vector3 position = default, Vector3 scale = default, PlayType playAs = PlayType.applyEffect)
    {
        if (prefabs.TryGetValue(key, out GameObject prefab))
            return PlayEffect(prefab, position, scale, playAs);
        else Debug.LogWarning($"[VFX Tools] Key [{key}] doesn't exist");
        return null;
    }
    public static GameObject PlayEffect(GameObject gifPrefab, Vector3 position = default, Vector3 scale = default, PlayType playType = PlayType.applyEffect)
    {
        //Debug.LogWarning($"Creating effect {gifPrefab.name} at {position}");
        GameObject go;
        (go = GameObject.Instantiate(gifPrefab, position, Quaternion.identity)).transform.localScale = scale;
        var ga = go.GetComponent<GIFAnimator>();
        bool destroyOnEnd = playType == PlayType.applyEffect || playType == PlayType.damageEffect;
        int loops = playType == PlayType.loopingAnimation ? -1 : 1;
        ga.destroyOnEnd = destroyOnEnd;
        ga.loops = loops;
        OnEffectPlayed?.Invoke(gifPrefab);
        return go;
    }
    public static GameObject PlayRandomEffect(IEnumerable<GameObject> prefabs, PlayType playAs = PlayType.applyEffect)
    {
        if (!prefabs.Any()) return default;
        GameObject prefab = prefabs.RandomItems(1).First();
        PlayEffect(prefab, playType: playAs);
        return prefab;
    }
}
public static class GIFLoaderExt
{
    /// <summary>
    /// Register this prefab in the global VfxStatusSystem to play on applying a status with defined .type
    /// </summary>
    /// <param name="applyEffectPrefab"></param>
    /// <param name="type">type of the StatusEffectApply this should play for</param>
    /// <returns></returns>
    public static VfxStatusSystem.Profile RegisterAsApplyEffect(this GameObject applyEffectPrefab, string type)
    {
        VfxStatusSystem.Profile profile;

        var vfx = GameObject.FindObjectOfType<VfxStatusSystem>();
        profile = vfx.profiles.FirstOrDefault(p => p.type == type);

        if (profile != default)
            profile.applyEffectPrefab = applyEffectPrefab;
        else
        {
            profile = new VfxStatusSystem.Profile()
            {
                type = type,
                applyEffectPrefab = applyEffectPrefab
            };
            vfx.profiles = vfx.profiles.With(profile);
            vfx.profileLookup[type] = profile;
            Debug.LogWarning("Registered " + type);
        }
        return profile;
    }
    public static List<VfxStatusSystem.Profile> RegisterAsApplyEffectMany(this IEnumerable<GameObject> applyEffectPrefabs) =>
        applyEffectPrefabs.Select(p => RegisterAsApplyEffect(p, p.name)).ToList();


    /// <summary>
    /// Register this prefab in the global VfxStatusSystem to play on taking damage from a status with defined .type
    /// </summary>
    /// <param name="damageEffectPrefab"></param>
    /// <param name="damageType">type of the damaging Status this should play for</param>
    /// <returns></returns>
    public static VfxStatusSystem.DamageProfile RegisterAsDamageEffect(this GameObject damageEffectPrefab, string damageType)
    {
        var profile = new VfxStatusSystem.DamageProfile()
        {
            damageType = damageType,
            damageEffectPrefab = damageEffectPrefab
        };
        var vfx = GameObject.FindObjectOfType<VfxStatusSystem>();
        vfx.damageProfiles = vfx.damageProfiles.With(profile);
        vfx.damageProfileLookup[damageType] = profile;
        return profile;
    }
    public static List<VfxStatusSystem.DamageProfile> RegisterAsDamageEffectMany(this IEnumerable<GameObject> damageEffectPrefabs) =>
        damageEffectPrefabs.Select(p => RegisterAsDamageEffect(p, p.name)).ToList();

}
