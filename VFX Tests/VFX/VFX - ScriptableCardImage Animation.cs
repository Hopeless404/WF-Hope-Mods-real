using Deadpan.Enums.Engine.Components.Modding;
using Gif2Textures;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using Dir = System.IO.Directory;

namespace WildfrostHopeMod.VFX;

/*[HarmonyPatch(typeof(Card), "SetDescription")]
public class PatchDescWhileTypewriting
{
    static public HashSet<Card> talkers = new();
    static bool Prefix(Card __instance) => !talkers.Contains(__instance);
}
[HarmonyPatch(typeof(Console), nameof(Console.Commands))]
internal class PatchAddCommandTalk
{
    static void Postfix()
    {
        var command = new CommandTalk();
        if (Console.commands.Any(c => c.id == command.id)) return;
        MonoBehaviour.print(command.id);
        Console.commands.Add(command);
    }
}
public class CommandTalk : Console.Command
{
    public override string id => "talk";
    public override string format => "talk <text?>";
    public override void Run(string args)
    {
        if (Console.hover == null)
            Fail("Please hover over a card to use this command");
        else
        {
            var writer = Console.hover.gameObject.GetOrAdd<TypewriterController>();
            var revealHandler = () => writer.Untalk(delay: 1, delayAfter: 1);
            var hiddenHandler = writer.Reset;

            writer.OnCompleteTextRevealed -= revealHandler;
            writer.OnCompleteTextHidden -= hiddenHandler;

            writer.OnCompleteTextRevealed += revealHandler;
            writer.OnCompleteTextHidden += hiddenHandler;

            var text = args.IsNullOrWhitespace() ? (Console.hover.display as Card).descText.text : args;
            writer.Talk(text);
        }
    }
}
public class CardScriptAddComponentTypewriter : CardScript
{
    public override void Run(CardData target) =>
        Events.OnEntityCreated += entity => entity.gameObject.GetOrAdd<TypewriterController>();
}*/

public class ScriptableCardImageAnimation : ScriptableCardImage
{
    public GameObject[] prefabs = new GameObject[0];
    public GameObject idleAnim;
    public Sprite defaultSprite;

    internal bool HasIdleAnim() => idleAnim != null;

    public ScriptableCardImageAnimation(Sprite defaultSprite)
    {
        this.defaultSprite = defaultSprite;
    }
    public override void AssignEvent()
    {
        
    }
}