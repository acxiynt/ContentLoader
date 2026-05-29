using DolocTown.Config;
using System;
using SimpleJSON;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
namespace Genesis.ContentLoader;
#pragma warning disable 1591
public class Main : IPluginBase
{
    private static Harmony harmony;
    public void Init()
    {
        Util.LogString("ContentLoader", "Patch started");
        harmony = new Harmony("Genesis.ContentLoader");
        //patchall does not work for somewhat reason, have to do it manually
        harmony.Patch(
            typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
            prefix: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch._Tables)))
        );
        foreach (ConstructorInfo ctor in
            typeof(Tables).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.PropertyType)
                .Select(tableTypes => tableTypes.GetConstructor(new Type[] { typeof(JSONNode) })).Where(_ctor => _ctor != null))
            harmony.Patch(ctor, transpiler: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch._Ctor))));
        Util.LogString("ContentLoader", "Patch finished");
        Util.LogString("ContentLoader", "Serialization started");
        foreach (string mods in Directory.GetDirectories(Constant.ModPath))
            JsonLoader.TryLoadMod(mods);
        Util.LogString("ContentLoader", "Serialization finished");
    }
}

internal class Patch
{
    internal static void _Tables(ref Func<string, JSONNode> loader)
    {
        Func<string, JSONNode> _loader = loader;
        loader = name =>
        {
            string dataPath = $"{Constant.DataPath}/{name}.json";
            if (!File.Exists(dataPath))
            {
                JSONNode json = _loader(name);
                StreamWriter filePtr = File.CreateText(dataPath);
                filePtr.WriteLine(json.ToString(2));
                filePtr.Close();
            }
            JSONArray mod = JsonLoader.LoadedJson.ContainsKey(name) ? JsonLoader.LoadedJson[name] : null;
            JSONNode original = _loader(name);
            if (mod != null)
                JsonUtil.Merge(original.AsArray, mod);
            return original;
        };
    }

    internal static IEnumerable<CodeInstruction> _Ctor(IEnumerable<CodeInstruction> instructions)
    {
        foreach (CodeInstruction il in instructions)
        {
            if (il.opcode == OpCodes.Callvirt &&
                il.operand is MethodInfo method &&
                method.Name == "Add" &&
                method.DeclaringType.IsGenericType &&
                method.DeclaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
            {
                MethodInfo setter = method.DeclaringType.GetProperty("Item").GetSetMethod();
                yield return new CodeInstruction(OpCodes.Callvirt, setter);
            }
            else
            {
                yield return il;
            }
        }
    }
}
//i actually also likes shiiba tsumugi