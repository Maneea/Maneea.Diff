using Maneea.Diff.Converters;
using Maneea.Diff.Extensions;

namespace Maneea.Diff;
public static class Differentiator<T> where T : class
{
    private static Dictionary<Type, IConverter> TypeStringers { get; } = new();
    public static Differences<T> GetDifferences(T oldVersion, T newVersion)
    {
        var type = oldVersion.GetType();
        if (newVersion.GetType() != type)
            throw new InvalidOperationException("both types must be similar to get the differences");

        var differences = new Differences<T>();

        foreach (var property in type.GetDifferentiableProperties())
        {
            var oldValue = property.GetValue(oldVersion);
            var newValue = property.GetValue(newVersion);

            var oldString = GetConverter(property.PropertyType).ToDifferenceString(oldValue);
            var newString = GetConverter(property.PropertyType).ToDifferenceString(newValue);

            differences.AddMemberDifferences(property, oldString, newString);
        }

        foreach (var field in type.GetDifferentiableFields())
        {
            var oldValue = field.GetValue(oldVersion);
            var newValue = field.GetValue(newVersion);

            var oldString = GetConverter(field.FieldType).ToDifferenceString(oldValue);
            var newString = GetConverter(field.FieldType).ToDifferenceString(newValue);

            differences.AddMemberDifferences(field, oldString, newString);
        }
        return differences;
    }

    public static void AddTypeConverter(Type type, IConverter converter)
    {
        if (TypeStringers.ContainsKey(type))
            throw new InvalidOperationException($"converter for {type.Name} already exists");
        TypeStringers[type] = converter;
    }

    private static IConverter GetConverter(Type type)
    {
        if (TypeStringers.ContainsKey(type))
            return TypeStringers[type];

        // TODO: Add a default converter for IEnumerable types
        // TODO: Add a default converter for Enums
        return new DefaultConverter();
    }
}
