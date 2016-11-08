using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using DiffMatchPatch;
using MwParserFromScratch;
using MwParserFromScratch.Nodes;
using Unclassified.TxLib;

namespace WikiEdit.Spark
{
    /// <summary>
    /// An intelligent summary builder. 
    /// </summary>
    public static class SummaryBuilder
    {
        private class WikitextSection
        {
            public Heading Heading { get; set; }

            public WikitextSection Parent { get; set; }

            public string Content { get; set; }

            public int Start { get; set; }

            public int Length { get; set; }

            public int End => Start + Length;

            public int InsertedChars { get; set; }

            public int DeletedChars { get; set; }
        }

        /// <summary>
        /// Generates summary by comparing the contents of wikitext.
        /// </summary>
        /// <returns>The auto-generated summary text.</returns>
        public static string BuildWikitextSummary(string text1, string text2)
        {
            var parser = new WikitextParser();
            var wt1 = parser.Parse(text1);
            var wt2 = parser.Parse(text2);
            var sections1 = WikitextSplitSections(text1, wt1);
            var dmp = new diff_match_patch();
            var diffs = dmp.diff_main(text1, text2);
            // We don't want too many segments stuck together, which may lead into
            // inaccurate results.
            dmp.diff_cleanupSemanticLossless(diffs);
            var position1 = 0;
            var sectionId1 = 0;
            // Check the number of characters changed in each section.
            foreach (var diff in diffs)
            {
                while (position1 > sections1[sectionId1].End)
                    sectionId1++;
                var section = sections1[sectionId1];
                switch (diff.operation)
                {
                    case Operation.DELETE:
                        section.DeletedChars += diff.text.Length;
                        position1 += diff.text.Length;
                        break;
                    case Operation.INSERT:
                        section.InsertedChars += diff.text.Length;
                        break;
                    case Operation.EQUAL:
                        position1 += diff.text.Length;
                        break;
                }
            }
            // Then summarize them.
            var summary = string.Join(" ", sections1.Where(s => s.InsertedChars + s.DeletedChars > 0)
                .Select(s =>
                {
                    var headingName = s.Heading == null ? "top" : string.Join(null, s.Heading.Inlines).Trim();
                    var segment = "/*" + headingName + "*/";
                    string change = null;
                    if (s.InsertedChars > 0) change = "+" + s.InsertedChars;
                    if (s.DeletedChars > 0) change = (change == null ? null : change + " ") + "-" + s.DeletedChars;
                    return segment + Tx.P(change);
                }));
            return summary;
        }

        private static WikitextSection SectionFromPosition(IList<WikitextSection> sections, int position)
        {
            foreach (var sect in sections)
            {
                if (sect.Start <= position && sect.Start + sect.Length > position) return sect;
            }
            return null;
        }

        private static IList<WikitextSection> WikitextSplitSections(string text, Wikitext wt)
        {
            Debug.Assert(text != null);
            Debug.Assert(wt != null);
            Debug.Assert(text == wt.ToString());
            var result = new List<WikitextSection>();
            var levelStack = new Stack<WikitextSection>();
            var pos = 0;
            Heading lastHeading = null;
            foreach (var h in wt.EnumDescendants().OfType<Heading>())
            {
                // Split the text
                var span = (IWikitextSpanInfo) h;
                var item = new WikitextSection
                {
                    Heading = lastHeading,
                    Content = text.Substring(pos, span.Start - pos),
                    Start = pos,
                    Length = span.Start - pos
                };
                result.Add(item);
                // Determine the level
                while (levelStack.Count > 0 && levelStack.Peek().Heading.Level > h.Level)
                    levelStack.Pop();
                if (levelStack.Count > 0)
                    item.Parent = levelStack.Peek();
//
                pos = span.Start + span.Length;
                lastHeading = h;
            }
            return result;
        }
    }
}
