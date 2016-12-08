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
using WikiDiffSummary;

namespace WikiEdit.Spark
{
    /// <summary>
    /// An intelligent summary builder. 
    /// </summary>
    public static class SummaryBuilder
    {
        /// <summary>
        /// Generates summary by comparing the contents of wikitext.
        /// </summary>
        /// <returns>The auto-generated summary text.</returns>
        public static string BuildWikitextSummary(string text1, string text2)
        {
            var cmp = new WikitextBySectionComparer();
            var diff = cmp.Compare(text1 ?? "", text2 ?? "");
            var headingCounter = new Dictionary<string, int> {{"top", 1}};
            var sb = new StringBuilder();
            Action<StringBuilder, SectionDiff> buildLengthChange = (b, d) =>
            {
                // Print length change
                if (d.AddedChars > 0 || d.RemovedChars > 0)
                {
                    b.Append('(');
                    if (d.AddedChars > 0)
                    {
                        b.Append('+');
                        b.Append(d.AddedChars);
                        if (d.RemovedChars > 0)
                            b.Append(' ');
                    }
                    if (d.RemovedChars > 0)
                    {
                        b.Append('-');
                        b.Append(d.RemovedChars);
                    }
                    b.Append(')');
                }
            };
            foreach (var d in diff)
            {
                if (d.Status == SectionDiffStatus.Identical)
                {
                }
                else if (d.Section1 != null)
                {
                    Debug.Assert(d.Status == SectionDiffStatus.Removed
                                 || d.Status == SectionDiffStatus.Modified
                                 || d.Status == SectionDiffStatus.WhitespaceModified);
                    // Print original heading path
                    if (d.Section2?.Path == d.Section1.Path)
                    {
                        sb.Append(BuildSectionLink(d.Section1.Path, null,
                            headingCounter.TryGetValue(d.Section1.Path.LastOrDefault() ?? "top"),
                            true));
                    }
                    else
                    {
                        sb.Append(d.Section1.Path);
                        sb.Append(':');
                    }
                    buildLengthChange(sb, d);
                    // Print summary
                    switch (d.Status)
                    {
                        case SectionDiffStatus.Removed:
                            sb.Append(Tx.T("editor.summary builder.removed section"));
                            break;
                        case SectionDiffStatus.Modified:
                        case SectionDiffStatus.WhitespaceModified:
                            Debug.Assert(d.Section2 != null);
                            if (d.Section1.Path != d.Section2.Path)
                            {
                                // Section rename
                                var newPathExpr = BuildSectionLink(d.Section2.Path, d.Section1.Path,
                                    headingCounter.TryGetValue(d.Section2.Path.LastOrDefault() ?? "top"),
                                    false);
                                sb.Append(Tx.T("editor.summary builder.renamed section", "path", newPathExpr));
                            }
                            if (d.Status == SectionDiffStatus.WhitespaceModified)
                                sb.Append(Tx.T("editor.summary builder.whitespace changes"));
                            break;
                    }
                    sb.Append(' ');
                }
                else
                {
                    Debug.Assert(d.Section2 != null);
                    Debug.Assert(d.Status == SectionDiffStatus.Added);
                    sb.Append(BuildSectionLink(d.Section2.Path, null,
                        headingCounter.TryGetValue(d.Section2.Path.LastOrDefault() ?? "top"),
                        true));
                    buildLengthChange(sb, d);
                    sb.Append(Tx.T("editor.summary builder.new section"));
                    sb.Append(' ');
                }
                if (d.Section2 != null && d.Section2.Path.Length > 0)
                {
                    var heading = d.Section2.Path.Last();
                    headingCounter[heading] = headingCounter.TryGetValue(heading) + 1;
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Build a section anchor link for edit summary.
        /// </summary>
        /// <param name="sectionPath">The section path.</param>
        /// <param name="originalSectionPath">The section path before modification. This is used to trace the renamed heading.</param>
        /// <param name="serial">The serial number of the section, if there're more than one sections with the same name.</param>
        /// <returns>The desired wikitext.</returns>
        private static string BuildSectionLink(SectionPath sectionPath, SectionPath originalSectionPath
            , int serial, bool withColon)
        {
            Debug.Assert(sectionPath != null);
            if (sectionPath.Length == 0) return "/*top*/";
            if (sectionPath.Length == 1 && !sectionPath[0].Contains("*/"))
            {
                var title = sectionPath[0];
                if (serial > 0) title += "_" + serial;
                return "/*" + title + "*/";
            }
            // Here we do not use MediaWiki's built-in anchor expression, i.e. /* name */
            // because it cannot show sufficient information to locate a section heading.
            // Instead, we use HeadingA/HeadingAA/[[#HeadingAAA]] to represent the full path.
            var sb = new StringBuilder();
            int i = 0;
            if (originalSectionPath != null)
            {
                // Skip the common segments.
                for (; i < sectionPath.Length - 1; i++)
                {
                    if (i >= originalSectionPath.Length || sectionPath[i] != originalSectionPath[i])
                        break;
                }
            }
            if (i > 0) sb.Append(".../");
            for (; i < sectionPath.Length - 1; i++)
            {

                sb.Append(sectionPath[i]);
                sb.Append('/');
            }
            sb.Append("[[#");
            sb.Append(sectionPath.Last());
            sb.Append("]]");
            if (withColon) sb.Append(":");
            return sb.ToString();
        }
    }
}
