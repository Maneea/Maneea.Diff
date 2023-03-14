namespace Maneea.Diff.Tests;
public class TestModel : ICloneable
{
    /// <summary>
    /// A string non-nullable string property
    /// </summary>
    [CheckDifference]
    public string Name {get; set;}
    /// <summary>
    /// A nullable string property
    /// </summary>
    [CheckDifference]
    public string? Description {get; set;}
    /// <summary>
    /// A non-nullable string field
    /// </summary>
    public string Notes;
    
    [CheckDifference]
    public TestModelEnum FavoriteNumber {get; set;}
    [CheckDifference]
    public TestModelFlagsEnum Permissions {get; set;}
    

    public TestModel()
    {
        Name = string.Empty;
        Notes = string.Empty;
    }

    public object Clone() => new TestModel()
    {
        Name = Name,
        Description = Description,
        Notes = Notes,
        FavoriteNumber = FavoriteNumber,
        Permissions = Permissions,
    };

    public TestModel CreateNewVersion()
    {
        TestModel newVersion = (TestModel)Clone();
        return newVersion;
    }
}