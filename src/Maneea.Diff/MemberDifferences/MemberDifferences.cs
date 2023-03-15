
using System.Reflection;

namespace Maneea.Diff;
public class MemberDifferences : IEquatable<MemberDifferences>
{
    public IList<MemberDifferenceSegment> OldVersionSegments { get; init; }
    public IList<MemberDifferenceSegment> NewVersionSegments { get; init; }
    public IList<MemberDifferenceSegment> AllSegments { get; init; }

    public MemberInfo MemberInfo { get; init; }

    public string OldVersionText => string.Join(string.Empty, OldVersionSegments.Select(s => s.Text));
    public string NewVersionText=> string.Join(string.Empty, NewVersionSegments.Select(s => s.Text));

    public Type? MemberType
    {
        get
        {
            if (MemberInfo.MemberType == MemberTypes.Property && MemberInfo is PropertyInfo propInfo)
                return propInfo.PropertyType;
            else if (MemberInfo.MemberType == MemberTypes.Field && MemberInfo is FieldInfo fieldInfo)
                return fieldInfo.FieldType;
            return Type.GetType(MemberInfo.Name);
        }
    }
    public bool IsEnum
    {
        get
        {
            if (MemberInfo.MemberType == MemberTypes.Property && MemberInfo is PropertyInfo propInfo)
                return propInfo.PropertyType.IsEnum;
            else if (MemberInfo.MemberType == MemberTypes.Field && MemberInfo is FieldInfo fieldInfo)
                return fieldInfo.FieldType.IsEnum;
            return false;
        }
    }

    public MemberDifferences(MemberInfo memberInfo, string? oldVersion, string? newVersion)
    {
        MemberInfo = memberInfo;
        var differ = new InternalDiffer();
        // TODO: Provide a way to change the configuration values of InternalDiffer.

        var internalDiffs = differ.GetTextDifferences(oldVersion, newVersion);
        differ.diff_cleanupEfficiency(internalDiffs);

        OldVersionSegments = new List<MemberDifferenceSegment>();
        NewVersionSegments = new List<MemberDifferenceSegment>();
        AllSegments = new List<MemberDifferenceSegment>();
        foreach (var internalDiff in internalDiffs)
        {
            DifferenceType type;
            if (internalDiff.Operation == Operation.INSERT)
                type = DifferenceType.Insertion;
            else if (internalDiff.Operation == Operation.DELETE)
                type = DifferenceType.Deletion;
            else
                type = DifferenceType.Similar;
            var diffSegment = new MemberDifferenceSegment(type, internalDiff.Text);

            AllSegments.Add(diffSegment);
            if (type != DifferenceType.Deletion)
                NewVersionSegments.Add(diffSegment);
            if (type != DifferenceType.Insertion)
                OldVersionSegments.Add(diffSegment);
        }
    }

    public bool Equals(MemberDifferences? other)
    {
        if (OldVersionSegments.Count != NewVersionSegments.Count)
            return false;
        for (int i = 0; i < OldVersionSegments.Count; i++)
        {
            if (!OldVersionSegments[i].Equals(NewVersionSegments[i]))
                return false;
        }
        return true;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;
        else if (obj is not MemberDifferences)
            return false;
        else
            return this.Equals(obj as MemberDifferences);
    }

    public override int GetHashCode()
    {
        return OldVersionSegments.GetHashCode() ^ NewVersionSegments.GetHashCode();
    }


}

