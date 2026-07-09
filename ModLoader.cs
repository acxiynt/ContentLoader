using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SimpleJSON;

namespace Genesis.ContentLoader
{
    /// <summary>
    /// Provides information of and load content mods.
    /// </summary>
    public class ModInfo
    {
        private string modID;
        private string modName = "No Mod Name";
        private string version = "No Version";
        private string description = "No Description";
        private string author = "No Author";
        private List<string> dependency;
        /// <summary>
        /// unique ID of the mod, cannot be empty or duplicating.
        /// </summary>
        public string ModID => modID;
        /// <summary>
        /// Verbalized Name of the mod.
        /// </summary>
        public string ModName => modName;
        /// <summary>
        /// Version of the mod, customizable.
        /// </summary>
        public string Version => version;
        /// <summary>
        /// Optional description of the mod.
        /// </summary>
        public string Description => description;
        /// <summary>
        /// Author of the mod.
        /// </summary>
        public string Author => author;
        /// <summary>
        /// Other mod(in form of ModID) that is required to run this mod.
        /// </summary>
        public List<string> Dependency => dependency;
        /// <summary>
        /// Constructs modinfo out of json node provided in the parameter.
        /// </summary>
        /// <param name="info"></param>
        /// <exception cref="Exception"></exception>
        public ModInfo(JSONNode info)
        {
            modID = info["ModID"];
            if (string.IsNullOrWhiteSpace(modID))
                throw new JsonParseException("ModInfo.json does not contain modID");
            string modName = info["ModName"];
            this.modName = string.IsNullOrWhiteSpace(modName) ? this.modName : modName;

            string version = info["Version"];
            this.version = string.IsNullOrWhiteSpace(version) ? this.version : version;

            string description = info["Description"];
            this.description = string.IsNullOrWhiteSpace(description) ? this.version : description;

            string author = info["Author"];
            this.author = string.IsNullOrWhiteSpace(author) ? this.author : author;

            dependency = info["Dependency"].AsStringList ?? new List<string>();
        }
        /// <summary>
        /// Converts all information of the mod into a string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{modName}({ModID}) {version}\nauthor: {author}\ndescription: {description}\nmod dependency:[{string.Join(", ", dependency)}]";
        }
        /// <summary>
        /// Overrides default GetHashCode(), uses modID and returns a hash based on it.
        /// </summary>
        /// <returns>Hash of modID, as int32</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(modID);
        }
        /// <summary>
        /// Overrides default Equals(), uses modID and returns a hash based on it.
        /// </summary>
        /// <returns>The result modID comparing</returns>
        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(ModInfo) && ((ModInfo)obj).modID == modID)
                return true;
            return false;
        }
    }



    /// <summary>
    /// Storage class for why a mod is disabled
    /// </summary>
    public class DisabledContext
    {

        private string[] missingDependency = null;

        /// <summary>
        /// Missing Dependency
        /// </summary>
        public string[] MissingDependency => missingDependency;

        /// <summary>
        /// Disabled Reason stored as a enum.
        /// </summary>
        public enum DisabledReason
        {

            /// <summary>
            /// Mod got automatically disabled due to a crash.
            /// </summary>
            Crashed = 0,

            /// <summary>
            /// Mod loader refuses to load the mod due to circular dependency.
            /// </summary>
            Circular = 1,

            /// <summary>
            /// Mod loader refuses to load the mod due to missing dependency.
            /// </summary>
            Dependency = 2,
        }
    }



    /// <summary>
    /// A container to pack up information of mods.
    /// </summary>
    public class Mod
    {
        private ModInfo info;
        private Dictionary<string, JSONArray> table = new Dictionary<string, JSONArray>();
        private Dictionary<string, JSONOperation> operations = new Dictionary<string, JSONOperation>();
        /// <summary>
        /// The mod's info.
        /// </summary>
        public ModInfo Info => info;
        /// <summary>
        /// The mod's json tables, stored in KVP of table name and the json data array.
        /// </summary>
        public Dictionary<string, JSONArray> Table => table;
        /// <summary>
        /// The mod's operation for modifying asset, stored in KVP of table name and the operation.
        /// </summary>
        public Dictionary<string, JSONOperation> Operations => operations;
        /// <summary>
        /// instantiate a mod with all the information possible.
        /// </summary>
        /// <param name="info"></param>
        /// <param name="table"></param>
        /// <param name="operations"></param>
        public Mod(ModInfo info, Dictionary<string, JSONArray> table = null, Dictionary<string, JSONOperation> operations = null)
        {
            this.info = info;
            this.table = table;
            this.operations = operations;
        }
        /// <summary>
        /// Overrides default GetHashCode(), uses modID and returns a hash based on it.
        /// </summary>
        /// <returns>Hash of modID, as int32</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(info.ModID);
        }
        /// <summary>
        /// Overrides default Equals(), returns true if the param passed is a mod or a string and the modid/string is equal to ID.
        /// </summary>
        /// <returns>The comparation result, as bool.</returns>
        public override bool Equals(object obj)
        {
            if (obj is Mod mod && mod.info.ModID == info.ModID)
                return true;
            if (obj is string str && str == info.ModID)
                return true;
            return false;
        }
        /// <summary>
        /// Checks if ModInfo's dependency section is empty or not.
        /// </summary>
        /// <returns>Dependency is empty or not, if not empty, return true, otherwise false.</returns>
        public bool HasDependency()
        {
            return info.Dependency.Any();
        }
    }
    /// <summary>
    /// Loads json and asset package into mod, supports hot reload and mod disabling.
    /// </summary>
    public static class ModLoader
    {

        /// <summary>
        /// Currently loaded mod, stored as dictionary of modID string and Mod.
        /// </summary>
        public static Dictionary<string, Mod> LoadedMod = new Dictionary<string, Mod>();

        /// <summary>
        /// Mods that dont have required prerequisite or disabled by user
        /// </summary>
        public static HashSet<string> DisabledMod = new HashSet<string>();

        /// <summary>
        /// Hot reload and rediscover mod for mod GUI inside main menu.
        /// </summary>
        public static void Reload()
        {
            foreach (string mods in Directory.GetDirectories(Constant.ModPath))
                __loadmod(mods);
            Update();
        }

        /// <summary>
        /// Adds a mod into disabled list.
        /// </summary>
        /// <param name="modID">Mod to be disabled</param>
        public static void AddDisabledMod(string modID)
        {
            DisabledMod.Add(modID);
        }

        /// <summary>
        /// Removes a mod in the disabled list.
        /// </summary>
        /// <param name="modID">The mod to be reenabled.</param>
        /// <returns>Successfully removed, if false, then the mod dont exist.</returns>
        public static bool RemoveDisabledMod(string modID)
        {
            if (DisabledMod.Contains(modID))
            {
                DisabledMod.Remove(modID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Replaces the disable list with a new one.
        /// </summary>
        /// <param name="modIDs">The new list of mods to disable.</param>
        public static void UpdateDisabledMod(string[] modIDs)
        {
            DisabledMod = new HashSet<string>(modIDs);
        }

        internal static Dictionary<string, Mod> ModWithDependency = new Dictionary<string, Mod>();

        internal static void __loadmod(string path)
        {
            ModInfo info = JsonLoader.__ldinfo(path);
            bool success = true;
            if (info == null)
                success = false;
            if (DisabledMod.Contains(info.ModID))
            {
                success = false;
                goto end;
            }
            List<JSONNode> nodes = new List<JSONNode>();
            if (Directory.Exists($"{path}/Contents"))
                foreach (string item in Directory.GetFiles($"{path}/Contents", "*.json", SearchOption.AllDirectories))
                    nodes.AddRange(JsonLoader.__ldjson(item));
            JsonLoader.__resolvenodes(nodes, out Dictionary<string, JSONArray> tb, out Dictionary<string, JSONOperation> op, path);
            Mod mod = new Mod(info, tb, op);
            if (mod.HasDependency())
                ModWithDependency.Add(info.ModID, mod);
            else
                LoadedMod.Add(info.ModID, mod);
        end:
            if (success)
                Util.LogString("ContentLoader", $"{info}");
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private unsafe delegate __depresult* solvedep(char** _loaded, __mod_kvp* _toload, int loadedlen, int toloadlen, char*** _disabled_reason);
        //recursion replaced by goto for no potential stack overflow.
        internal unsafe static void __resolvedeps()
        {
            //making sure if software accel worth the marshalling
            if (ModWithDependency.Count < 50)
                goto noacl;

            string path = $"{Config.GetConfig("Path", "AssemblyPath")}\\genesis.swacl.so.0";
            const long nullptr = 0;
            IntPtr lib = (IntPtr)nullptr;
            if (File.Exists(path))
                lib = NativeLibrary.Load(path);
            if ((long)lib == nullptr)
                goto noacl;

            NativeLibrary.TryGetExport(lib, "solvedep", out IntPtr _func);
            if ((long)_func == nullptr)
            {
                NativeLibrary.Free(lib);
                goto noacl;
            }

            __depresult* result = (__depresult*)0;
            char*** reason = (char***)Marshal.AllocHGlobal(sizeof(IntPtr));
            solvedep func = Marshal.GetDelegateForFunctionPointer<solvedep>(_func);

            result = func(InteropHelper.__strarrtoptr(LoadedMod.Keys.ToArray()), InteropHelper.__getmodkvps(out int kvpcount), LoadedMod.Keys.Count, kvpcount, reason);

            goto swacl_cleanup;

        noacl:
            List<string> remove = new List<string>();
        recur:
            foreach (KeyValuePair<string, Mod> pair in ModWithDependency)
            {
                bool skip = false;
                foreach (string mod in pair.Value.Info.Dependency)
                {
                    if (!LoadedMod.ContainsKey(mod))
                        if (!ModWithDependency.ContainsKey(mod))
                        {
                            skip = true;
                            DisabledMod.Add(pair.Value.Info.ModID);
                            remove.Add(pair.Value.Info.ModID);
                            break;
                        }
                        else
                        {
                            skip = true;
                            break;
                        }
                }
                if (skip)
                    continue;
                LoadedMod[pair.Key] = pair.Value;
                Util.LogString("ContentLoader", $"{pair.Value.Info}");
                remove.Add(pair.Value.Info.ModID);
            }
            if (!remove.Any())
            {
                foreach (KeyValuePair<string, Mod> pair in ModWithDependency)
                    DisabledMod.Add(pair.Key);
                ModWithDependency.Clear();
                return;
            }
            else if (ModWithDependency.Any())
            {
                ModWithDependency.Remove(remove);
                remove.Clear();
                goto recur;
            }
            return;

        swacl_cleanup:

            NativeLibrary.Free(lib);

            if ((long)result == nullptr)
            {
                Util.LogString("ContentLoader", "Software acceleration failed, falling back to noacl", InfoType.Warning);
                goto noacl;
            }

            for (int i = 0; i < result->dcount; i++)
            {
                string mod = new string(result->disable[i]);
                ModWithDependency.Remove(mod);
                DisabledMod.Add(mod);
            }
            for (int i = 0; i < result->ecount; i++)
            {
                string mod = new string(result->enable[i]);
                LoadedMod.Add(mod, ModWithDependency[mod]);
                ModWithDependency.Remove(mod);
            }

            //frees the block
            Marshal.FreeHGlobal((IntPtr)result);

            return;
        }

        //resolves every JSONOperation loaded.
        internal static void __resolveops()
        {

        }

        //combine every json mod into a dictionary of tables.
        internal static Dictionary<string, JSONArray> __mergejson()
        {
            Dictionary<string, JSONArray> dict = new Dictionary<string, JSONArray>();
            foreach (Mod mod in LoadedMod.Values)
                foreach (KeyValuePair<string, JSONArray> pair in mod.Table)
                {
                    if (dict.ContainsKey(pair.Key))
                        JsonUtil.Merge(dict[pair.Key], pair.Value);
                    else dict[pair.Key] = pair.Value;
                }
            return dict;
        }

        //updates the mod list after start/reload by removing every disabled mod from actively loaded mods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void Update()
        {
            LoadedMod.Remove(DisabledMod);
        }
    }
}