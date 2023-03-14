namespace Maneea.Diff;
public class MemberDifferenceSegment
{
    public DifferenceType Operation;
    public string Text;
    public MemberDifferenceSegment(DifferenceType operation, string text)
    {
        Operation = operation;
        Text = text;
    }

    public override bool Equals(object? obj)
    {
        if (obj == null)
            return false;

        MemberDifferenceSegment? p = obj as MemberDifferenceSegment;
        if (p == null)
            return false;

        return p.Operation == this.Operation && p.Text == this.Text;
    }

    public bool Equals(MemberDifferenceSegment obj)
    {
        if (obj == null)
            return false;

        return obj.Operation == this.Operation && obj.Text == this.Text;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode() ^ Operation.GetHashCode();
    }
}
