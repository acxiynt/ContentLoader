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

public class Main : IPluginBase
{
    public static Harmony harmony;
    public void Init()
    {
        Util.LogString("ContentLoader", "Patch started");
        harmony = new Harmony("Genesis.ContentLoader");
        //dont ever DARE to use patchall, it does nothing
        harmony.Patch(
            typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
            prefix: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch._Tables)))
        );
        //exception due to dupe keys? never heard of
        IEnumerable<Type> tableTypes = typeof(Tables).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.PropertyType);
        IEnumerable<ConstructorInfo> ctors = tableTypes.Select(tableTypes => tableTypes.GetConstructor(new Type[] { typeof(JSONNode) })).Where(ctor => ctor != null);
        foreach (ConstructorInfo ctor in ctors)
        {
            harmony.Patch(ctor, null, null, new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch._Ctor))));
        }
        Util.LogString("ContentLoader", "Patch finished");
        Util.LogString("ContentLoader", "Serialization started");
        foreach (string mods in Directory.GetDirectories(Constant.ModPath))
            JsonLoader.TryLoadMod(mods);
        Util.LogString("ContentLoader", "Serialization finished");
    }
}

public class Patch
{
    public static void _Tables(ref Func<string, JSONNode> loader)
    {
        Func<string, JSONNode> _loader = loader;
        loader = name =>
        {
            string dataPath = $"Constant.DataPath/{name}.json";
            if (!Directory.Exists(dataPath))
                Directory.CreateDirectory(Path.GetDirectoryName(dataPath));
            if (!File.Exists(dataPath))
            {
                JSONNode json = _loader(name);
                StreamWriter filePtr = File.CreateText(dataPath);
                filePtr.WriteLine(json.ToString(2));
                filePtr.Close();
            }
            JSONArray mod = JsonLoader.jsons.ContainsKey(name) ? JsonLoader.jsons[name] : null;
            JSONNode original = _loader(name);
            if (mod != null)
                JsonUtil.Merge(original.AsArray, mod);
            return original;
        };
    }

    public static IEnumerable<CodeInstruction> _Ctor(IEnumerable<CodeInstruction> instructions)
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

/*
IL_0049: callvirt instance string DolocTown.Config.Resource.VegetationSpawnLut::get_Id()

IL_004e: ldloc.1
IL_004f: callvirt instance void class [netstandard] System.Collections.Generic.Dictionary`2<string, class DolocTown.Config.Resource.VegetationSpawnLut>::Add(!0, !1)
IL_0049: callvirt instance string DolocTown.Config.Time.ChairProto::get_Id()

IL_004E: ldloc.1
IL_004F: callvirt instance void class [mscorlib] System.Collections.Generic.Dictionary`2<string, class DolocTown.Config.Time.ChairProto>::set_Item(!0, !1)
*/