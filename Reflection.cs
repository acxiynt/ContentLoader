using System;
using System.Linq;
using System.Reflection;
using Genesis;

/// <summary>
/// Some reflection utils
/// </summary>
public static class ReflectionHelper
{

    /// <summary>
    /// Get method based on the type generic passed and returns the method found. If not, warns and return null.
    /// </summary>
    /// <typeparam name="T">The type where the method is contained.</typeparam>
    /// <param name="name">Name of the method.</param>
    /// <returns>Info of the method, could be null.</returns>
    public static MethodInfo GetMethod<T>(string name)
    {
        MethodInfo info = typeof(T).GetMethod(name, (BindingFlags)0x7F);
        if (info == null)
        {
            Util.LogString("ContentLoader", $"Failed to find method {name}.");
            return null;
        }
        return info;
    }

    /// <summary>
    /// Get property field based on the type generic passed and returns the property's field found. If not, warns and return null.
    /// </summary>
    /// <typeparam name="T">The type where the property is contained.</typeparam>
    /// <param name="name">Name of the property.</param>
    /// <returns>Info of the field, could be null.</returns>
    public static FieldInfo GetPropertyField<T>(string name)
    {
        FieldInfo info = typeof(T).GetField($"<{name}>k__BackingField", (BindingFlags)0x7C);
        if (info == null)
        {
            Util.LogString("ContentLoader", $"Failed to find property field {name}.");
            return null;
        }
        return info;
    }
    /// <summary>
    /// Get field based on the type generic passed and returns the field found. If not, warns and return null.
    /// </summary>
    /// <typeparam name="T">The type where the field is contained.</typeparam>
    /// <param name="name">Name of the field.</param>
    /// <returns>Info of the field, could be null.</returns>
    public static FieldInfo GetField<T>(string name)
    {
        FieldInfo info = typeof(T).GetField(name, (BindingFlags)0x7C);
        if (info == null)
        {
            Util.LogString("ContentLoader", $"Failed to find field {name}.");
            return null;
        }
        return info;
    }
    /// <summary>
    /// Get constructor based on the type generic passed and returns the constructor found. If not, warns and return null.
    /// </summary>
    /// <typeparam name="T">The constructor's type</typeparam>
    /// <param name="isStatic">Is the target constructor static or not, default: false.</param>
    /// <param name="types">The type of params to be passed into the constructor, default: new Type[0].</param>
    /// <param name="pmod">The parameter modifier, use if a param of the constructor have any special keyword before it.</param>
    /// <returns>Info of the constructor, could be null.</returns>
    public static ConstructorInfo GetCtor<T>(bool isStatic = false, Type[] types = null, ParameterModifier[] pmod = null)
    {
        //fast return if its the only constructor
        ConstructorInfo[] infos = typeof(T).GetConstructors();
        if (infos == null)
        {
            Util.LogString("ContentLoader", $"{typeof(T)} does not have a constructor.");
            return null;
        }
        if (types == null)
            return typeof(T).GetConstructors()[0];
        pmod = (pmod == null) ? new ParameterModifier[0] : pmod;
        byte flags = isStatic ? (byte)0x78 : (byte)0x74;
        ConstructorInfo info = typeof(T).GetConstructor((BindingFlags)flags, Type.DefaultBinder, types, pmod);
        if (info == null)
        {
            Util.LogString("ContentLoader", $"Failed to find constructor in {typeof(T)} with types of [{string.Join(", ", types.Select(s => s.ToString()))}].");
            return null;
        }
        return info;
    }
}