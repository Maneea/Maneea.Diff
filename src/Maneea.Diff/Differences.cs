using System.Collections;
using System.Linq.Expressions;
using System.Reflection;

namespace Maneea.Diff;
public class Differences<T> : IEnumerable<MemberDifferences> where T : class
{
    private IDictionary<string, MemberDifferences> _membersDictionary { get; set; }
    public Differences()
    {
        _membersDictionary = new Dictionary<string, MemberDifferences>();
    }

    public MemberDifferences GetMemberDifferences(Expression<Func<T, object?>> member)
    {
        var memberExpression = (MemberExpression)member.Body;
        if (memberExpression == null)
            throw new ArgumentException();

        var memberName = memberExpression.Member.Name;
        return GetMemberDifferences(memberName);
    }

    public MemberDifferences GetMemberDifferences(string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
            throw new ArgumentException();

        if (!_membersDictionary.ContainsKey(memberName))
            throw new ArgumentException();

        return _membersDictionary[memberName];
    }

    internal void AddMemberDifferences(MemberInfo memberInfo, string? oldVersion, string? newVersion)
    {
        if (string.IsNullOrEmpty(memberInfo.Name))
            throw new ArgumentException();

        else if (_membersDictionary.ContainsKey(memberInfo.Name))
            throw new ArgumentException();

        var memberDifferences = new MemberDifferences(memberInfo, oldVersion, newVersion);
        _membersDictionary.Add(memberInfo.Name, memberDifferences);
    }

    public IEnumerator<MemberDifferences> GetEnumerator() => _membersDictionary.Values.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
