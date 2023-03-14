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
            Name = "Old Test Model",
            Description = "This is an old test model that needs to be changes.\n"
                + "In the newer version, some words, letters, or whole paragraphs might\n"
                + "be changed.\n\n"
                + "Kapish?",
            Permissions = TestModelFlagsEnum.Read | TestModelFlagsEnum.Execute,
            FavoriteNumber = TestModelEnum.Three,
        };

        TestModel newVersion = oldVersion.CreateNewVersion();
        newVersion.Name = newVersion.Name
            .Replace("Old", "New")
            .Replace("Model", "model");

        newVersion.Description = newVersion.Description?
            .Replace("old", "new")
            .Replace("might", "may");

        newVersion.Permissions = TestModelFlagsEnum.Read | TestModelFlagsEnum.Write;
        newVersion.FavoriteNumber = TestModelEnum.Two;

        var diffs = Differentiator<TestModel>.GetDifferences(oldVersion, newVersion);

        var nameDiffs = diffs.GetMemberDifferences(_ => _.Name);
        Assert.Equal(4, nameDiffs.OldVersionSegments.Count);

        var firstDiff = nameDiffs.OldVersionSegments[0];
        var secondDiff = nameDiffs.OldVersionSegments[1];
        var thirdDiff = nameDiffs.OldVersionSegments[2];
        var fourthDiff = nameDiffs.OldVersionSegments[3];

        Assert.Equal("Old", firstDiff.Text);
        Assert.Equal(DifferenceType.Deletion, firstDiff.Operation);

        Assert.Equal(" Test ", secondDiff.Text);
        Assert.Equal(DifferenceType.Similar, secondDiff.Operation);

        Assert.Equal("M", thirdDiff.Text);
        Assert.Equal(DifferenceType.Deletion, thirdDiff.Operation);

        Assert.Equal("odel", fourthDiff.Text);
        Assert.Equal(DifferenceType.Similar, fourthDiff.Operation);

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