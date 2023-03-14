/*
 * Diff Match and Patch
 * Copyright 2018 The diff-match-patch Authors.
 * https://github.com/google/diff-match-patch
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Text;
using System.Text.RegularExpressions;

namespace Maneea.Diff;

internal static class CompatibilityExtensions
{
    // JScript splice function
    public static List<T> Splice<T>(this List<T> input, int start, int count,
        params T[] objects)
    {
        List<T> deletedRange = input.GetRange(start, count);
        input.RemoveRange(start, count);
        input.InsertRange(start, objects);

        return deletedRange;
    }

    // Java substring function
    public static string JavaSubstring(this string s, int begin, int end)
    {
        return s.Substring(begin, end - begin);
    }
}

/**-
 * The data structure representing a diff is a List of Diff objects:
 * {Diff(Operation.DELETE, "Hello"), Diff(Operation.INSERT, "Goodbye"),
 *  Diff(Operation.EQUAL, " world.")}
 * which means: delete "Hello", add "Goodbye" and keep " world."
 */
internal enum Operation
{
    DELETE, INSERT, EQUAL
}


/**
 * Class representing one diff Operation.
 */
internal class Diff
{
    public Operation Operation;
    // One of: INSERT, DELETE or EQUAL.
    public string Text;
    // The Text associated with this diff Operation.

    /**
     * Constructor.  Initializes the diff with the provided values.
     * @param Operation One of INSERT, DELETE or EQUAL.
     * @param Text The Text being applied.
     */
    public Diff(Operation operation, string text)
    {
        // Construct a diff with the specified Operation and Text.
        Operation = operation;
        Text = text;
    }

    /**
     * Display a human-readable version of this Diff.
     * @return Text version.
     */
    public override string ToString()
    {
        string prettyText = Text.Replace('\n', '\u00b6');
        return "Diff(" + Operation + ",\"" + prettyText + "\")";
    }

    /**
     * Is this Diff equivalent to another Diff?
     * @param d Another Diff to compare against.
     * @return true or false.
     */
    public override bool Equals(object? obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // If parameter cannot be cast to Diff return false.
        Diff? p = obj as Diff;
        if (p == null)
        {
            return false;
        }

        // Return true if the fields match.
        return p.Operation == Operation && p.Text == Text;
    }

    public bool Equals(Diff obj)
    {
        // If parameter is null return false.
        if (obj == null)
        {
            return false;
        }

        // Return true if the fields match.
        return obj.Operation == Operation && obj.Text == Text;
    }

    public override int GetHashCode()
    {
        return Text.GetHashCode() ^ Operation.GetHashCode();
    }
}


/**
 * Class containing the diff, match and patch methods.
 * Also Contains the behaviour settings.
 */
internal class InternalDiffer
{
    // Defaults.
    // Set these on your InternalDiffer instance to override the defaults.

    // Number of seconds to map a diff before giving up (0 for infinity).
    public float Diff_Timeout = 0.0f;
    // Cost of an empty edit Operation in terms of edit characters.
    public short Diff_EditCost = 4;
    // At what point is no match declared (0.0 = perfection, 1.0 = very loose).

    // Define some regex patterns for matching boundaries.
    private Regex BLANKLINEEND = new Regex("\\n\\r?\\n\\Z");
    private Regex BLANKLINESTART = new Regex("\\A\\r?\\n\\r?\\n");

    //  DIFF FUNCTIONS


    /**
     * Find the differences between two texts.
     * Run a faster, slightly less optimal diff.
     * This method allows the 'checklines' of GetTextDifferences() to be optional.
     * Most of the time checklines is wanted, so default to true.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @return List of Diff objects.
     */
    public List<Diff> GetTextDifferences(string? text1, string? text2)
    {
        if (text1 == null) text1 = string.Empty;
        if (text2 == null) text2 = string.Empty;
        return GetTextDifferences(text1, text2, true);
    }

    /**
     * Find the differences between two texts.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @return List of Diff objects.
     */
    public List<Diff> GetTextDifferences(string text1, string text2, bool checklines)
    {
        // Set a deadline by which time the diff must be complete.
        DateTime deadline;
        if (Diff_Timeout <= 0)
        {
            deadline = DateTime.MaxValue;
        }
        else
        {
            deadline = DateTime.Now +
                new TimeSpan((long)(Diff_Timeout * 1000) * 10000);
        }
        return GetTextDifferences(text1, text2, checklines, deadline);
    }

