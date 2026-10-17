using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using ZenQOffice.UI;

namespace ZenQOffice.Core
{
    public enum DiffOp
    {
        Equal,
        Insert,
        Delete
    }

    public class DiffToken
    {
        public DiffOp Operation { get; set; }
        public string Text { get; set; }

        public DiffToken(DiffOp op, string text)
        {
            Operation = op;
            Text = text;
        }
    }

    public class DocumentCompareResult
    {
        public string CombinedText { get; set; } = "";
        public List<DocumentRevision> Revisions { get; set; } = new List<DocumentRevision>();
        public int InsertionsCount { get; set; } = 0;
        public int DeletionsCount { get; set; } = 0;
        public int UnchangedWordsCount { get; set; } = 0;
        public double SimilarityPercentage { get; set; } = 100.0;
        public List<DiffToken> Tokens { get; set; } = new List<DiffToken>();
    }

    public static class DocumentCompareEngine
    {
        /// <summary>
        /// Splits text into tokens (words and punctuation/whitespace preserved)
        /// </summary>
        public static List<string> TokenizeWords(string text)
        {
            var tokens = new List<string>();
            if (string.IsNullOrEmpty(text)) return tokens;

            int i = 0;
            int n = text.Length;
            while (i < n)
            {
                if (char.IsWhiteSpace(text[i]))
                {
                    int start = i;
                    while (i < n && char.IsWhiteSpace(text[i])) i++;
                    tokens.Add(text.Substring(start, i - start));
                }
                else if (char.IsLetterOrDigit(text[i]))
                {
                    int start = i;
                    while (i < n && char.IsLetterOrDigit(text[i])) i++;
                    tokens.Add(text.Substring(start, i - start));
                }
                else
                {
                    tokens.Add(text[i].ToString());
                    i++;
                }
            }
            return tokens;
        }

        /// <summary>
        /// Compares originalText vs revisedText using Word-level Longest Common Subsequence (LCS)
        /// </summary>
        public static List<DiffToken> ComputeWordDiff(string originalText, string revisedText)
        {
            var words1 = TokenizeWords(originalText ?? "");
            var words2 = TokenizeWords(revisedText ?? "");

            int n = words1.Count;
            int m = words2.Count;

            // LCS Matrix
            int[,] lcs = new int[n + 1, m + 1];
            for (int i = 0; i <= n; i++)
            {
                for (int j = 0; j <= m; j++)
                {
                    if (i == 0 || j == 0)
                    {
                        lcs[i, j] = 0;
                    }
                    else if (string.Equals(words1[i - 1], words2[j - 1], StringComparison.Ordinal))
                    {
                        lcs[i, j] = lcs[i - 1, j - 1] + 1;
                    }
                    else
                    {
                        lcs[i, j] = Math.Max(lcs[i - 1, j], lcs[i, j - 1]);
                    }
                }
            }

            // Backtrack to extract diff tokens
            var diff = new List<DiffToken>();
            int x = n;
            int y = m;
            var revDiff = new List<DiffToken>();

            while (x > 0 || y > 0)
            {
                if (x > 0 && y > 0 && string.Equals(words1[x - 1], words2[y - 1], StringComparison.Ordinal))
                {
                    revDiff.Add(new DiffToken(DiffOp.Equal, words1[x - 1]));
                    x--;
                    y--;
                }
                else if (y > 0 && (x == 0 || lcs[x, y - 1] >= lcs[x - 1, y]))
                {
                    revDiff.Add(new DiffToken(DiffOp.Insert, words2[y - 1]));
                    y--;
                }
                else if (x > 0 && (y == 0 || lcs[x, y - 1] < lcs[x - 1, y]))
                {
                    revDiff.Add(new DiffToken(DiffOp.Delete, words1[x - 1]));
                    x--;
                }
            }

            revDiff.Reverse();
            return revDiff;
        }

        /// <summary>
        /// Performs full comparison and produces tracked revisions and metrics
        /// </summary>
        public static DocumentCompareResult CompareDocuments(string originalText, string revisedText, string author = "So sánh tài liệu")
        {
            var result = new DocumentCompareResult();
            var tokens = ComputeWordDiff(originalText, revisedText);
            result.Tokens = tokens;

            StringBuilder combinedSb = new StringBuilder();
            int currentOffset = 0;
            int revId = 1;

            Color insColor = Color.FromArgb(16, 185, 129); // Green
            Color delColor = Color.FromArgb(239, 68, 68);  // Red

            int wordIns = 0;
            int wordDel = 0;
            int wordEq = 0;

            for (int i = 0; i < tokens.Count; i++)
            {
                var tok = tokens[i];
                if (tok.Operation == DiffOp.Equal)
                {
                    combinedSb.Append(tok.Text);
                    currentOffset += tok.Text.Length;
                    if (!string.IsNullOrWhiteSpace(tok.Text)) wordEq++;
                }
                else if (tok.Operation == DiffOp.Insert)
                {
                    int start = currentOffset;
                    combinedSb.Append(tok.Text);
                    currentOffset += tok.Text.Length;
                    if (!string.IsNullOrWhiteSpace(tok.Text)) wordIns++;

                    var rev = new DocumentRevision
                    {
                        Id = revId++,
                        Type = RevisionType.Insertion,
                        Author = author,
                        Timestamp = DateTime.Now,
                        StartIndex = start,
                        Length = tok.Text.Length,
                        Text = tok.Text,
                        Color = insColor
                    };
                    result.Revisions.Add(rev);
                }
                else if (tok.Operation == DiffOp.Delete)
                {
                    if (!string.IsNullOrWhiteSpace(tok.Text)) wordDel++;
                    // Deletions are anchored at the currentOffset
                    var rev = new DocumentRevision
                    {
                        Id = revId++,
                        Type = RevisionType.Deletion,
                        Author = author,
                        Timestamp = DateTime.Now,
                        StartIndex = currentOffset,
                        Length = tok.Text.Length,
                        Text = tok.Text,
                        Color = delColor
                    };
                    result.Revisions.Add(rev);
                }
            }

            result.CombinedText = combinedSb.ToString();
            result.InsertionsCount = wordIns;
            result.DeletionsCount = wordDel;
            result.UnchangedWordsCount = wordEq;

            int totalWords = wordEq + Math.Max(wordIns, wordDel);
            if (totalWords > 0)
            {
                result.SimilarityPercentage = Math.Round(((double)wordEq / totalWords) * 100.0, 1);
            }
            else
            {
                result.SimilarityPercentage = 100.0;
            }

            return result;
        }

        /// <summary>
        /// Applies the comparison result into a target WriterView instance
        /// </summary>
        public static void ApplyToWriterView(WriterView writerView, DocumentCompareResult cmpResult)
        {
            if (writerView == null || cmpResult == null) return;

            writerView.PieceTable.SetInitialText(cmpResult.CombinedText ?? "");
            writerView.CursorPosition = 0;
            writerView.SelectionStart = 0;
            writerView.SelectionEnd = 0;
            writerView.Revisions.Clear();
            writerView.Revisions.AddRange(cmpResult.Revisions);
            writerView.TrackChanges = true;
            writerView.DisplayMarkupMode = MarkupDisplayMode.AllMarkup;
            writerView.Invalidate();
        }
    }
}
