

//there is a refactor after ver95 rendering the old patch unusable, new one needs to patch if(tryadd(table)) to simply a set and send a true to the stack
#define Ver96
using DolocTown.Config;
using System;
using SimpleJSON;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using DolocTown.GameData;
using static ReflectionHelper;
using static Mono.Cecil.Cil.OpCodes;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Mono.Cecil;
namespace Genesis.ContentLoader
{
    /// <summary>
    /// The class where this mod bootstraps.
    /// </summary>
    public class Main : IPluginBase
    {

        private static bool devmode = false;
        internal static byte counter = 0;
        internal static Dictionary<string, JSONArray> jsons;
        internal static Dictionary<string, object> __strtotable = new Dictionary<string, object>();
        internal static Dictionary<Type, string> __tabletostr = new Dictionary<Type, string>();
        /// <summary>
        /// Returns the dictionary for string to tables, if its not initalized, returns null instead.
        /// </summary>
        public static Dictionary<string, object> StrToTable => (counter > 1) ? __strtotable : null;
        /// <summary>
        /// Returns the dictionary for table type to string, if its not initalized, returns null instead.
        /// </summary>
        public static Dictionary<Type, string> TableToStr => (counter > 1) ? __tabletostr : null;
#pragma warning disable 1591
        public void OnGameInit() { }
        public void OnSceneLoaded() { }

        //going to fully replace harmony with runtimedetour soon
        public void Init()
        {

#if DEBUG
            Util.LogString("ContentLoader", "You are using a nightly / dev version of this plugin, please download latest stable release for best experience.", InfoType.Warning);
#endif

            //planning to move some IL-based patch somewhere else.
            Util.LogString("ContentLoader", "Preloading started.");
            devmode = bool.TryParse(Config.GetConfig("Debug", "Devmode"), out devmode);
            __patch();

            foreach (ConstructorInfo ctor in
                typeof(Tables).GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(property => property.PropertyType)
                    .Select(tableTypes => tableTypes.GetConstructor(new Type[] { typeof(JSONNode) })).Where(_ctor => _ctor != null))
                new Patch(ctor, transpiler: patchconst.__ctor).AddPatch();


            Util.LogString("ContentLoader", "Serialization started.");
            foreach (string mods in Directory.GetDirectories(Constant.ModPath))
                ModLoader.__loadmod(mods);
            Util.LogString("ContentLoader", "Resolving dependencies.");
            ModLoader.__resolvedeps();
            Util.LogString("ContentLoader", "Serialization finished.");

            jsons = ModLoader.__mergejson();
            Util.LogString("ContentLoader", $"custom jsons loaded: [{string.Join(", ", jsons.Keys)}]");

            Util.LogString("ContentLoader", $"Preloading finished, loaded {ModLoader.LoadedMod.Count} mod{(ModLoader.LoadedMod.Count > 1 ? "s" : "")}.");
        }



        private static void __patch()
        {
            if (devmode)
                new Patch(
                    GetMethod<GameOuterConfigSO>(nameof(GameOuterConfigSO.GetGameOuterConfig)),
                    prefix: GetMethod<patchconst>(nameof(patchconst.__getgameouterconfig)),
                    replace: true
                ).AddPatch();

            new Patch(
                typeof(Tables).GetConstructor(new Type[] { typeof(Func<string, JSONNode>) }),
                prefix: GetMethod<patchconst>(nameof(patchconst.__tables__prefix)),
                postfix: GetMethod<patchconst>(nameof(patchconst.__tables__postfix)),
                transpiler: patchconst.__tables__ilmod
            ).AddPatch();
        }



