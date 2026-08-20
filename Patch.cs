using System;
using System.Collections.Generic;
using System.Reflection;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using static Mono.Cecil.Cil.OpCodes;
using Mono.Cecil.Cil;
using System.Linq;
namespace Genesis.ContentLoader
{



    /// <summary>
    /// Similar to harmony patch, but without the bloat.<br/>
    /// Because this is just a simple monomod wrapper, might have unpredicted result when multiple patch instances applied on a single method.
    /// </summary>
    public class Patch
    {
        private ILHook self;
        private RuntimeMethodHandle handle;
        private static object obj = new object();
        private static Action<ILCursor> transpiler;

        //used to store prefix and postfix for the patch to read, [0]: prefix, [1]: postfix
        internal static MethodBase[] storage = new MethodBase[2];

        /// <summary>
        /// Initializes a patch to the method with either prefix, postfix, or transpiler(or all of them).<br/>
        /// The method must have the parameters in its original order.<br/>
        /// If you need its instance, have the first parameter as the original method's declaring type, then the rest of parameters you need.<br/>
        /// Example:<br/>
        /// A method with signature of [bool, int], prefix can be [bool], [int], but it CANNOT be [int, bool].
        /// </summary>
        /// <param name="original">The method base of original method.</param>
        /// <param name="prefix">The method base of the prefix.</param>
        /// <param name="postfix">The method base of the postfix.</param>
        /// <param name="transpiler">The method instance for transpiler.</param>
        /// <param name="replace">If true, adds a extra ret after prefix so it replaces original logic.</param>
        /// <exception cref="ArgumentNullException">Throws when both prefix and postfix is null.</exception>
        public Patch(MethodBase original, MethodBase prefix = null, MethodBase postfix = null, Action<ILCursor> transpiler = null, bool replace = false)
        {
            lock (obj)
            {
                if (original == null)
                    throw new ArgumentNullException("Original method is null.");
                if (postfix == null && prefix == null && transpiler == null)
                    throw new ArgumentNullException("Cannot create patch with no method specificed.");

                Patch.transpiler = transpiler;
                storage = new MethodBase[] { prefix, postfix };
                handle = original.MethodHandle;
                CurMethod.param = original.GetParameters();

                CurMethod.thistype = (!original.IsStatic) ? original.DeclaringType : null;
                CurMethod.name = original.Name;
                CurMethod.replace = replace;

                if (original is MethodInfo method)
                    CurMethod.@return = method.ReturnType;
                else
                {
                    CurMethod.name = ((ConstructorInfo)original).Verbose();
                    CurMethod.@return = typeof(void);
                }
                self = new ILHook(original, __patch);
            }
        }



        /// <summary>
        /// Undone the patch applied.
        /// </summary>
        public void Destroy()
        {
            self.Dispose();
        }



        /// <summary>
        /// Ensure patch is undone when GC'd.
        /// </summary>
        ~Patch()
        {
            self.Dispose();
        }



        /// <summary>
        /// Stores the patch into PatchInfo class for other mod to access when needed.
        /// </summary>
        public void AddPatch()
        {
            PatchInfo.addPatch(handle, this);
        }



        private static void __patch(ILContext il)
        {
            ILCursor cur = new ILCursor(il);
            if (CurMethod.replace && (CurMethod.@return == ((MethodInfo)storage[0]).ReturnType) && storage[0] != null)
            {
                cur.Index = 0;
                __ldfx(cur, storage[0]);
                cur.Emit(Call, storage[0]);
                cur.Emit(Ret);
            }
            else if (CurMethod.replace) throw new Exception("Return type must be matching in order to replace");

            transpiler?.Invoke(cur);
            cur.Index = 0;
            //prefix
            if (storage[0] != null && !CurMethod.replace)
            {
                __ldfx(cur, storage[0]);
                cur.Emit(Call, storage[0]);
            }

            //postfix
            if (storage[1] != null)
            {
                cur.Index = cur.Instrs.Count - 1;
                cur.Remove();
                __ldfx(cur, storage[1]);
                cur.Emit(Call, storage[1]);
                cur.Emit(Ret);
            }

#if DEBUG
            __dumpil(cur.Context);
#endif
        }