    /**
     * Find the differences between two texts.  Simplifies the problem by
     * stripping any common prefix or suffix off the texts before diffing.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param deadline Time when the diff should be complete by.  Used
     *     internally for recursive calls.  Users should set DiffTimeout
     *     instead.
     * @return List of Diff objects.
     */
    private List<Diff> GetTextDifferences(string text1, string text2, bool checklines,
        DateTime deadline)
    {
        // Check for null inputs not needed since null can't be passed in C#.

        // Check for equality (speedup).
        List<Diff> diffs;
        if (text1 == text2)
        {
            diffs = new List<Diff>();
            if (text1.Length != 0)
            {
                diffs.Add(new Diff(Operation.EQUAL, text1));
            }
            return diffs;
        }

        // Trim off common prefix (speedup).
        int commonlength = diff_commonPrefix(text1, text2);
        string commonprefix = text1.Substring(0, commonlength);
        text1 = text1.Substring(commonlength);
        text2 = text2.Substring(commonlength);

        // Trim off common suffix (speedup).
        commonlength = diff_commonSuffix(text1, text2);
        string commonsuffix = text1.Substring(text1.Length - commonlength);
        text1 = text1.Substring(0, text1.Length - commonlength);
        text2 = text2.Substring(0, text2.Length - commonlength);

        // Compute the diff on the middle block.
        diffs = diff_compute(text1, text2, checklines, deadline);

        // Restore the prefix and suffix.
        if (commonprefix.Length != 0)
        {
            diffs.Insert(0, new Diff(Operation.EQUAL, commonprefix));
        }
        if (commonsuffix.Length != 0)
        {
            diffs.Add(new Diff(Operation.EQUAL, commonsuffix));
        }

        diff_cleanupMerge(diffs);
        return diffs;
    }

    /**
     * Find the differences between two texts.  Assumes that the texts do not
     * have any common prefix or suffix.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param checklines Speedup flag.  If false, then don't run a
     *     line-level diff first to identify the changed areas.
     *     If true, then run a faster slightly less optimal diff.
     * @param deadline Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private List<Diff> diff_compute(string text1, string text2,
                                    bool checklines, DateTime deadline)
    {
        List<Diff> diffs = new List<Diff>();

        if (text1.Length == 0)
        {
            // Just add some Text (speedup).
            diffs.Add(new Diff(Operation.INSERT, text2));
            return diffs;
        }

        if (text2.Length == 0)
        {
            // Just delete some Text (speedup).
            diffs.Add(new Diff(Operation.DELETE, text1));
            return diffs;
        }

        string longtext = text1.Length > text2.Length ? text1 : text2;
        string shorttext = text1.Length > text2.Length ? text2 : text1;
        int i = longtext.IndexOf(shorttext, StringComparison.Ordinal);
        if (i != -1)
        {
            // Shorter Text is inside the longer Text (speedup).
            Operation op = text1.Length > text2.Length ?
                Operation.DELETE : Operation.INSERT;
            diffs.Add(new Diff(op, longtext.Substring(0, i)));
            diffs.Add(new Diff(Operation.EQUAL, shorttext));
            diffs.Add(new Diff(op, longtext.Substring(i + shorttext.Length)));
            return diffs;
        }

        if (shorttext.Length == 1)
        {
            // Single character string.
            // After the previous speedup, the character can't be an equality.
            diffs.Add(new Diff(Operation.DELETE, text1));
            diffs.Add(new Diff(Operation.INSERT, text2));
            return diffs;
        }

        // Check to see if the problem can be split in two.
        string[]? hm = diff_halfMatch(text1, text2);
        if (hm != null)
        {
            // A half-match was found, sort out the return data.
            string text1_a = hm[0];
            string text1_b = hm[1];
            string text2_a = hm[2];
            string text2_b = hm[3];
            string mid_common = hm[4];
            // Send both pairs off for separate processing.
            List<Diff> diffs_a = GetTextDifferences(text1_a, text2_a, checklines, deadline);
            List<Diff> diffs_b = GetTextDifferences(text1_b, text2_b, checklines, deadline);
            // Merge the results.
            diffs = diffs_a;
            diffs.Add(new Diff(Operation.EQUAL, mid_common));
            diffs.AddRange(diffs_b);
            return diffs;
        }

        if (checklines && text1.Length > 100 && text2.Length > 100)
        {
            return diff_lineMode(text1, text2, deadline);
        }

        return diff_bisect(text1, text2, deadline);
    }

    /**
     * Do a quick line-level diff on both strings, then rediff the parts for
     * greater accuracy.
     * This speedup can produce non-minimal diffs.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time when the diff should be complete by.
     * @return List of Diff objects.
     */
    private List<Diff> diff_lineMode(string text1, string text2,
                                     DateTime deadline)
    {
        // Scan the Text on a line-by-line basis first.
        object[] a = diff_linesToChars(text1, text2);
        text1 = (string)a[0];
        text2 = (string)a[1];
        List<string> linearray = (List<string>)a[2];

        List<Diff> diffs = GetTextDifferences(text1, text2, false, deadline);

        // Convert the diff back to original Text.
        diff_charsToLines(diffs, linearray);
        // Eliminate freak matches (e.g. blank lines)
        diff_cleanupSemantic(diffs);

        // Rediff any replacement blocks, this time character-by-character.
        // Add a dummy entry at the end.
        diffs.Add(new Diff(Operation.EQUAL, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        string text_delete = string.Empty;
        string text_insert = string.Empty;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.INSERT:
                    count_insert++;
                    text_insert += diffs[pointer].Text;
                    break;
                case Operation.DELETE:
                    count_delete++;
                    text_delete += diffs[pointer].Text;
                    break;
                case Operation.EQUAL:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete >= 1 && count_insert >= 1)
                    {
                        // Delete the offending records and add the merged ones.
                        diffs.RemoveRange(pointer - count_delete - count_insert,
                            count_delete + count_insert);
                        pointer = pointer - count_delete - count_insert;
                        List<Diff> subDiff =
                            GetTextDifferences(text_delete, text_insert, false, deadline);
                        diffs.InsertRange(pointer, subDiff);
                        pointer = pointer + subDiff.Count;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = string.Empty;
                    text_insert = string.Empty;
                    break;
            }
            pointer++;
        }
        diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.

        return diffs;
    }

