using System;
using System.Reflection;
using Xunit;
using Xunit.Abstractions;

namespace Maneea.Diff.Tests;

public class UnitTest1
{
    private readonly ITestOutputHelper Output;

    public UnitTest1(ITestOutputHelper output)
    {
        Output = output;
    }

    [Fact]
    public void Test1()
    {
        TestModel oldVersion = new()
        {
            Name = "مهمة مهمة",
            Description = "This is an old test model that needs to be changes.\n"
                + "In the newer version, some words, letters, or whole paragraphs might\n"
                + "be changed.\n\n"
                + "Kapish?",
            Permissions = TestModelFlagsEnum.Read | TestModelFlagsEnum.Execute,
            FavoriteNumber = TestModelEnum.Three,
        };

        TestModel newVersion = oldVersion.CreateNewVersion();
        newVersion.Name = "مهمة بالغة الأهمية";
        newVersion.Description = newVersion.Description?
            .Replace("old", "new")
            .Replace("might", "may");

        newVersion.Permissions = TestModelFlagsEnum.Read | TestModelFlagsEnum.Write;
        newVersion.FavoriteNumber = TestModelEnum.Two;

        var diffs = Differentiator<TestModel>.GetDifferences(oldVersion, newVersion);

        var nameDiffs = diffs.GetMemberDifferences(_ => _.Name);
        Output.WriteLine("OLD:");
        foreach (var item in nameDiffs.OldVersionSegments)
            Output.WriteLine($"[{(item.Operation == DifferenceType.Deletion ? '-':' ')}]{item.Text}");

        Output.WriteLine("NEW:");
        foreach (var item in nameDiffs.NewVersionSegments)
            Output.WriteLine($"[{(item.Operation == DifferenceType.Insertion ? '+' : ' ')}]{item.Text}");

        foreach (var diff in diffs)
        {
            if (diff is null) continue;
            if (diff.IsEnum)
            {
                Output.WriteLine(diff.MemberInfo.Name);
                var memberType = diff.MemberType!;
                var memberText = diff.OldVersionText;
                string enumDisplayName = string.Empty;

                var enumValue = Enum.Parse(memberType, memberText);
                if (enumValue is not null && enumValue is Enum materializedEnumValue)
                    enumDisplayName = materializedEnumValue.GetDisplayName() ?? string.Empty;


                Output.WriteLine($"Display Name: '{enumDisplayName}'");
            }
            else
                Output.WriteLine($"X ------ {diff.MemberInfo.Name}");
        }
    }
}