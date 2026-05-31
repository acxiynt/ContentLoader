using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SimpleJSON;

namespace Genesis.ContentLoader;
/// <summary>
/// A more fine-grained counterpart compared to simple patch, parsed altogether with the array and destroys itself after completion.
/// </summary>
public class JSONOperation
{
    /// <summary>
    /// Creates a json operation.
    /// </summary>
    /// <param name="json">The json to be parsed.</param>
    /// <param name="assetPath">The path to be operated.</param>
    /// <param name="opcode">The type of operation to be performed.</param>
    public JSONOperation(JSONNode json, string assetPath, JSONOpcode opcode = JSONOpcode.Replace)
    {
        this.json = json;
        this.assetPath = assetPath;
        this.opcode = opcode;
    }
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
    private JSONOpcode opcode;
    private string json;
    private string assetPath;
    /// <summary>
    /// Opcode of the operation.
    /// </summary>
    public JSONOpcode Opcode => opcode;
    /// <summary>
    /// The json to be parsed.
    /// </summary>
    public JSONNode Json => json;
    /// <summary>
    /// The type of operation to be performed.
    /// </summary>
    public string AssetPath => assetPath;

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
internal static class JsonLoader
{
    //ModInfo verification, made into another method because its cleaner
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static ModInfo __ldinfo(string path)
    {
        ModInfo info = null;
        if (!File.Exists($"{path}/ModInfo.json"))
            goto skip;
        info = new ModInfo(JSON.Parse(File.ReadAllText($"{path}/ModInfo.json")));
        if (ModLoader.LoadedMod.Contains(info))
        {
            Util.LogString("ContentLoader", $"{path} has same modID as another loaded mod, skipping", InfoType.Warning);
            goto skip;
        }
        goto ret;
    skip:
        return null;
    ret:
        return info;
    }
    //will be removed for v1 support removal in the future solely on performance concerns
    [Obsolete]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool __islegacy(JSONArray arr)
    {
        foreach (JSONNode node in arr)
            if (__isop(node) || __istb(node))
                return false;
        return true;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool __isop(JSONNode node)
    {
        return !(node["op"].Value.IsNullOrEmpty() || node["assetpath"].Value.IsNullOrEmpty());
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static bool __istb(JSONNode node)
    {
        return !node["tbname"].Value.IsNullOrEmpty();
    }
    //loads up file then parse the whole file into one or multiple JSONNode.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static List<JSONNode> __ldjson(string path)
    {
        JSONNode json = JSON.Parse(path);
        if (json.IsArray && __islegacy(json.AsArray))
        {
            JSONNode node = JSON.Parse($"{{\"tbname\": \"{Path.GetFileNameWithoutExtension(path)}\"}}");
            node.Add("payload", json);
            return new List<JSONNode>() { node };
        }
        List<JSONNode> list = new List<JSONNode>();
        if (json.IsArray &&)
        {

            foreach (JSONNode node )
        }
        else if (json.IsArray && json.Count <= 1)
            list.Add(json);
        return list;
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
#pragma warning restore
}

//Yoruno Sakura my beloved