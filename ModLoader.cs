using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DolocTown;
using SimpleJSON;

namespace Genesis.ContentLoader
{
    //for interop with genesis.swacl.0
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct __depresult
    {
        internal char* __global_buf;
        internal char** enable;
        internal char** disable;
        internal int ecount;
        internal int dcount;
    }
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct __mod_kvp
    {
        internal char* id;
        internal char** dep;
        internal int depcount;
    }
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
        /// Overrides default Equals(), uses modID and returns a hash based on it.
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
        /// Mods that requires other mod as dependency to run.
        /// </summary>
        public static Dictionary<string, Mod> ModWithDependency = new Dictionary<string, Mod>();
        /// <summary>
        /// Mods that dont have required prerequisite or disabled by user
        /// </summary>
        public static HashSet<string> DisabledMod = new HashSet<string>();
        internal static void Start()
        {

        }
        /// <summary>
        /// Hot reload and rediscover mod for mod GUI inside main menu.
        /// </summary>
        public static void Reload()
        {
        }
        /// <summary>
        /// Adds a mod into disabled list.
        /// </summary>
        /// <param name="modID">Mod to be disabled</param>
        public static void AddDisabledMod(string modID)
        {
            if (DisabledMod.Contains(modID))
                return;
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

        internal static bool __loadmod(string path)
        {
            ModInfo info = JsonLoader.__ldinfo(path);
            if (info == null)
                return false;
            List<JSONNode> nodes = new List<JSONNode>();
            if (Directory.Exists($"{path}/Contents"))
                foreach (string item in Directory.GetFiles($"{path}/Contents", "*.json", SearchOption.AllDirectories))
                    nodes.AddRange(JsonLoader.__ldjson(path));
            JsonLoader.__resolvenodes(nodes, out Dictionary<string, JSONArray> tb, out Dictionary<string, JSONOperation> op, path);
            Mod mod = new Mod(info, tb, op);
            if (mod.HasDependency())
                ModWithDependency.Add(info.ModID, mod);
            else
                LoadedMod.Add(info.ModID, mod);
            return true;
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]

        private unsafe delegate __depresult* solvedep(char** _loaded, __mod_kvp* _toload, int loadedlen, int toloadlen);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]

        //adding out __buf for easy cleanup
        private static unsafe char** __strarrtoptr(string[] arr, out char* __buf)
        {
            int size = 0;
            int charsize = sizeof(char);

            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == null)
                    throw new ArgumentNullException($"Array 0X{RuntimeHelpers.GetHashCode(arr): X} contains null string");
                size += (arr[i].Length + 1) * charsize;
            }


            //mallocing once is faster than mallocing twice
            __buf = (char*)Marshal.AllocHGlobal(size + (sizeof(IntPtr) * arr.Length));
            char** ptr = (char**)((byte*)__buf + size);

            int off = 0;

            //parses string[] into the big char* buffer, then re-wrap the char* buffer into char**
            for (int i = 0; i < arr.Length; i++)
            {
                int len = arr[i].Length;
                fixed (char* str = arr[i])
                    Buffer.MemoryCopy(str, __buf + off, (len + 1) * charsize, len * charsize);
                __buf[off + len] = '\0';
                ptr[i] = &__buf[off];
                off += len + 1;
            }
            return ptr;
        }
        /// <summary>
        /// Creates a __mod_kvp* out of all ModWithDependency with a buffer pointing all the memory it allowcated. <br/>
        /// The memory have a layout of 4 areas, shown below:<br/>
        /// Main string / String pointer for mod strings / Dependency pointer pointing the second area / The kvps stored in pointer(this is what this method returns)
        /// </summary>
        /// <param name="__buf">the pointer of its global memory for easy cleanups.</param>
        /// <param name="count">the number of elements inside the created pointer.</param>
        /// <returns>the pointer of initialized __mod_kvp*.</returns>
        private static unsafe __mod_kvp* __getmodkvps(out char* __buf, out int count)
        {
            //single malloc
            int size = 0;
            int charsize = sizeof(char);
            count = ModWithDependency.Count;

            //how many char** do we need to preallowcate for dependency (yes the deps is a char***)
            int depsize = 0;

            //how many space for char** do we need to preallowcate for dependency
            int deplen = 0;

            foreach (KeyValuePair<string, Mod> pair in ModWithDependency)
            {
                size += (pair.Key.Length + 1) * charsize;
                foreach (string dep in pair.Value.Info.Dependency)
                {
                    size += (dep.Length + 1) * charsize;
                    deplen += sizeof(IntPtr);
                }
                depsize += sizeof(IntPtr);
            }

            // buffer have 4 layers
            __buf = (char*)Marshal.AllocHGlobal(size + deplen + depsize + (sizeof(__mod_kvp) * count));

            char*** deps = (char***)((byte*)__buf + size + deplen);

            __mod_kvp* kvps = (__mod_kvp*)((byte*)__buf + size + deplen + depsize);
            KeyValuePair<string, string[]>[] _kvps =
                ModWithDependency.Select(
                    pair => new KeyValuePair<string, string[]>(
                        pair.Key, pair.Value.Info.Dependency.ToArray())).ToArray();

            int off = 0;
            int offdep = 0;
            int strsize;
            for (int a = 0; a < count; a++)
            {
                //pushes id into buffer
                string str = _kvps[a].Key;
                int len = str.Length;
                strsize = (len + 1) * charsize;
                fixed (char* id = str)
                    Buffer.MemoryCopy(id, __buf + off, strsize, strsize - charsize);
                kvps[a].id = __buf + off;
                off += len + 1;
                __buf[off - 1] = '\0';

                //gradually initializes char** areas
                int _deplen = _kvps[a].Value.Length;
                deps[a] = (char**)((byte*)__buf + size + offdep);
                offdep += _deplen * sizeof(IntPtr);

                //pushes dependencies into buffer
                for (int b = 0; b < _kvps[a].Value.Length; b++)
                {
                    str = _kvps[a].Value[b];
                    len = str.Length;
                    strsize = (len + 1) * charsize;
                    fixed (char* id = str)
                        Buffer.MemoryCopy(id, __buf + off, strsize, strsize - charsize);
                    deps[a][b] = __buf + off;
                    off += len + 1;
                    __buf[off - 1] = '\0';
                }

                kvps[a].depcount = _kvps[a].Value.Length;
                kvps[a].dep = deps[a];
            }
            return kvps;
        }

        //recursion replaced by goto for no potential stack overflow.
        internal unsafe static void __resolvedeps()
        {
            //making sure if software accel worth the marshalling
            if (ModWithDependency.Count < 50)
                goto noacl;

            string path = $"{Config.GetConfig("Path", "AssemblyPath")}\\genesis.swacl.0.dll";
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
            solvedep func = Marshal.GetDelegateForFunctionPointer<solvedep>(_func);
            result = func(__strarrtoptr(LoadedMod.Keys.ToArray(), out char* __modbuf), __getmodkvps(out char* __kvpbuf, out int kvpcount), LoadedMod.Keys.Count, kvpcount);

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

            //global buffer made cleanup easy
            Marshal.FreeHGlobal((IntPtr)__modbuf);
            Marshal.FreeHGlobal((IntPtr)__kvpbuf);
            Marshal.FreeHGlobal((IntPtr)result->__global_buf);

            return;
        }
    }
}

//fun fact: half of this source file are for the interop.