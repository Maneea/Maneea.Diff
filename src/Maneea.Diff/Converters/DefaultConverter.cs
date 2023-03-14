namespace Maneea.Diff.Converters;
internal class DefaultConverter : IConverter
{
    public string? ToDifferenceString(object? member)
        => member != null ? member?.ToString() : string.Empty;
}
