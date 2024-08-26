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
using Aspose.Imaging.FileFormats.Apng;
using LibAPNG;

namespace WildfrostHopeMod.VFX;

public class APNGLoader : GIFLoader
{
    /// <summary>
    /// Call RegisterAllAsApplyEffect or RegisterAllAsDamageEffect to see in VfxStatusSystem
    /// </summary>
    /// <param name="directory"></param>
    /// <param name="playType"></param>
    /// <param name="lookThroughSubfolders"></param>
    /// <param name="initialize"></param>
    public APNGLoader(string directory, PlayType playType = PlayType.applyEffect, bool lookThroughSubfolders = true, bool initialize = true) 
        : base(directory, playType, lookThroughSubfolders, false)
    {
        Debug.LogWarning("APNG Const");
        if (initialize) Initialize(playType);// asTask);
        Debug.LogWarning("APNG Const end");
    }

    /// <summary>
    /// Call RegisterAllAsApplyEffect or RegisterAllAsDamageEffect to see in VfxStatusSystem
    /// </summary>
    public new async void Initialize(PlayType playType = PlayType.applyEffect)
    {
        Debug.Log("[VFX Tools] VFX Init (APNG)");
        SearchOption s = LookThroughSubfolders ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        //CoroutineManager.Start(TextureFromPath(VFXMod.Mod.ImagesDirectory + "muse-dash.gif"));
        //LoadGIFsFromPaths(Dir.GetFiles(Directory, "*.gif", s), PlayType.applyEffect);

        //await LoadGIFsFromPathsAsync(Dir.GetFiles(Directory, "*.gif", s), PlayType.applyEffect);
        LoadAPNGsFromPaths(Dir.GetFiles(Directory, "*.png", s), PlayType.applyEffect);
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
        List<List<Aspose.Imaging.Color>> images = new();

        new Task(() => Debug.LogWarning("frame method " + name)).RunSynchronously();
        using (var image = Aspose.Imaging.Image.Load(path))
        {
            var apng = image as Aspose.Imaging.FileFormats.Apng.ApngImage;
            var apngFrames = apng.Pages.Cast<Aspose.Imaging.FileFormats.Apng.ApngFrame>();
            foreach (var apngFrame in apngFrames)
            {
                var colors = new List<Aspose.Imaging.Color>();
                foreach (var y in Enumerable.Range(0, apngFrame.Height))
                    foreach (var x in Enumerable.Range(0, apngFrame.Width))
                        colors.Add(apngFrame.GetPixel(x, y));
                images.Add(colors);
                delays.Add(apngFrame.FrameTime/1000);
            }
        }

        var textures = new List<Texture2D>();
        new Task(() =>
        {
            foreach (var image in images)
            {
                Texture2D texture = new Texture2D(1, 1);
                texture.SetPixels(image.Select(color => new Color(color.R, color.G, color.B, color.A)).ToArray());
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
    public List<GameObject> LoadAPNGsFromDir(string dirPath, PlayType playType = PlayType.applyEffect) =>
        LoadAPNGsFromPaths(Dir.GetFiles(dirPath, "*.apng", SearchOption.TopDirectoryOnly)).ToList();
    /// <summary>
    /// 
    /// </summary>
    /// <param name="paths"></param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    public List<GameObject> LoadAPNGsFromPaths(string[] paths, PlayType playType = PlayType.applyEffect) => 
        paths.Select(path => LoadAPNGFromPath(path)).ToList();

    

    /// <summary>
    /// 
    /// </summary>
    /// <param name="path"></param>
    /// <returns>Automatically goes into SFXLoader.sounds</returns>
    /// <exception cref="Exception">The FMOD.Result of createSound</exception>
    public GameObject LoadAPNGFromPath(string path, string name = null, PlayType playType = PlayType.applyEffect)
    {
        Debug.LogWarning($"[VFX Tools] Loading from path {path}");
        bool destroyOnEnd = playType == PlayType.applyEffect || playType == PlayType.damageEffect;
        int loops = playType == PlayType.loopingAnimation ? -1 : 1;
        var result = CreateAPNGPrefab(path, loops, out GameObject prefab, name, destroyOnEnd);
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
    public GameObject LoadAPNG(string name, PlayType playType = PlayType.applyEffect)
    {
        var path = Dir.GetFiles(Directory, $"{name}.*").First();
        return LoadAPNGFromPath(path, name, playType);
    }

    public static bool CreateAPNGPrefab(string path, int loops, out GameObject prefab, string name = null, bool destroyOnEnd = true)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"[VFX Tools] {path} doesn't exist! Make sure to include \".apng\"");
        name ??= Path.GetFileNameWithoutExtension(path);

        Debug.LogError("Load vs APNG");
        
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
        {
            List<Sprite> frames = new();
            List<float> delays = new();
            List<Frame> images = new();

            var apng = new APNG(path);
            if (apng.IsSimplePNG)
            {
                Debug.LogError($"[VFX Tools] {path} isn't animated!");
                prefab.Destroy();
                return false;
            }

            int i = 0;
            foreach (var apngFrame in apng.Frames)
            {
                Debug.LogWarning((i++, apngFrame.fcTLChunk.XOffset, apngFrame.fcTLChunk.YOffset, apngFrame.IHDRChunk.Width, apngFrame.IHDRChunk.Height));
                images.Add(apngFrame);
                delays.Add(apngFrame.fcTLChunk.DelayNum / 1000f);
            }

            frames = images.Select(FrameToSprite).ToList();
            foreach (var frame in frames)
            {
                frame.texture.SaveAsPNG(path+$"_{frames.IndexOf(frame)}.png");
            }
            gifAnimator.frames = frames.ToArray();
            gifAnimator.delays = delays.ToArray();
            Debug.LogWarning("Finishing invoke");
        }
        Debug.LogWarning("Escaped invoke");


        if (gifAnimator.frames.Length == 0)
        {
            Debug.LogError($"[VFX Tools] {path} cannot be read!");
            prefab.Destroy();
            return false;
        }
        if (gifAnimator.frames.Length == 1)
        {
            Debug.LogError($"[VFX Tools] {path} isn't animated!");
            prefab.Destroy();
            return false;
        }
        gifAnimator.enabled = true;
        Debug.LogWarning($"[VFX Tools] Created prefab: [{name}] with ID {prefab.GetInstanceID()}!");
        return true;
    }

    static Sprite FrameToSprite(Frame frame)
    {
        uint xOffset = frame.fcTLChunk.XOffset;
        uint yOffset = frame.fcTLChunk.YOffset;
        int targetWidth = frame.IHDRChunk.Width;
        int targetHeight = frame.IHDRChunk.Height;

        Texture2D texture = new Texture2D(2, 2);
        texture.LoadImage(frame.GetStream().ToArray());
        /*texture = texture.MakeReadable(
            new Rect(0, 0, texture.width, texture.height),
            targetWidth,
            targetHeight,
            (int)xOffset,
            (int)(targetHeight - texture.height - yOffset)
            );*/
        Sprite sprite = texture.ToSpriteFull();
        return sprite;
    }








    public GameObject TryPlayEffectFromPath(string path)
    {
        if (!prefabs.ContainsKey(Path.GetFileNameWithoutExtension(path)))
            LoadGIFFromPath(path);
        return TryPlayEffect(Path.GetFileNameWithoutExtension(path));
    }
}