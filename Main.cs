using DolocTown.Config;
using System;
using SimpleJSON;
using HarmonyLib;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Reflection.Emit;
using DolocTown.GameData;
using static ReflectionHelper;
namespace Genesis.ContentLoader
{
    /// <summary>
    /// The class where this mod booststraps.
    /// </summary>
    public class Main : IPluginBase
    {
        private static bool devmode = false;
        internal static byte counter = 0;
        internal static Dictionary<string, JSONArray> jsons;
        private static Dictionary<string, object> __strtotable = new Dictionary<string, object>();
        private static Dictionary<Type, string> __tabletostr = new Dictionary<Type, string>();
        /// <summary>
        /// Returns the dictionary for string to tables, if its not initalized, returns null instead.
        /// </summary>
        public static Dictionary<string, object> StrToTable => (counter > 1) ? __strtotable : null;
        private static Harmony harmony;
#pragma warning disable 1591
        public void OnGameInit() { }
        public void OnSceneLoaded() { }
        public void Init()
        {
            Util.LogString("ContentLoader", "Preloading started.");
            harmony = new Harmony("Genesis.ContentLoader.");
            bool.TryParse(Config.GetConfig("Debug", "Devmode"), out devmode);
            devmode = true;
            if (devmode)
                //ReloadOuterConfig seems to have integraty check, any modification will cause a exception thrown
                harmony.Patch(GetMethod<GameOuterConfigSO>(nameof(GameOuterConfigSO.GetGameOuterConfig)),
                    transpiler: new HarmonyMethod(GetMethod<Patch>(nameof(Patch.__getgameouterconfig)))
                );
            //patchall does not work for somewhat reason, have to do it manually
            harmony.Patch(
                typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
                // i was wondering why the code working previously without binding flags passed
                prefix: new HarmonyMethod(method: GetMethod<Patch>(nameof(Patch.__tables__prefix))),
                postfix: new HarmonyMethod(method: GetMethod<Patch>(nameof(Patch.__tables__postfix))),
                transpiler: new HarmonyMethod(method: GetMethod<Patch>(nameof(Patch.__tables__ilmod)))
            );

            foreach (ConstructorInfo ctor in
                typeof(Tables).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.PropertyType)
                    .Select(tableTypes => tableTypes.GetConstructor(new Type[] { typeof(JSONNode) })).Where(_ctor => _ctor != null))
                harmony.Patch(ctor, transpiler: new HarmonyMethod(method: GetMethod<Patch>(nameof(Patch.__ctor))));

            Util.LogString("ContentLoader", "Serialization started.");
            foreach (string mods in Directory.GetDirectories(Constant.ModPath))
                ModLoader.__loadmod(mods);
            Util.LogString("ContentLoader", "Resolving dependencies.");
            ModLoader.__resolvedeps();
            Util.LogString("ContentLoader", "Serialization finished.");

            jsons = ModLoader.__mergejson();
            Util.LogString("ContentLoader", $"Preloading finished, loaded {ModLoader.LoadedMod.Count} mod{(ModLoader.LoadedMod.Count > 1 ? "s" : "")}.");
        }

        internal static void __postinit()
        {
            Util.LogString("ContentLoader", "Post init started.");
            Util.LogString("ContentLoader", "Resolving JSONOperations.");
            ModLoader.__resolveops();
            Util.LogString("ContentLoader", "Post init finished.");

            //enables dev console
            if (devmode)
            {
                DolocAPI.InitDevelopmentHelper();
                if (DolocAPI.gameManager.gameOuterConfig.enableGameConsole)
                    Util.LogString("ContentLoader", "Devmode active, press F1 to open console");
                DolocAPI.devHelper.Update();
            }
        }
#pragma warning restore
        /// <summary>
        /// For the GUI part, called when reload button is pressed.
        /// </summary>
        public static void OnReload()
        {
            foreach (string mods in Directory.GetDirectories(Constant.ModPath))
                ModLoader.__loadmod(mods);
            ModLoader.__resolvedeps();
            jsons = ModLoader.__mergejson();
            DolocConfig.Reload();
        }
        /// <summary>
        /// For the GUI part, called when mod list is refreshed.
        /// </summary>
        public static void OnRefresh()
        {

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

                    //push this
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    //push this->_dataFileMap; pop
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

        internal static IEnumerable<CodeInstruction> __getgameouterconfig()
        {
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Ldc_I4_1);
            yield return new CodeInstruction(OpCodes.Newobj, GetCtor<GameOuterConfig>());
            yield return new CodeInstruction(OpCodes.Ret);
        }
    }
}
//i actually also likes shiiba tsumugi