        internal static void __postinit()
        {
            Util.LogString("ContentLoader", "Post init started.");
            Util.LogString("ContentLoader", "Resolving JSONOperations.");
            ModLoader.__resolveops();
            Util.LogString("ContentLoader", "Post init finished.");

            if (devmode)
                Util.LogString("ContentLoader", "Devmode active, press F1 to open console.");
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

    internal class patchconst
    {
        //kept as prefix because it can still help relieve the pain of loading jsons
        internal static void __tables__prefix(Tables @this, ref Func<string, JSONNode> loader)
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

                //note: have to change __ctor for ver 96
                if (mod != null)
                {
                    Util.LogString("ContentLoader", $"loading {name}");
                    JsonUtil.Merge(original.AsArray, mod);
                }
                return original;
            };
        }

        internal static void __ctor(ILCursor cur)
        {

#if Ver95
            foreach (Instruction inst in cur.GetEnumerable())
            {
                //for ver 95
                if (inst.OpCode == Callvirt &&
                    inst.Operand is MethodInfo _method &&
                    _method.Name == "Add" &&
                    _method.DeclaringType.IsGenericType &&
                    _method.DeclaringType.GetGenericTypeDefinition() == typeof(Dictionary<,>))
                {
                    MethodInfo setter = ((MethodInfo)inst.Operand).DeclaringType.GetProperty("Item").GetSetMethod();
                    inst.Operand = setter;
                }
            }
#elif Ver96

#if DEBUG

#endif
            List<Instruction> instrs = new List<Instruction>();
            //find the tryadd()
            foreach (Instruction inst in cur.Instrs)
                if (inst.OpCode == Callvirt &&
                    inst.Operand is MethodReference _method &&
                    _method.Name.Contains("TryAdd"))
                {
                    //doing stuff that modifies the list while enumerating it is a UB and in this case, it throws.
                    instrs.Add(inst);
                }

            foreach (Instruction inst in instrs)
            {
                //replace if(tryadd()) with dictionary_set()
                MethodReference method = (MethodReference)inst.Operand;
                GenericInstanceType type = (GenericInstanceType)method.DeclaringType;
                MethodReference setter = new MethodReference("set_Item", cur.Context.Method.Module.TypeSystem.Void, type)
                {
                    HasThis = true,
                };
                setter.Parameters.Add(new ParameterDefinition(type.GenericArguments[0]));
                setter.Parameters.Add(new ParameterDefinition(type.GenericArguments[1]));
                inst.Operand = setter;

                //faster and even more efficient than adding a extra ldc_i4_1
                if (inst.Next.OpCode == Brfalse || inst.Next.OpCode == Brfalse_S)
                    inst.Next.OpCode = Nop;
                else if (inst.Next.OpCode == Brtrue || inst.Next.OpCode == Brtrue_S)
                    inst.Next.OpCode = Br_S;
            }

            //somehow my brain is wired enough to do those stuff so inefficient and now here goes the cleanest solution
#endif
        }

        //i swear to god transpiler is way better than either postfix or prefix
        internal static void __tables__ilmod(ILCursor cur)
        {
            //true unique instruction in the ctor

            if (cur.TryFindNext(out ILCursor[] curs, (inst) => { return inst.OpCode == Stloc_0 ? true : false; }))
            {
                if (curs.Length != 0)
                    return;

                //self explaintory
                curs[0].Emit(Ldloc_0);
                curs[0].Emit(Stsfld, GetField<Main>(nameof(Main.__strtotable)));

                //push this
                curs[0].Emit(Ldarg_0);
                //push this->_dataFileMap; pop
                curs[0].Emit(Ldfld, GetField<Tables>("_dataFileMap"));
                //pop Main::__tabletostr 
                curs[0].Emit(Stsfld, GetField<Main>(nameof(Main.__tabletostr)));
            }
        }

        internal static void __tables__postfix()
        {
            Main.counter++;
            //somehow the Tables ctor is called twice, and only the second one(or beyond because hot reload in the future) is what we wanted.
            if (Main.counter == 2)
                Main.__postinit();
        }

        //for instance methods, arg0 is void* this, therefore replacing it need to add a @this parameter
        internal static GameOuterConfig __getgameouterconfig()
        {
            return new GameOuterConfig(true, true, true, true);
        }
    }
}
//i actually also likes shiiba tsumugi