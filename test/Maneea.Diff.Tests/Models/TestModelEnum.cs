using System.ComponentModel.DataAnnotations;

namespace Maneea.Diff.Tests;
public enum TestModelEnum
{
    [Display(Name = nameof(Resources.None), ResourceType = typeof(Resources))]
    None,
    [Display(Name = nameof(Resources.One), ResourceType = typeof(Resources))]
    One,
    [Display(Name = nameof(Resources.Two), ResourceType = typeof(Resources))]
    Two,
    [Display(Name = nameof(Resources.Three), ResourceType = typeof(Resources))]
    Three,
    [Display(Name = nameof(Resources.Four), ResourceType = typeof(Resources))]
    Four
}

[Flags]
public enum TestModelFlagsEnum
{
    [Display(Name = nameof(Resources.Read), ResourceType = typeof(Resources))]
    Read = 1 << 0,
    [Display(Name = nameof(Resources.Write), ResourceType = typeof(Resources))]
    Write = 1 << 1,
    [Display(Name = nameof(Resources.Execute), ResourceType = typeof(Resources))]
    Execute = 1 << 2,
}