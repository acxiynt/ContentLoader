using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Genesis.ContentLoader
{
    //for interop with genesis.swacl.0
    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct __depresult
    {
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
    internal static class InteropHelper
    {
        internal static unsafe char** __strarrtoptr(string[] arr)
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
            char* __buf = (char*)Marshal.AllocHGlobal(size + (sizeof(IntPtr) * arr.Length));
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
        /// Main string / String pointer for mod strings / Dependency pointer pointing the second area / The kvps stored in pointer to a single memory(this is what this method returns)
        /// </summary>
        /// <param name="count">the number of elements inside the created pointer.</param>
        /// <returns>the pointer of initialized __mod_kvp*.</returns>
        internal static unsafe __mod_kvp* __getmodkvps(out int count)
        {
            //single malloc
            int size = 0;
            int charsize = sizeof(char);
            count = ModLoader.ModWithDependency.Count;

            //how many char** do we need to preallowcate for dependency (yes the deps is a char***)
            int depsize = 0;

            //how many space for char** do we need to preallowcate for dependency
            int deplen = 0;

            foreach (KeyValuePair<string, Mod> pair in ModLoader.ModWithDependency)
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
            char* __buf = (char*)Marshal.AllocHGlobal(size + deplen + depsize + (sizeof(__mod_kvp) * count));

            char*** deps = (char***)((byte*)__buf + size + deplen);

            __mod_kvp* kvps = (__mod_kvp*)((byte*)__buf + size + deplen + depsize);
            KeyValuePair<string, string[]>[] _kvps =
                ModLoader.ModWithDependency.Select(
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
    }
}