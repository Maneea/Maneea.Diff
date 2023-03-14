using System.Reflection;

namespace Maneea.Diff.Extensions;

internal static class TypeExtension
{
    public static IEnumerable<PropertyInfo> GetDifferentiableProperties(this Type type)
        => type
        .GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .Where(p => p
        .GetCustomAttributes(typeof(CheckDifferenceAttribute), true)
        .Any());

    public static IEnumerable<FieldInfo> GetDifferentiableFields(this Type type)
        => type
        .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
        .Where(p => p
        .GetCustomAttributes(typeof(CheckDifferenceAttribute), true)
        .Any());
}