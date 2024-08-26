using Deadpan.Enums.Engine.Components.Modding; // Assembly-CSharp.dll
[HarmonyLib.HarmonyPatch(typeof(CheckAchievements), "Start")] // 0Harmony.dll
public class TemplateMod(string modDirectory) : WildfrostMod(modDirectory)
{
    static bool Prefix() => false;
    public override string GUID => "hope.wildfrost.ACHIEVEMENT_16";
    public override string[] Depends => [];
    public override string Title => "ACHIEVEMENT_16";
    public override string Description => "Here's your ACHIEVEMENT_16";
}