using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SimpleJSON;
using UnityEngine;

namespace Genesis.ContentLoader;
/// <summary>
/// A more fine-grained counterpart compared to simple patch, parsed altogether with the array and destroys itself after completion.
/// </summary>
public class JSONOperation
{
    /// <summary>
    /// Opcode to fine-grain control where to insert json.
    /// </summary>
    public enum JSONOpcode
    {
        /// <summary>
        /// Add a json node to the designated path.
        /// </summary>
        Add = 0,
        /// <summary>
        /// Remove a json node of the designated path.
        /// </summary>
        Remove = 1,
        /// <summary>
        /// Replace a json node of the designated path with another json node, and add if it doesn't exist.
        /// </summary>
        Replace = 2
    }
    private JSONOpcode opcode = JSONOpcode.Replace;
    /// <summary>
    /// Opcode of the 
    /// </summary>
    public JSONOpcode Opcode => opcode;

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
    /// 
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
    /// Converts all 
    /// </summary>
    /// <returns></returns>
    public override string ToString()
    {
        return $"{modName}({ModID}) {version}\nauthor: {author}\ndescription: {description}\nmod dependency:[{string.Join(", ", dependency)}]";
    }
}
internal class JsonUtil
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Merge(JSONArray target, JSONArray toMerge)
    {
        foreach (JSONNode item in toMerge)
            target.Add(item);
    }
}
/// <summary>
/// Parse and load json into the game.
/// </summary>
public class JsonLoader
{
    /// <summary>
    /// Currently loaded mod, in the form of KVP of modID and ModInfo.
    /// </summary>
    public static Dictionary<string, ModInfo> LoadedMod = new Dictionary<string, ModInfo>();
    /// <summary>
    /// Currently loaded tables, in the form of KVP of table name and JsonArray.
    /// </summary>
    public static Dictionary<string, JSONArray> LoadedJson = new Dictionary<string, JSONArray>();
    /// <summary>
    /// Load json mod and modinfo from the path provided in the parameter into LoadedMod and LoadedJson.
    /// </summary>
    /// <param name="path">The path of the mod.</param>
    public static void TryLoadMod(string path)
    {
        ModInfo info = null;
        //ModInfo verification
        if (!File.Exists($"{path}/ModInfo.json"))
            return;
        try
        {
            info = new ModInfo(JSON.Parse(File.ReadAllText($"{path}/ModInfo.json")));
        }
        catch (Exception)
        {
            Util.LogString("ContentLoader", $"{path} has malformed modinfo.json, skipping", InfoType.Warning);
            return;
        }
        if (LoadedMod.ContainsKey(info.ModID))
        {
            Util.LogString("ContentLoader", $"{path} has same modID as another loaded mod, skipping", InfoType.Warning);
            return;
        }
        LoadedMod.Add(info.ModID, info);

        if (Directory.Exists($"{path}/Contents"))
            foreach (string item in Directory.GetFiles($"{path}/Contents", "*.json", SearchOption.AllDirectories))
                LoadJson(item);

        List<Task> tasks = new List<Task>();
        if (Directory.Exists($"{path}/Assets"))
            foreach (string item in Directory.GetFiles($"{path}/Assets", "*.*", SearchOption.AllDirectories))
                tasks.Add(Task.Run(async () => AssetBundle.LoadFromFileAsync(item)));
        Task isComplete = Task.WhenAll(tasks);
        Util.LogString("ContentLoader", $"\n{info}");
    }

    /// <summary>
    /// Loads the json file provided from the path.
    /// </summary>
    /// <param name="path"></param>
    public static void LoadJson(string path)
    {
        JSONNode json = JSON.Parse(File.ReadAllText(path));
        string tbName = json["tbname"];
        string name = string.IsNullOrWhiteSpace(tbName) ? Path.GetFileNameWithoutExtension(path) : tbName;
        if (!LoadedJson.ContainsKey(name))
            LoadedJson[name] = new JSONArray();
        JSONArray payload = string.IsNullOrWhiteSpace(tbName) ? json.AsArray : json["payload"].AsArray;
        JsonUtil.Merge(LoadedJson[name], payload);
    }
}

/// <summary>
/// Custom exception for JSON parsing exceptions.
/// </summary>
[Serializable]
public class JsonParseException : Exception
{
#pragma warning disable 1591
    public JsonParseException() : base() { }
    public JsonParseException(string message) : base(message) { }
    public JsonParseException(string message, Exception inner) : base(message, inner) { }
    protected JsonParseException(System.Runtime.Serialization.SerializationInfo info,
                                System.Runtime.Serialization.StreamingContext context)
        : base(info, context) { }
}