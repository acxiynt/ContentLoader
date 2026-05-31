using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SimpleJSON;

namespace Genesis.ContentLoader;
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

        dependency = info["Dependency"].AsStringList ?? [];
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
/// A container to pack up information of mods, 
/// </summary>
public class Mod
{
    private ModInfo info;
    private Dictionary<string, JSONArray> table;
    private Dictionary<string, JSONOperation> operations = [];
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
        if (obj.GetType() == typeof(Mod) && ((Mod)obj).info.ModID == info.ModID)
            return true;
        return false;
    }
}
/// <summary>
/// Loads json and asset package into mod, supports hot reload and mod disabling.
/// </summary>
public static class ModLoader
{
    /// <summary>
    /// Currently loaded mod, in KVP of ModInfo and parsed json table.
    /// </summary>
    public static List<Mod> LoadedMod = new List<Mod>();
    /// <summary>
    /// Mods that requires other mod as dependency to run, will resolve later.
    /// </summary>
    public static List<Mod> ModWithDependency = new List<Mod>();
    internal static void Start()
    {

    }
    /// <summary>
    /// Hot reload and rediscover mod for future usage.
    /// </summary>
    public static void Reload()
    {
    }

    internal static void TryLoadMod(string path)
    {
        ModInfo info = JsonLoader.__ldinfo(path);
        if (info == null)
            return;
        List<JSONNode> nodes;
        Dictionary<string, JSONArray> tb = new Dictionary<string, JSONArray>();
        if (Directory.Exists($"{path}/Contents"))
            foreach (string item in Directory.GetFiles($"{path}/Contents", "*.json", SearchOption.AllDirectories))
            {

            }
    }
}