    /**
     * Find the 'middle snake' of a diff, split the problem in two
     * and return the recursively constructed diff.
     * See Myers 1986 paper: An O(ND) Difference Algorithm and Its Variations.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param deadline Time at which to bail if not yet complete.
     * @return List of Diff objects.
     */
    protected List<Diff> diff_bisect(string text1, string text2,
        DateTime deadline)
    {
        // Cache the Text lengths to prevent multiple calls.
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        int max_d = (text1_length + text2_length + 1) / 2;
        int v_offset = max_d;
        int v_length = 2 * max_d;
        int[] v1 = new int[v_length];
        int[] v2 = new int[v_length];
        for (int x = 0; x < v_length; x++)
        {
            v1[x] = -1;
            v2[x] = -1;
        }
        v1[v_offset + 1] = 0;
        v2[v_offset + 1] = 0;
        int delta = text1_length - text2_length;
        // If the total number of characters is odd, then the front path will
        // collide with the reverse path.
        bool front = delta % 2 != 0;
        // Offsets for start and end of k loop.
        // Prevents mapping of space beyond the grid.
        int k1start = 0;
        int k1end = 0;
        int k2start = 0;
        int k2end = 0;
        for (int d = 0; d < max_d; d++)
        {
            // Bail out if deadline is reached.
            if (DateTime.Now > deadline)
            {
                break;
            }

            // Walk the front path one step.
            for (int k1 = -d + k1start; k1 <= d - k1end; k1 += 2)
            {
                int k1_offset = v_offset + k1;
                int x1;
                if (k1 == -d || k1 != d && v1[k1_offset - 1] < v1[k1_offset + 1])
                {
                    x1 = v1[k1_offset + 1];
                }
                else
                {
                    x1 = v1[k1_offset - 1] + 1;
                }
                int y1 = x1 - k1;
                while (x1 < text1_length && y1 < text2_length
                      && text1[x1] == text2[y1])
                {
                    x1++;
                    y1++;
                }
                v1[k1_offset] = x1;
                if (x1 > text1_length)
                {
                    // Ran off the right of the graph.
                    k1end += 2;
                }
                else if (y1 > text2_length)
                {
                    // Ran off the bottom of the graph.
                    k1start += 2;
                }
                else if (front)
                {
                    int k2_offset = v_offset + delta - k1;
                    if (k2_offset >= 0 && k2_offset < v_length && v2[k2_offset] != -1)
                    {
                        // Mirror x2 onto top-left coordinate system.
                        int x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return diff_bisectSplit(text1, text2, x1, y1, deadline);
                        }
                    }
                }
            }

            // Walk the reverse path one step.
            for (int k2 = -d + k2start; k2 <= d - k2end; k2 += 2)
            {
                int k2_offset = v_offset + k2;
                int x2;
                if (k2 == -d || k2 != d && v2[k2_offset - 1] < v2[k2_offset + 1])
                {
                    x2 = v2[k2_offset + 1];
                }
                else
                {
                    x2 = v2[k2_offset - 1] + 1;
                }
                int y2 = x2 - k2;
                while (x2 < text1_length && y2 < text2_length
                    && text1[text1_length - x2 - 1]
                    == text2[text2_length - y2 - 1])
                {
                    x2++;
                    y2++;
                }
                v2[k2_offset] = x2;
                if (x2 > text1_length)
                {
                    // Ran off the left of the graph.
                    k2end += 2;
                }
                else if (y2 > text2_length)
                {
                    // Ran off the top of the graph.
                    k2start += 2;
                }
                else if (!front)
                {
                    int k1_offset = v_offset + delta - k2;
                    if (k1_offset >= 0 && k1_offset < v_length && v1[k1_offset] != -1)
                    {
                        int x1 = v1[k1_offset];
                        int y1 = v_offset + x1 - k1_offset;
                        // Mirror x2 onto top-left coordinate system.
                        x2 = text1_length - v2[k2_offset];
                        if (x1 >= x2)
                        {
                            // Overlap detected.
                            return diff_bisectSplit(text1, text2, x1, y1, deadline);
                        }
                    }
                }
            }
        }
        // Diff took too long and hit the deadline or
        // number of diffs equals number of characters, no commonality at all.
        List<Diff> diffs = new List<Diff>();
        diffs.Add(new Diff(Operation.DELETE, text1));
        diffs.Add(new Diff(Operation.INSERT, text2));
        return diffs;
    }

    /**
     * Given the location of the 'middle snake', split the diff in two parts
     * and recurse.
     * @param text1 Old string to be diffed.
     * @param text2 New string to be diffed.
     * @param x Index of split point in text1.
     * @param y Index of split point in text2.
     * @param deadline Time at which to bail if not yet complete.
     * @return LinkedList of Diff objects.
     */
    private List<Diff> diff_bisectSplit(string text1, string text2,
        int x, int y, DateTime deadline)
    {
        string text1a = text1.Substring(0, x);
        string text2a = text2.Substring(0, y);
        string text1b = text1.Substring(x);
        string text2b = text2.Substring(y);

        // Compute both diffs serially.
        List<Diff> diffs = GetTextDifferences(text1a, text2a, false, deadline);
        List<Diff> diffsb = GetTextDifferences(text1b, text2b, false, deadline);

        diffs.AddRange(diffsb);
        return diffs;
    }

    /**
     * Split two texts into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Three element Object array, containing the encoded text1, the
     *     encoded text2 and the List of unique strings.  The zeroth element
     *     of the List of unique strings is intentionally blank.
     */
    protected object[] diff_linesToChars(string text1, string text2)
    {
        List<string> lineArray = new List<string>();
        Dictionary<string, int> lineHash = new Dictionary<string, int>();
        // e.g. linearray[4] == "Hello\n"
        // e.g. linehash.get("Hello\n") == 4

        // "\x00" is a valid character, but various debuggers don't like it.
        // So we'll insert a junk entry to avoid generating a null character.
        lineArray.Add(string.Empty);

        // Allocate 2/3rds of the space for text1, the rest for text2.
        string chars1 = diff_linesToCharsMunge(text1, lineArray, lineHash, 40000);
        string chars2 = diff_linesToCharsMunge(text2, lineArray, lineHash, 65535);
        return new object[] { chars1, chars2, lineArray };
    }

    /**
     * Split a Text into a list of strings.  Reduce the texts to a string of
     * hashes where each Unicode character represents one line.
     * @param Text String to encode.
     * @param lineArray List of unique strings.
     * @param lineHash Map of strings to indices.
     * @param maxLines Maximum length of lineArray.
     * @return Encoded string.
     */
    private string diff_linesToCharsMunge(string text, List<string> lineArray,
        Dictionary<string, int> lineHash, int maxLines)
    {
        int lineStart = 0;
        int lineEnd = -1;
        string line;
        StringBuilder chars = new StringBuilder();
        // Walk the Text, pulling out a Substring for each line.
        // Text.split('\n') would would temporarily double our memory footprint.
        // Modifying Text would create many large strings to garbage collect.
        while (lineEnd < text.Length - 1)
        {
            lineEnd = text.IndexOf('\n', lineStart);
            if (lineEnd == -1)
            {
                lineEnd = text.Length - 1;
            }
            line = text.JavaSubstring(lineStart, lineEnd + 1);

            if (lineHash.ContainsKey(line))
            {
                chars.Append((char)lineHash[line]);
            }
            else
            {
                if (lineArray.Count == maxLines)
                {
                    // Bail out at 65535 because char 65536 == char 0.
                    line = text.Substring(lineStart);
                    lineEnd = text.Length;
                }
                lineArray.Add(line);
                lineHash.Add(line, lineArray.Count - 1);
                chars.Append((char)(lineArray.Count - 1));
            }
            lineStart = lineEnd + 1;
        }
        return chars.ToString();
    }

    /**
     * Rehydrate the Text in a diff from a string of line hashes to real lines
     * of Text.
     * @param diffs List of Diff objects.
     * @param lineArray List of unique strings.
     */
    protected void diff_charsToLines(ICollection<Diff> diffs,
                    IList<string> lineArray)
    {
        StringBuilder text;
        foreach (Diff diff in diffs)
        {
            text = new StringBuilder();
            for (int j = 0; j < diff.Text.Length; j++)
            {
                text.Append(lineArray[diff.Text[j]]);
            }
            diff.Text = text.ToString();
        }
    }

    /**
     * Determine the common prefix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the start of each string.
     */
    public int diff_commonPrefix(string text1, string text2)
    {
        // Performance analysis: https://neil.fraser.name/news/2007/10/09/
        int n = Math.Min(text1.Length, text2.Length);
        for (int i = 0; i < n; i++)
        {
            if (text1[i] != text2[i])
            {
                return i;
            }
        }
        return n;
    }

    /**
     * Determine the common suffix of two strings.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of each string.
     */
    public int diff_commonSuffix(string text1, string text2)
    {
        // Performance analysis: https://neil.fraser.name/news/2007/10/09/
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        int n = Math.Min(text1.Length, text2.Length);
        for (int i = 1; i <= n; i++)
        {
            if (text1[text1_length - i] != text2[text2_length - i])
            {
                return i - 1;
            }
        }
        return n;
    }

    /**
     * Determine if the suffix of one string is the prefix of another.
     * @param text1 First string.
     * @param text2 Second string.
     * @return The number of characters common to the end of the first
     *     string and the start of the second string.
     */
    protected int diff_commonOverlap(string text1, string text2)
    {
        // Cache the Text lengths to prevent multiple calls.
        int text1_length = text1.Length;
        int text2_length = text2.Length;
        // Eliminate the null case.
        if (text1_length == 0 || text2_length == 0)
        {
            return 0;
        }
        // Truncate the longer string.
        if (text1_length > text2_length)
        {
            text1 = text1.Substring(text1_length - text2_length);
        }
        else if (text1_length < text2_length)
        {
            text2 = text2.Substring(0, text1_length);
        }
        int text_length = Math.Min(text1_length, text2_length);
        // Quick check for the worst case.
        if (text1 == text2)
        {
            return text_length;
        }

        // Start by looking for a single character match
        // and increase length until no match is found.
        // Performance analysis: https://neil.fraser.name/news/2010/11/04/
        int best = 0;
        int length = 1;
        while (true)
        {
            string pattern = text1.Substring(text_length - length);
            int found = text2.IndexOf(pattern, StringComparison.Ordinal);
            if (found == -1)
            {
                return best;
            }
            length += found;
            if (found == 0 || text1.Substring(text_length - length) ==
                text2.Substring(0, length))
            {
                best = length;
                length++;
            }
        }
    }

    /**
     * Do the two texts share a Substring which is at least half the length of
     * the longer Text?
     * This speedup can produce non-minimal diffs.
     * @param text1 First string.
     * @param text2 Second string.
     * @return Five element String array, containing the prefix of text1, the
     *     suffix of text1, the prefix of text2, the suffix of text2 and the
     *     common middle.  Or null if there was no match.
     */

    protected string[]? diff_halfMatch(string text1, string text2)
    {
        if (Diff_Timeout <= 0)
        {
            // Don't risk returning a non-optimal diff if we have unlimited time.
            return null;
        }
        string longtext = text1.Length > text2.Length ? text1 : text2;
        string shorttext = text1.Length > text2.Length ? text2 : text1;
        if (longtext.Length < 4 || shorttext.Length * 2 < longtext.Length)
        {
            return null;  // Pointless.
        }

        // First check if the second quarter is the seed for a half-match.
        string[]? hm1 = diff_halfMatchI(longtext, shorttext,
                                       (longtext.Length + 3) / 4);
        // Check again based on the third quarter.
        string[]? hm2 = diff_halfMatchI(longtext, shorttext,
                                       (longtext.Length + 1) / 2);
        string[] hm;
        if (hm1 is null && hm2 is null)
            return null;
        else if (hm2 is null)
            hm = hm1!;
        else if (hm1 is null)
            hm = hm2;
        else
            // Both matched.  Select the longest.
            hm = hm1[4].Length > hm2[4].Length ? hm1 : hm2;

        // A half-match was found, sort out the return data.
        if (text1.Length > text2.Length)
            return hm;
            //return new string[]{hm[0], hm[1], hm[2], hm[3], hm[4]};
        else
            return new string[] { hm[2], hm[3], hm[0], hm[1], hm[4] };
    }

    /**
     * Does a Substring of shorttext exist within longtext such that the
     * Substring is at least half the length of longtext?
     * @param longtext Longer string.
     * @param shorttext Shorter string.
     * @param i Start index of quarter length Substring within longtext.
     * @return Five element string array, containing the prefix of longtext, the
     *     suffix of longtext, the prefix of shorttext, the suffix of shorttext
     *     and the common middle.  Or null if there was no match.
     */
    private string[]? diff_halfMatchI(string longtext, string shorttext, int i)
    {
        // Start with a 1/4 length Substring at position i as a seed.
        string seed = longtext.Substring(i, longtext.Length / 4);
        int j = -1;
        string best_common = string.Empty;
        string best_longtext_a = string.Empty, best_longtext_b = string.Empty;
        string best_shorttext_a = string.Empty, best_shorttext_b = string.Empty;
        while (j < shorttext.Length && (j = shorttext.IndexOf(seed, j + 1,
            StringComparison.Ordinal)) != -1)
        {
            int prefixLength = diff_commonPrefix(longtext.Substring(i),
                                                 shorttext.Substring(j));
            int suffixLength = diff_commonSuffix(longtext.Substring(0, i),
                                                 shorttext.Substring(0, j));
            if (best_common.Length < suffixLength + prefixLength)
            {
                best_common = shorttext.Substring(j - suffixLength, suffixLength)
                    + shorttext.Substring(j, prefixLength);
                best_longtext_a = longtext.Substring(0, i - suffixLength);
                best_longtext_b = longtext.Substring(i + prefixLength);
                best_shorttext_a = shorttext.Substring(0, j - suffixLength);
                best_shorttext_b = shorttext.Substring(j + prefixLength);
            }
        }
        if (best_common.Length * 2 >= longtext.Length)
        {
            return new string[]{best_longtext_a, best_longtext_b,
        best_shorttext_a, best_shorttext_b, best_common};
        }
        else
        {
            return null;
        }
    }

    /**
     * Reduce the number of edits by eliminating semantically trivial
     * equalities.
     * @param diffs List of Diff objects.
     */
    public void diff_cleanupSemantic(List<Diff> diffs)
    {
        bool changes = false;
        // Stack of indices where equalities are found.
        Stack<int> equalities = new Stack<int>();
        // Always equal to equalities[equalitiesLength-1][1]
        string? lastEquality = null;
        int pointer = 0;  // Index of current position.
                          // Number of characters that changed prior to the equality.
        int length_insertions1 = 0;
        int length_deletions1 = 0;
        // Number of characters that changed after the equality.
        int length_insertions2 = 0;
        int length_deletions2 = 0;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer].Operation == Operation.EQUAL)
            {  // Equality found.
                equalities.Push(pointer);
                length_insertions1 = length_insertions2;
                length_deletions1 = length_deletions2;
                length_insertions2 = 0;
                length_deletions2 = 0;
                lastEquality = diffs[pointer].Text;
            }
            else
            {  // an insertion or deletion
                if (diffs[pointer].Operation == Operation.INSERT)
                {
                    length_insertions2 += diffs[pointer].Text.Length;
                }
                else
                {
                    length_deletions2 += diffs[pointer].Text.Length;
                }
                // Eliminate an equality that is smaller or equal to the edits on both
                // sides of it.
                if (lastEquality != null && lastEquality.Length
                    <= Math.Max(length_insertions1, length_deletions1)
                    && lastEquality.Length
                        <= Math.Max(length_insertions2, length_deletions2))
                {
                    // Duplicate record.
                    diffs.Insert(equalities.Peek(),
                                 new Diff(Operation.DELETE, lastEquality));
                    // Change second copy to insert.
                    diffs[equalities.Peek() + 1].Operation = Operation.INSERT;
                    // Throw away the equality we just deleted.
                    equalities.Pop();
                    if (equalities.Count > 0)
                    {
                        equalities.Pop();
                    }
                    pointer = equalities.Count > 0 ? equalities.Peek() : -1;
                    length_insertions1 = 0;  // Reset the counters.
                    length_deletions1 = 0;
                    length_insertions2 = 0;
                    length_deletions2 = 0;
                    lastEquality = null;
                    changes = true;
                }
            }
            pointer++;
        }

        // Normalize the diff.
        if (changes)
        {
            diff_cleanupMerge(diffs);
        }
        diff_cleanupSemanticLossless(diffs);

        // Find any overlaps between deletions and insertions.
        // e.g: <del>abcxxx</del><ins>xxxdef</ins>
        //   -> <del>abc</del>xxx<ins>def</ins>
        // e.g: <del>xxxabc</del><ins>defxxx</ins>
        //   -> <ins>def</ins>xxx<del>abc</del>
        // Only extract an overlap if it is as big as the edit ahead or behind it.
        pointer = 1;
        while (pointer < diffs.Count)
        {
            if (diffs[pointer - 1].Operation == Operation.DELETE &&
                diffs[pointer].Operation == Operation.INSERT)
            {
                string deletion = diffs[pointer - 1].Text;
                string insertion = diffs[pointer].Text;
                int overlap_length1 = diff_commonOverlap(deletion, insertion);
                int overlap_length2 = diff_commonOverlap(insertion, deletion);
                if (overlap_length1 >= overlap_length2)
                {
                    if (overlap_length1 >= deletion.Length / 2.0 ||
                        overlap_length1 >= insertion.Length / 2.0)
                    {
                        // Overlap found.
                        // Insert an equality and trim the surrounding edits.
                        diffs.Insert(pointer, new Diff(Operation.EQUAL,
                            insertion.Substring(0, overlap_length1)));
                        diffs[pointer - 1].Text =
                            deletion.Substring(0, deletion.Length - overlap_length1);
                        diffs[pointer + 1].Text = insertion.Substring(overlap_length1);
                        pointer++;
                    }
                }
                else
                {
                    if (overlap_length2 >= deletion.Length / 2.0 ||
                        overlap_length2 >= insertion.Length / 2.0)
                    {
                        // Reverse overlap found.
                        // Insert an equality and swap and trim the surrounding edits.
                        diffs.Insert(pointer, new Diff(Operation.EQUAL,
                            deletion.Substring(0, overlap_length2)));
                        diffs[pointer - 1].Operation = Operation.INSERT;
                        diffs[pointer - 1].Text =
                            insertion.Substring(0, insertion.Length - overlap_length2);
                        diffs[pointer + 1].Operation = Operation.DELETE;
                        diffs[pointer + 1].Text = deletion.Substring(overlap_length2);
                        pointer++;
                    }
                }
                pointer++;
            }
            pointer++;
        }
    }

    /**
     * Look for single edits surrounded on both sides by equalities
     * which can be shifted sideways to align the edit to a word boundary.
     * e.g: The c<ins>at c</ins>ame. -> The <ins>cat </ins>came.
     * @param diffs List of Diff objects.
     */
    public void diff_cleanupSemanticLossless(List<Diff> diffs)
    {
        int pointer = 1;
        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < diffs.Count - 1)
        {
            if (diffs[pointer - 1].Operation == Operation.EQUAL &&
              diffs[pointer + 1].Operation == Operation.EQUAL)
            {
                // This is a single edit surrounded by equalities.
                string equality1 = diffs[pointer - 1].Text;
                string edit = diffs[pointer].Text;
                string equality2 = diffs[pointer + 1].Text;

                // First, shift the edit as far left as possible.
                int commonOffset = diff_commonSuffix(equality1, edit);
                if (commonOffset > 0)
                {
                    string commonString = edit.Substring(edit.Length - commonOffset);
                    equality1 = equality1.Substring(0, equality1.Length - commonOffset);
                    edit = commonString + edit.Substring(0, edit.Length - commonOffset);
                    equality2 = commonString + equality2;
                }

                // Second, step character by character right,
                // looking for the best fit.
                string bestEquality1 = equality1;
                string bestEdit = edit;
                string bestEquality2 = equality2;
                int bestScore = diff_cleanupSemanticScore(equality1, edit) +
                    diff_cleanupSemanticScore(edit, equality2);
                while (edit.Length != 0 && equality2.Length != 0
                    && edit[0] == equality2[0])
                {
                    equality1 += edit[0];
                    edit = edit.Substring(1) + equality2[0];
                    equality2 = equality2.Substring(1);
                    int score = diff_cleanupSemanticScore(equality1, edit) +
                        diff_cleanupSemanticScore(edit, equality2);
                    // The >= encourages trailing rather than leading whitespace on
                    // edits.
                    if (score >= bestScore)
                    {
                        bestScore = score;
                        bestEquality1 = equality1;
                        bestEdit = edit;
                        bestEquality2 = equality2;
                    }
                }

                if (diffs[pointer - 1].Text != bestEquality1)
                {
                    // We have an improvement, save it back to the diff.
                    if (bestEquality1.Length != 0)
                    {
                        diffs[pointer - 1].Text = bestEquality1;
                    }
                    else
                    {
                        diffs.RemoveAt(pointer - 1);
                        pointer--;
                    }
                    diffs[pointer].Text = bestEdit;
                    if (bestEquality2.Length != 0)
                    {
                        diffs[pointer + 1].Text = bestEquality2;
                    }
                    else
                    {
                        diffs.RemoveAt(pointer + 1);
                        pointer--;
                    }
                }
            }
            pointer++;
        }
    }

    /**
     * Given two strings, compute a score representing whether the internal
     * boundary falls on logical boundaries.
     * Scores range from 6 (best) to 0 (worst).
     * @param one First string.
     * @param two Second string.
     * @return The score.
     */
    private int diff_cleanupSemanticScore(string one, string two)
    {
        if (one.Length == 0 || two.Length == 0)
        {
            // Edges are the best.
            return 6;
        }

        // Each port of this function behaves slightly differently due to
        // subtle differences in each language's definition of things like
        // 'whitespace'.  Since this function's purpose is largely cosmetic,
        // the choice has been made to use each language's native features
        // rather than force total conformity.
        char char1 = one[one.Length - 1];
        char char2 = two[0];
        bool nonAlphaNumeric1 = !char.IsLetterOrDigit(char1);
        bool nonAlphaNumeric2 = !char.IsLetterOrDigit(char2);
        bool whitespace1 = nonAlphaNumeric1 && char.IsWhiteSpace(char1);
        bool whitespace2 = nonAlphaNumeric2 && char.IsWhiteSpace(char2);
        bool lineBreak1 = whitespace1 && char.IsControl(char1);
        bool lineBreak2 = whitespace2 && char.IsControl(char2);
        bool blankLine1 = lineBreak1 && BLANKLINEEND.IsMatch(one);
        bool blankLine2 = lineBreak2 && BLANKLINESTART.IsMatch(two);

        if (blankLine1 || blankLine2)
        {
            // Five points for blank lines.
            return 5;
        }
        else if (lineBreak1 || lineBreak2)
        {
            // Four points for line breaks.
            return 4;
        }
        else if (nonAlphaNumeric1 && !whitespace1 && whitespace2)
        {
            // Three points for end of sentences.
            return 3;
        }
        else if (whitespace1 || whitespace2)
        {
            // Two points for whitespace.
            return 2;
        }
        else if (nonAlphaNumeric1 || nonAlphaNumeric2)
        {
            // One point for non-alphanumeric.
            return 1;
        }
        return 0;
    }


    /**
     * Reorder and merge like edit sections.  Merge equalities.
     * Any edit section can move as long as it doesn't cross an equality.
     * @param diffs List of Diff objects.
     */
    public void diff_cleanupMerge(List<Diff> diffs)
    {
        // Add a dummy entry at the end.
        diffs.Add(new Diff(Operation.EQUAL, string.Empty));
        int pointer = 0;
        int count_delete = 0;
        int count_insert = 0;
        string text_delete = string.Empty;
        string text_insert = string.Empty;
        int commonlength;
        while (pointer < diffs.Count)
        {
            switch (diffs[pointer].Operation)
            {
                case Operation.INSERT:
                    count_insert++;
                    text_insert += diffs[pointer].Text;
                    pointer++;
                    break;
                case Operation.DELETE:
                    count_delete++;
                    text_delete += diffs[pointer].Text;
                    pointer++;
                    break;
                case Operation.EQUAL:
                    // Upon reaching an equality, check for prior redundancies.
                    if (count_delete + count_insert > 1)
                    {
                        if (count_delete != 0 && count_insert != 0)
                        {
                            // Factor out any common prefixies.
                            commonlength = diff_commonPrefix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                if (pointer - count_delete - count_insert > 0 &&
                                  diffs[pointer - count_delete - count_insert - 1].Operation
                                      == Operation.EQUAL)
                                {
                                    diffs[pointer - count_delete - count_insert - 1].Text
                                        += text_insert.Substring(0, commonlength);
                                }
                                else
                                {
                                    diffs.Insert(0, new Diff(Operation.EQUAL,
                                        text_insert.Substring(0, commonlength)));
                                    pointer++;
                                }
                                text_insert = text_insert.Substring(commonlength);
                                text_delete = text_delete.Substring(commonlength);
                            }
                            // Factor out any common suffixies.
                            commonlength = diff_commonSuffix(text_insert, text_delete);
                            if (commonlength != 0)
                            {
                                diffs[pointer].Text = text_insert.Substring(text_insert.Length
                                    - commonlength) + diffs[pointer].Text;
                                text_insert = text_insert.Substring(0, text_insert.Length
                                    - commonlength);
                                text_delete = text_delete.Substring(0, text_delete.Length
                                    - commonlength);
                            }
                        }
                        // Delete the offending records and add the merged ones.
                        pointer -= count_delete + count_insert;
                        diffs.Splice(pointer, count_delete + count_insert);
                        if (text_delete.Length != 0)
                        {
                            diffs.Splice(pointer, 0,
                                new Diff(Operation.DELETE, text_delete));
                            pointer++;
                        }
                        if (text_insert.Length != 0)
                        {
                            diffs.Splice(pointer, 0,
                                new Diff(Operation.INSERT, text_insert));
                            pointer++;
                        }
                        pointer++;
                    }
                    else if (pointer != 0
                        && diffs[pointer - 1].Operation == Operation.EQUAL)
                    {
                        // Merge this equality with the previous one.
                        diffs[pointer - 1].Text += diffs[pointer].Text;
                        diffs.RemoveAt(pointer);
                    }
                    else
                    {
                        pointer++;
                    }
                    count_insert = 0;
                    count_delete = 0;
                    text_delete = string.Empty;
                    text_insert = string.Empty;
                    break;
            }
        }
        if (diffs[diffs.Count - 1].Text.Length == 0)
        {
            diffs.RemoveAt(diffs.Count - 1);  // Remove the dummy entry at the end.
        }

        // Second pass: look for single edits surrounded on both sides by
        // equalities which can be shifted sideways to eliminate an equality.
        // e.g: A<ins>BA</ins>C -> <ins>AB</ins>AC
        bool changes = false;
        pointer = 1;
        // Intentionally ignore the first and last element (don't need checking).
        while (pointer < diffs.Count - 1)
        {
            if (diffs[pointer - 1].Operation == Operation.EQUAL &&
              diffs[pointer + 1].Operation == Operation.EQUAL)
            {
                // This is a single edit surrounded by equalities.
                if (diffs[pointer].Text.EndsWith(diffs[pointer - 1].Text,
                    StringComparison.Ordinal))
                {
                    // Shift the edit over the previous equality.
                    diffs[pointer].Text = diffs[pointer - 1].Text +
                        diffs[pointer].Text.Substring(0, diffs[pointer].Text.Length -
                                                      diffs[pointer - 1].Text.Length);
                    diffs[pointer + 1].Text = diffs[pointer - 1].Text
                        + diffs[pointer + 1].Text;
                    diffs.Splice(pointer - 1, 1);
                    changes = true;
                }
                else if (diffs[pointer].Text.StartsWith(diffs[pointer + 1].Text,
                    StringComparison.Ordinal))
                {
                    // Shift the edit over the next equality.
                    diffs[pointer - 1].Text += diffs[pointer + 1].Text;
                    diffs[pointer].Text =
                        diffs[pointer].Text.Substring(diffs[pointer + 1].Text.Length)
                        + diffs[pointer + 1].Text;
                    diffs.Splice(pointer + 1, 1);
                    changes = true;
                }
            }
            pointer++;
        }
        // If shifts were made, the diff needs reordering and another shift sweep.
        if (changes)
        {
            diff_cleanupMerge(diffs);
        }
    }

}