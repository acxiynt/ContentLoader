using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using SimpleJSON;

namespace Genesis.ContentLoader
{
    /// <summary>
    /// A more fine-grained counterpart compared to simple patch, parsed altogether with the array and destroys itself after completion.
    /// </summary>
    public class JSONOperation
    {
        /// <summary>
        /// Creates a json operation.
        /// </summary>
        /// <param name="tableName">The table to be operated.</param>
        /// <param name="assetPath">The path to be operated.</param>
        /// <param name="opcode">The type of operation to be performed.</param>
        public JSONOperation(string tableName, string assetPath, JSONOpcode opcode = JSONOpcode.Replace)
        {
            this.tableName = tableName;
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
        private string tableName;
        private string assetPath;
        /// <summary>
        /// Opcode of the operation.
        /// </summary>
        public JSONOpcode Opcode => opcode;
        /// <summary>
        /// The json to be parsed.
        /// </summary>
        public JSONNode TableName => tableName;
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
    //__<func> is for internal call that should be in one method but separated for code clarity
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
            if (ModLoader.LoadedMod.ContainsKey(info.ModID))
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
            return !(node["op"].Value.IsNullOrEmpty() || node["assetpath"].Value.IsNullOrEmpty() || node["tbname"].Value.IsNullOrEmpty());
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool __istb(JSONNode node)
        {
            return !node["tbname"].Value.IsNullOrEmpty();
        }
        //loads up json file then parse the whole json into one or multiple JSONNode.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static List<JSONNode> __ldjson(string path)
        {
            JSONNode json = JSON.Parse(File.ReadAllText(path));
            if (json.IsArray && __islegacy(json.AsArray))
            {
                JSONNode node = JSON.Parse($"{{\"tbname\": \"{Path.GetFileNameWithoutExtension(path)}\"}}");
                node.Add("payload", json);
                return new List<JSONNode>() { node };
            }
            List<JSONNode> list = new List<JSONNode>();
            if (json.IsArray && json.AsArray.Count > 1)
            {
                foreach (JSONNode node in json)
                    list.Add(node);
            }
            else if (json.IsArray && json.Count == 1)
                list.Add(json[0]);
            else if (!json.IsArray)
                list.Add(json);
            return list;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool __resolvenodes(List<JSONNode> nodes, out Dictionary<string, JSONArray> tb, out Dictionary<string, JSONOperation> op, string path)
        {
            tb = new Dictionary<string, JSONArray>();
            op = new Dictionary<string, JSONOperation>();
            for (int i = 0; i < nodes.Count; i++)
            {
                JSONNode node = nodes[i];
                if (__istb(node))
                    tb.Add(__ldtb(node));
                else if (__isop(node))
                {
                    KeyValuePair<string, JSONOperation> pair = __ldop(node, path, i, out bool error);
                    if (!error)
                        op.Add(pair);
                }
                else
                    Util.LogString("ContentLoader", $"Error parsing node {i} in {path}: not a table or operation, skipping", InfoType.Warning);
            }
            return true;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static KeyValuePair<string, JSONArray> __ldtb(JSONNode node)
        {
            return new KeyValuePair<string, JSONArray>(node["tbname"].Value, node["payload"].AsArray);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static KeyValuePair<string, JSONOperation> __ldop(JSONNode node, string path, int i, out bool error)
        {
            error = false;
            JSONOperation.JSONOpcode opcode = JSONOperation.JSONOpcode.Replace;
            switch (node["op"].Value)
            {
                case "add":
                    opcode = JSONOperation.JSONOpcode.Add;
                    break;
                case "remove":
                    opcode = JSONOperation.JSONOpcode.Remove;
                    break;
                case "replace":
                    opcode = JSONOperation.JSONOpcode.Replace;
                    break;
                default:
                    Util.LogString("ContentLoader", $"Error parsing node {i} in {path}: have unimplemented opcode, skipping", InfoType.Warning);
                    error = true;
                    break;
            }
            return new KeyValuePair<string, JSONOperation>(node["tbname"].Value, new JSONOperation(node["tbname"].Value, node["assetpath"].Value, opcode));
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
}
//Yoruno Sakura my beloved