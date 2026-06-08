using DolocTown.Config;
using System;
using SimpleJSON;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using RedSaw;
namespace Genesis.ContentLoader
{
#pragma warning disable 1591
    public class Main : IPluginBase
    {
        internal static byte counter = 0;
        internal static Dictionary<string, JSONArray> jsons;
        internal static Dictionary<string, object> __strtotable = new Dictionary<string, object>();
        internal static Dictionary<Type, string> __tabletostr = new Dictionary<Type, string>();
        private static Harmony harmony;
        public void OnGameInit() { }
        public void OnSceneLoaded() { }
        public void Init()
        {
            Util.LogString("ContentLoader", "Patch started");
            harmony = new Harmony("Genesis.ContentLoader");
            //patchall does not work for somewhat reason, have to do it manually
            harmony.Patch(
                typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
                // i was wondering why the code working previously without binding flags passed
                prefix: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch.__tables__prefix), BindingFlags.NonPublic | BindingFlags.Static)),
                postfix: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch.__tables__postfix), BindingFlags.NonPublic | BindingFlags.Static)),
                transpiler: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch.__tables__ilmod), BindingFlags.NonPublic | BindingFlags.Static))
            );
            foreach (ConstructorInfo ctor in
                typeof(Tables).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.PropertyType)
                    .Select(tableTypes => tableTypes.GetConstructor(new Type[] { typeof(JSONNode) })).Where(_ctor => _ctor != null))
                harmony.Patch(ctor, transpiler: new HarmonyMethod(method: typeof(Patch).GetMethod(nameof(Patch.__ctor), BindingFlags.NonPublic | BindingFlags.Static)));
            Util.LogString("ContentLoader", "Patch finished");
            Util.LogString("ContentLoader", "Serialization started");
            foreach (string mods in Directory.GetDirectories(Constant.ModPath))
                ModLoader.__loadmod(mods);
            //ModLoader.__resolvedeps();
            jsons = ModLoader.__mergejson();
            Util.LogString("ContentLoader", "Serialization finished");

        }

        internal static void __postinit()
        {
            if (Main.__strtotable.Any())
                Util.LogString("ContentLoader", "Table initialization done");

        }
    }

    internal class Patch
    {
        //kept as prefix because it can still help relieve the pain of loading jsons
        internal static void __tables__prefix(ref Func<string, JSONNode> loader)
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
                JSONArray mod = Main.jsons.ContainsKey(name) ? Main.jsons[name] : null;
                JSONNode original = _loader(name);
                if (mod != null)
                    JsonUtil.Merge(original.AsArray, mod);
                return original;
            };
        }

        internal static IEnumerable<CodeInstruction> __ctor(IEnumerable<CodeInstruction> instructions)
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

        //i swear to god transpiler is way better than either postfix or prefix
        internal static IEnumerable<CodeInstruction> __tables__ilmod(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction il in instructions)
                //true unique instruction in the ctor
                if (il.opcode == OpCodes.Stloc_0)
                {
                    yield return il;
                    //self explaintory
                    yield return new CodeInstruction(OpCodes.Ldloc_0);
                    yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field("Genesis.ContentLoader.Main:__strtotable"));

                    //push Table* this
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //push this->_dataFileMap
                    yield return new CodeInstruction(OpCodes.Ldfld, AccessTools.Field("DolocTown.Config.Tables:_dataFileMap"));
                    //pop Main::__tabletostr
                    yield return new CodeInstruction(OpCodes.Stsfld, AccessTools.Field("Genesis.ContentLoader.Main:__tabletostr"));
                }
                else
                    yield return il;
        }

        internal static void __tables__postfix()
        {
            Main.counter++;
            //somehow the Tables ctor is called twice, and only the second one(or beyond because hot reload in the future) is what we wanted.
            if (Main.counter > 1)
                Main.__postinit();
        }
    }
}
//i actually also likes shiiba tsumugi