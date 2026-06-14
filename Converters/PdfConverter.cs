using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Doc2Md.Converters
{
    public class PdfConverter : IPdfConverter
    {
        public int Convert(string inputPath, string outputPath)
        {
            return ConvertPdfToMarkdown(inputPath, outputPath);
        }

        public static int ConvertPdfToMarkdown(string inputPath, string outputPath)
        {
            Console.WriteLine($"Converting PDF: {inputPath}");

            var sb = new StringBuilder();

            using (PdfDocument document = PdfDocument.Open(inputPath))
            {
                foreach (var page in document.GetPages())
                {
                    var lines = BuildStructuredLines(page);

                    foreach (var line in lines)
                    {
                        if (string.IsNullOrWhiteSpace(line.Text))
                        {
                            sb.AppendLine();
                            continue;
                        }

                        string text = line.Text.Trim();

                        // Headings
                        if (line.HeadingLevel > 0)
                        {
                            string prefix = line.HeadingLevel switch
                            {
                                1 => "# ",
                                2 => "## ",
                                3 => "### ",
                                _ => "# "
                            };

                            sb.AppendLine(prefix + ApplyInlineStyles(text, line.IsBold, line.IsItalic));
                            continue;
                        }

                        // List items
                        if (line.IsListItem)
                        {
                            string bullet = "- ";
                            string indent = new string(' ', line.ListLevel * 2);
                            string content = text.TrimStart('-', '*', '•', ' ', '\t');
                            sb.AppendLine($"{indent}{bullet}{ApplyInlineStyles(content, line.IsBold, line.IsItalic)}");
                            continue;
                        }

                        // Table rows
                        if (line.IsTableRow)
                        {
                            var cells = SplitIntoColumns(line.RawWords);
                            string row = "| " + string.Join(" | ", cells.Select(c => c.Trim())) + " |";
                            sb.AppendLine(row);
                            continue;
                        }

                        // Normal paragraph
                        sb.AppendLine(ApplyInlineStyles(text, line.IsBold, line.IsItalic));
                    }

                    sb.AppendLine();
                }
            }

            File.WriteAllText(outputPath, sb.ToString());

            Console.WriteLine("PDF conversion completed (structured).");
            return 0;
        }

        // ---------------- STRUCTURE ANALYSIS ----------------

        private static List<StructuredLine> BuildStructuredLines(Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
                return new List<StructuredLine>();

            double maxFont = words.Max(w => GetWordFontSize(w));
            double avgFont = words.Average(w => GetWordFontSize(w));

            var groupedByY = words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key);

            var lines = new List<StructuredLine>();

            foreach (var group in groupedByY)
            {
                var lineWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
                string text = string.Join(" ", lineWords.Select(w => w.Text));

                if (string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(new StructuredLine("", 0, false, false, 0, false, 0, false, lineWords, 0));
                    continue;
                }

                double avgLineFont = lineWords.Average(w => GetWordFontSize(w));
                bool isBold = lineWords.Any(IsBoldFont);
                bool isItalic = lineWords.Any(IsItalicFont);
                double indent = lineWords.First().BoundingBox.Left;

                int headingLevel = 0;
                if (avgLineFont >= maxFont - 0.5)
                    headingLevel = 1;
                else if (avgLineFont >= avgFont + 2)
                    headingLevel = 2;
                else if (avgLineFont >= avgFont + 1)
                    headingLevel = 3;

                bool isListItem = IsListItemText(text);
                int listLevel = isListItem ? ComputeListLevel(indent, words) : 0;

                bool isTableRow = IsTableRowByGaps(lineWords);

                lines.Add(new StructuredLine(
                    text,
                    avgLineFont,
                    isBold,
                    isItalic,
                    indent,
                    isListItem,
                    listLevel,
                    isTableRow,
                    lineWords,
                    headingLevel
                ));
            }

            return lines;
        }

        private static bool IsBoldFont(Word w)
        {
            string name = w.FontName?.ToLowerInvariant() ?? "";
            return name.Contains("bold") || name.Contains("black") || name.Contains("heavy");
        }

        private static bool IsItalicFont(Word w)
        {
            string name = w.FontName?.ToLowerInvariant() ?? "";
            return name.Contains("italic") || name.Contains("oblique");
        }

        private static bool IsListItemText(string text)
        {
            text = text.TrimStart();
            if (text.StartsWith("- ") || text.StartsWith("* ") || text.StartsWith("• "))
                return true;

            if (text.Length > 2 && (char.IsDigit(text[0]) || char.IsLetter(text[0])))
            {
                if (text[1] == '.' || text[1] == ')')
                    return true;
            }

            return false;
        }

        private static int ComputeListLevel(double indent, List<Word> allWords)
        {
            var lefts = allWords
                .Select(w => w.BoundingBox.Left)
                .OrderBy(x => x)
                .Distinct()
                .ToList();

            if (lefts.Count == 0)
                return 0;

            double baseIndent = lefts.First();
            double step = 15.0;

            int level = (int)Math.Round((indent - baseIndent) / step);
            return Math.Clamp(level, 0, 5);
        }

        private static bool IsTableRowByGaps(List<Word> words)
        {
            if (words.Count < 3)
                return false;

            var gaps = new List<double>();
            for (int i = 0; i < words.Count - 1; i++)
            {
                double gap = words[i + 1].BoundingBox.Left - words[i].BoundingBox.Right;
                if (gap > 0)
                    gaps.Add(gap);
            }

            if (gaps.Count == 0)
                return false;

            double avgGap = gaps.Average();
            double largeGapThreshold = avgGap * 2.5;

            int largeGaps = gaps.Count(g => g >= largeGapThreshold);
            return largeGaps >= 1;
        }

        private static List<string> SplitIntoColumns(List<Word> words)
        {
            var cells = new List<string>();
            if (words.Count == 0)
                return cells;

            var current = new List<Word>();
            double? lastRight = null;

            foreach (var w in words.OrderBy(w => w.BoundingBox.Left))
            {
                if (lastRight.HasValue)
                {
                    double gap = w.BoundingBox.Left - lastRight.Value;
                    if (gap > (w.BoundingBox.Width * 1.5))
                    {
                        cells.Add(string.Join(" ", current.Select(x => x.Text)));
                        current.Clear();
                    }
                }

                current.Add(w);
                lastRight = w.BoundingBox.Right;
            }

            if (current.Count > 0)
                cells.Add(string.Join(" ", current.Select(x => x.Text)));

            return cells;
        }

        private static string ApplyInlineStyles(string text, bool isBold, bool isItalic)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text;

            if (isBold && isItalic)
                return $"***{text}***";
            if (isBold)
                return $"**{text}**";
            if (isItalic)
                return $"*{text}*";

            return text;
        }

        private static double GetWordFontSize(Word w)
        {
            if (w.Letters == null || w.Letters.Count == 0)
                return 10;

            return w.Letters.Average(l => l.FontSize);
        }

        private record StructuredLine(
            string Text,
            double FontSize,
            bool IsBold,
            bool IsItalic,
            double Indent,
            bool IsListItem,
            int ListLevel,
            bool IsTableRow,
            List<Word> RawWords,
            int HeadingLevel
        );
    }
}
