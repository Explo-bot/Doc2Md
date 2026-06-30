using System.Text;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Content;

namespace Doc2Md.Converters
{
    /// <summary>
    /// Converts PDF files to Markdown with structural detection of headings,
    /// lists, table rows, and inline styles (bold/italic).
    /// </summary>
    public class PdfConverter : IPdfConverter
    {
        /// <summary>Markdown heading prefixes for levels H1–H3.</summary>
        private static readonly string[] _headingPrefixes = { "# ", "## ", "### " };

        /// <summary>
        /// Converts a PDF file to Markdown by structurally analysing each page.
        /// </summary>
        /// <param name="inputPath">Absolute path to the source .pdf file.</param>
        /// <param name="outputPath">Absolute path for the generated .md file.</param>
        /// <returns>0 on success, non-zero on failure.</returns>
        public int Convert(string inputPath, string outputPath)
        {
            Console.WriteLine($"Converting PDF: {inputPath}");

            var sb = new StringBuilder();

            using (var document = PdfDocument.Open(inputPath))
            {
                foreach (var page in document.GetPages())
                {
                    foreach (var line in BuildStructuredLines(page))
                    {
                        if (string.IsNullOrWhiteSpace(line.Text))
                        {
                            sb.AppendLine();
                            continue;
                        }

                        string text = line.Text.Trim();

                        // Heading
                        if (line.HeadingLevel > 0)
                        {
                            string prefix = _headingPrefixes[Math.Clamp(line.HeadingLevel - 1, 0, _headingPrefixes.Length - 1)];
                            sb.AppendLine(prefix + ApplyInlineStyles(text, line.IsBold, line.IsItalic));
                            continue;
                        }

                        // List item
                        if (line.IsListItem)
                        {
                            string indent = new string(' ', line.ListLevel * 2);
                            string content = text.TrimStart('-', '*', '•', ' ', '\t');
                            sb.AppendLine($"{indent}- {ApplyInlineStyles(content, line.IsBold, line.IsItalic)}");
                            continue;
                        }

                        // Table row
                        if (line.IsTableRow)
                        {
                            var cells = SplitIntoColumns(line.RawWords);
                            sb.AppendLine("| " + string.Join(" | ", cells.Select(c => c.Trim())) + " |");
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

        // ──────────────────────── STRUCTURAL ANALYSIS ────────────────────────

        /// <summary>
        /// Groups the words on a PDF page into structured lines,
        /// classifying each line as a heading, list item, or table row.
        /// </summary>
        /// <param name="page">The PDF page to analyse.</param>
        /// <returns>Ordered list of structured lines extracted from the page.</returns>
        private static List<StructuredLine> BuildStructuredLines(Page page)
        {
            var words = page.GetWords().ToList();
            if (words.Count == 0)
                return new List<StructuredLine>();

            double maxFont = words.Max(w => GetWordFontSize(w));
            double avgFont = words.Average(w => GetWordFontSize(w));

            var lines = new List<StructuredLine>();

            foreach (var group in words
                .GroupBy(w => Math.Round(w.BoundingBox.Bottom, 1))
                .OrderByDescending(g => g.Key))
            {
                var lineWords = group.OrderBy(w => w.BoundingBox.Left).ToList();
                string text = string.Join(" ", lineWords.Select(w => w.Text));

                if (string.IsNullOrWhiteSpace(text))
                {
                    lines.Add(new StructuredLine("", 0, false, false, 0, false, 0, false, lineWords, 0));
                    continue;
                }

                double avgLineFont = lineWords.Average(w => GetWordFontSize(w));
                bool isBold       = lineWords.Any(IsBoldFont);
                bool isItalic     = lineWords.Any(IsItalicFont);
                double indent     = lineWords.First().BoundingBox.Left;

                int headingLevel = 0;
                if (avgLineFont >= maxFont - 0.5)   headingLevel = 1;
                else if (avgLineFont >= avgFont + 2) headingLevel = 2;
                else if (avgLineFont >= avgFont + 1) headingLevel = 3;

                bool isListItem = IsListItemText(text);
                int listLevel   = isListItem ? ComputeListLevel(indent, words) : 0;
                bool isTableRow = IsTableRowByGaps(lineWords);

                lines.Add(new StructuredLine(text, avgLineFont, isBold, isItalic,
                    indent, isListItem, listLevel, isTableRow, lineWords, headingLevel));
            }

            return lines;
        }

        /// <summary>
        /// Determines whether a word's font is bold based on the font name.
        /// </summary>
        /// <param name="w">The PDF word to inspect.</param>
        /// <returns><c>true</c> if the font name contains a bold indicator.</returns>
        private static bool IsBoldFont(Word w)
        {
            string name = w.FontName?.ToLowerInvariant() ?? "";
            return name.Contains("bold") || name.Contains("black") || name.Contains("heavy");
        }

        /// <summary>
        /// Determines whether a word's font is italic based on the font name.
        /// </summary>
        /// <param name="w">The PDF word to inspect.</param>
        /// <returns><c>true</c> if the font name contains an italic indicator.</returns>
        private static bool IsItalicFont(Word w)
        {
            string name = w.FontName?.ToLowerInvariant() ?? "";
            return name.Contains("italic") || name.Contains("oblique");
        }

        /// <summary>
        /// Determines whether a text string represents a list item.
        /// </summary>
        /// <param name="text">Text to analyse.</param>
        /// <returns><c>true</c> if the text begins with a list marker.</returns>
        private static bool IsListItemText(string text)
        {
            text = text.TrimStart();
            if (text.StartsWith("- ") || text.StartsWith("* ") || text.StartsWith("• "))
                return true;

            if (text.Length > 2 && (char.IsDigit(text[0]) || char.IsLetter(text[0])))
                if (text[1] == '.' || text[1] == ')')
                    return true;

            return false;
        }

        /// <summary>
        /// Computes the nesting level of a list item relative to the page's base indentation.
        /// </summary>
        /// <param name="indent">Left X position of the current line.</param>
        /// <param name="allWords">All words on the page, used to determine the base indentation.</param>
        /// <returns>Indentation level clamped between 0 and 5.</returns>
        private static int ComputeListLevel(double indent, List<Word> allWords)
        {
            var lefts = allWords
                .Select(w => w.BoundingBox.Left)
                .OrderBy(x => x)
                .Distinct()
                .ToList();

            if (lefts.Count == 0) return 0;

            int level = (int)Math.Round((indent - lefts.First()) / 15.0);
            return Math.Clamp(level, 0, 5);
        }

        /// <summary>
        /// Determines whether a row of words forms a table row by detecting large horizontal gaps.
        /// </summary>
        /// <param name="words">Words in the row, ordered by position.</param>
        /// <returns><c>true</c> if at least one gap between words exceeds 2.5× the average gap.</returns>
        private static bool IsTableRowByGaps(List<Word> words)
        {
            if (words.Count < 3) return false;

            var gaps = new List<double>();
            for (int i = 0; i < words.Count - 1; i++)
            {
                double gap = words[i + 1].BoundingBox.Left - words[i].BoundingBox.Right;
                if (gap > 0) gaps.Add(gap);
            }

            if (gaps.Count == 0) return false;

            double threshold = gaps.Average() * 2.5;
            return gaps.Any(g => g >= threshold);
        }

        /// <summary>
        /// Groups the words in a row into table cells by splitting on large gaps.
        /// </summary>
        /// <param name="words">Words in the row to split into columns.</param>
        /// <returns>List of strings, one per cell.</returns>
        private static List<string> SplitIntoColumns(List<Word> words)
        {
            var cells = new List<string>();
            if (words.Count == 0) return cells;

            var current = new List<Word>();
            double? lastRight = null;

            foreach (var w in words.OrderBy(w => w.BoundingBox.Left))
            {
                if (lastRight.HasValue && w.BoundingBox.Left - lastRight.Value > w.BoundingBox.Width * 1.5)
                {
                    cells.Add(string.Join(" ", current.Select(x => x.Text)));
                    current.Clear();
                }

                current.Add(w);
                lastRight = w.BoundingBox.Right;
            }

            if (current.Count > 0)
                cells.Add(string.Join(" ", current.Select(x => x.Text)));

            return cells;
        }

        /// <summary>
        /// Applies Markdown bold and/or italic markers to the given text.
        /// </summary>
        /// <param name="text">Text to decorate.</param>
        /// <param name="isBold">If <c>true</c>, wraps the text with bold markers.</param>
        /// <param name="isItalic">If <c>true</c>, wraps the text with italic markers.</param>
        /// <returns>Text with appropriate Markdown markers, or the original text if no style is active.</returns>
        private static string ApplyInlineStyles(string text, bool isBold, bool isItalic)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;

            if (isBold && isItalic) return $"***{text}***";
            if (isBold)             return $"**{text}**";
            if (isItalic)           return $"*{text}*";

            return text;
        }

        /// <summary>
        /// Calculates the average font size of a word based on its individual letters.
        /// </summary>
        /// <param name="w">The PDF word to measure.</param>
        /// <returns>Average font size, or 10 if the word contains no letters.</returns>
        private static double GetWordFontSize(Word w) =>
            w.Letters is { Count: > 0 } ? w.Letters.Average(l => l.FontSize) : 10;

        /// <summary>
        /// Represents a structured text line extracted from a PDF page.
        /// </summary>
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