        private static void __ldfx(ILCursor cur, MethodBase method)
        {
            if (method == null)
            {
                Util.LogString("ContentLoader", "Method is null", InfoType.Error);
                return;
            }

            List<ParameterInfo> pms = method.GetParameters().ToList();
            if (pms.Count == 0)
                return;
            byte plen = (byte)pms.Count;
            byte plen2 = (byte)(CurMethod.thistype != null ? CurMethod.param.Length + 1 : CurMethod.param.Length);

            if (plen > plen2)
                throw new Exception($"Parameter count does not match up, method: {method.Name}, original: {CurMethod.name}");

            byte extra = 0;

            if (pms.Count == 0)
                return;

            if (pms.Count != 0 && CurMethod.thistype == pms[0].ParameterType)
            {
                cur.Emit(Ldarg_0);
                extra++;
                pms.RemoveAt(0);
            }

            byte b = 0;
#if DEBUG
            __dumppm(pms.ToArray());
#endif
            for (byte a = 0; a < CurMethod.param.Length; a++)
            {
                if (b < pms.Count)
                {
                    Type curtype = CurMethod.param[a].ParameterType;
                    Type pmtype = pms[b].ParameterType;
                    bool ispmref = pmtype.IsByRef;
                    bool iscurref = curtype.IsByRef;
                    if (curtype.IsByRef) curtype = curtype.GetElementType();
                    if (ispmref) pmtype = pmtype.GetElementType();
                    if (curtype == pmtype)
                    {
                        b++;
                        if (ispmref && !iscurref)
                            cur.Emit(Ldarga, a + extra);
                        else
                            cur.Emit(Ldarg, a + extra);
                        continue;
                    }
                }
            }
        }



        // provides a insight of what IL is generated from the top so i can have less headache debugging
#if DEBUG
        private static void __dumpil(ILContext il)
        {
            Util.LogString("ContentLoader", $"Metadata: prefix:{storage[0]?.Name}, postfix:{storage[1]?.Name}, contains transpiler:{transpiler != null}, replace:{CurMethod.replace}");
            Util.LogString("ContentLoader", "Begin IL dump section--------");
            foreach (Instruction ins in il.Instrs)
                Util.LogString("ContentLoader", $"IL_{ins.Offset:X4}|{ins.OpCode}: {ins.Operand}");
            Util.LogString("ContentLoader", "End IL dump section----------");
        }

        //dumps parameters
        private static void __dumppm(ParameterInfo[] pms)
        {
            Util.LogString("ContentLoader", $"Metadata: prefix:{storage[0]?.Name}, postfix:{storage[1]?.Name}, contains transpiler:{transpiler != null}, replace:{CurMethod.replace}");
            Util.LogString("ContentLoader", "Begin parameters dump section--------");
            foreach (ParameterInfo pm in pms)
                Util.LogString("ContentLoader", $"{pm.ParameterType}, is ref: {pm.ParameterType.IsByRef}");
            Util.LogString("ContentLoader", "End parameters dump section----------");
        }
#endif



        /// <summary>
        /// Used to track patches, unlike harmony, there is no mod/instance names, just runtime handle and their corresponding patches.
        /// </summary>
        public static class PatchInfo
        {
            internal static Dictionary<RuntimeMethodHandle, HashSet<Patch>> patches = new Dictionary<RuntimeMethodHandle, HashSet<Patch>>();




            /// <summary>
            /// Obtains the dictionary of currently applied patches.
            /// </summary>
            public static Dictionary<RuntimeMethodHandle, HashSet<Patch>> Patches => patches;



            internal static void addPatch(RuntimeMethodHandle handle, Patch patch)
            {
                if (!patches.ContainsKey(handle))
                    patches.Add(handle, new HashSet<Patch>());
                patches[handle].Add(patch);
            }
        }



        internal static class CurMethod
        {
            internal static ParameterInfo[] param;
            internal static Type thistype;
            internal static string name;
            internal static Type @return;
            internal static bool replace;
        }
    }
}