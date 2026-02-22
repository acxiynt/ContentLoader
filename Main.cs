using DolocTown.Config;
using System;
using SimpleJSON;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
namespace Genesis.ContentLoader;

public class Main : IPluginBase
{
    public static Harmony harmony;
    public void Init()
    {
        Util.LogString("ContentLoader", "Hook hit");
        harmony = new Harmony("Genesis.ContentLoader");
        //dont ever DARE to use patchall, it does nothing
        harmony.Patch(
            typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
            prefix: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch._Tables)))
        );
        Util.LogString("ContentLoader", "Patch finished");
    }

    public void OnGameInit()
    {
    }
}

public class Patch
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(Tables))]
    [HarmonyPatch(MethodType.Constructor)]
    [HarmonyPatch(new Type[] { typeof(Func<string, JSONNode>) })]
    public static void _Tables(ref Func<string, JSONNode> loader)
    {
        Func<string, JSONNode> _loader = loader;
        loader = name =>
        {
            string path = Path.Combine(Config.GetConfig("Path", "ModPath"), $"{name}.json");
            string dataPath = Path.Combine(Config.GetConfig("Path", "DataPath"), $"{name}.json");
            if (!File.Exists(dataPath))
                Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
            string errorPath = Config.GetConfig("Path", "LogPath");
            if (!File.Exists(dataPath))
            {
                JSONNode json = _loader(name);
                using StreamWriter filePtr = File.CreateText(dataPath);
                filePtr.WriteLine(json.ToString(2));
            }
            if (File.Exists(path))
            {
                JSONNode mod;
                try
                {
                    mod = JSON.Parse(File.ReadAllText(path));
                }
                catch (Exception e)
                {
                    Util.LogString("GenesisLoader", $"json {name}.json failed to parse with exception {e}, reading game default.");
                    return _loader(name);
                }
                return mod;
            }
            else
            {
                JSONNode json = _loader(name);
                return json;
            }
        };
    }
}