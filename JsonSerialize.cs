using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using SimpleJSON;
using UnityEngine;

namespace Genesis.ContentLoader;

public class ModInfo
{
    private string modID;
    private string modName = "No Mod Name";
    private string version = "No Version";
    private string description = "No Description";
    private string author = "No Author";
    private string[] dependency;

    public string ModID => modID;
    public string ModName => modName;
    public string Version => version;
    public string Description => description;
    public string Author => author;
    public string[] Dependency => dependency;

    public ModInfo(JSONNode info)
    {
        modID = info["ModID"];
        if (string.IsNullOrWhiteSpace(modID))
            throw new Exception("ModInfo.json does not contain modID");

        string modName = info["ModName"];
        this.modName = string.IsNullOrWhiteSpace(modName) ? this.modName : modName;

        string version = info["Version"];
        this.version = string.IsNullOrWhiteSpace(version) ? this.version : version;

        string description = info["Description"];
        this.description = string.IsNullOrWhiteSpace(description) ? this.version : description;

        string author = info["Author"];
        this.author = string.IsNullOrWhiteSpace(author) ? this.author : author;

        dependency = info["Dependency"].AsStringArray ?? [];
    }

    public override string ToString()
    {
        return $"{modName}({ModID}) {version}\nauthor: {author}\ndescription: {description}\nmod dependency:[{string.Join(", ", dependency)}]";
    }
}
public class JsonUtil
{
    public static void Merge(JSONArray target, JSONArray toMerge)
    {
        foreach (JSONNode item in toMerge)
            target.Add(item);
    }
}
public class JsonLoader
{
    public static Dictionary<string, ModInfo> loadedMod = new Dictionary<string, ModInfo>();
    public static Dictionary<string, JSONArray> jsons = new Dictionary<string, JSONArray>();

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
        }
        if (loadedMod.ContainsKey(info.ModID))
        {
            Util.LogString("ContentLoader", $"{path} has same modID as another loaded mod, skipping", InfoType.Warning);
            return;
        }
        loadedMod.Add(info.ModID, info);

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

    public static void LoadJson(string path)
    {
        JSONNode json = JSON.Parse(File.ReadAllText(path));
        string tbName = json["tbname"];
        string name = string.IsNullOrWhiteSpace(tbName) ? Path.GetFileNameWithoutExtension(path) : tbName;
        if (!jsons.ContainsKey(name))
            jsons[name] = new JSONArray();
        JSONArray payload = string.IsNullOrWhiteSpace(tbName) ? json.AsArray : json["payload"].AsArray;
        JsonUtil.Merge(jsons[name], payload);
    }


}