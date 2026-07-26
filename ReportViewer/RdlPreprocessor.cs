using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace ReportVisualizer.ReportViewer
{
    public class RdlPreprocessOptions
    {
        public bool ForceRepeatHeaderRowsOnEveryPage { get; set; } = true;
        public bool PromotePageFooterToBodyForExcel { get; set; } = false;
        public bool PromotePageHeaderToBodyForExcel { get; set; } = false;
    }

    public static class RdlPreprocessor
    {
        private static readonly XNamespace RdlNs2016 = "http://schemas.microsoft.com/sqlserver/reporting/2016/01/reportdefinition";
        private static readonly XNamespace RdlNs2010 = "http://schemas.microsoft.com/sqlserver/reporting/2010/01/reportdefinition";
        private static readonly XNamespace RdlNs2008 = "http://schemas.microsoft.com/sqlserver/reporting/2008/01/reportdefinition";

        public static Stream PreprocessFile(string reportPath, RdlPreprocessOptions options)
        {
            if (!File.Exists(reportPath)) throw new FileNotFoundException("Report file not found.", reportPath);
            var xml = File.ReadAllText(reportPath);
            return PreprocessXml(xml, options);
        }

        public static Stream PreprocessXml(string xml, RdlPreprocessOptions options)
        {
            var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            var ns = DetectNamespace(doc);
            if (options == null) options = new RdlPreprocessOptions();

            if (options.ForceRepeatHeaderRowsOnEveryPage)
                ApplyRepeatHeaderOnEveryPage(doc, ns);

            if (options.PromotePageHeaderToBodyForExcel)
                PromotePageSection(doc, ns, "PageHeader");

            if (options.PromotePageFooterToBodyForExcel)
                PromotePageSection(doc, ns, "PageFooter");

            var ms = new MemoryStream();
            var writerSettings = new System.Xml.XmlWriterSettings
            {
                Encoding = System.Text.Encoding.UTF8,
                Indent = true,
                OmitXmlDeclaration = false,
                NewLineHandling = System.Xml.NewLineHandling.Replace
            };
            using (var w = System.Xml.XmlWriter.Create(ms, writerSettings))
            {
                doc.Save(w);
            }
            ms.Position = 0;
            return ms;
        }

        private static XNamespace DetectNamespace(XDocument doc)
        {
            var root = doc?.Root;
            if (root == null) return RdlNs2016;
            if (root.Name.NamespaceName?.Length > 0) return root.Name.Namespace;
            return RdlNs2016;
        }

        private static XElement Element(XContainer parent, XNamespace ns, string localName)
        {
            return parent?.Elements(ns + localName).FirstOrDefault();
        }

        private static IEnumerable<XElement> Elements(XContainer parent, XNamespace ns, string localName)
        {
            return parent?.Elements(ns + localName) ?? Enumerable.Empty<XElement>();
        }

        private static IEnumerable<XElement> Descendants(XContainer parent, XNamespace ns, string localName)
        {
            return parent?.Descendants(ns + localName) ?? Enumerable.Empty<XElement>();
        }

        private static string Attr(XElement e, string name)
        {
            return e?.Attribute(name)?.Value;
        }

        private static void SetAttr(XElement e, string name, string value)
        {
            if (e == null) return;
            var a = e.Attribute(name);
            if (a == null) e.Add(new XAttribute(name, value));
            else a.Value = value;
        }

        private static void ApplyRepeatHeaderOnEveryPage(XDocument doc, XNamespace ns)
        {
            var report = doc.Root;
            if (report == null) return;

            // Report-level properties that help rendering with repeated headers
            var body = Element(report, ns, "Body");
            if (body != null)
            {
                // No specific body attributes required for repeat headers; keep for future.
            }

            // Force ConsumeContainerWhitespace if not present
            var consume = Element(report, ns, "ConsumeContainerWhitespace");
            if (consume == null)
            {
                report.AddFirst(new XElement(ns + "ConsumeContainerWhitespace", "true"));
            }
            else if (!string.Equals(consume.Value, "true", StringComparison.OrdinalIgnoreCase))
            {
                consume.Value = "true";
            }

            foreach (var tablix in Descendants(report, ns, "Tablix"))
            {
                var rowHierarchy = Element(tablix, ns, "TablixRowHierarchy");
                ProcessTablixMembersForHeaders(rowHierarchy, ns, hierarchyIsColumns: false);

                var colHierarchy = Element(tablix, ns, "TablixColumnHierarchy");
                ProcessTablixMembersForHeaders(colHierarchy, ns, hierarchyIsColumns: true);
            }
        }

        private static void ProcessTablixMembersForHeaders(XElement hierarchy, XNamespace ns, bool hierarchyIsColumns)
        {
            if (hierarchy == null) return;
            // TablixMembers may be nested: inspect each TablixMember
            foreach (var member in hierarchy.Descendants(ns + "TablixMember"))
            {
                var group = Element(member, ns, "Group");
                if (group != null) continue; // group members are dynamic; static members only
                // Static member → header candidate. For row hierarchy: first (outermost) static members correspond to headers.
                // Apply RepeatOnNewPage, KeepWithGroup, FixedData if not already set appropriately
                if (!hierarchyIsColumns)
                {
                    SetAttr(member, "RepeatOnNewPage", "true");
                    var keepWith = Attr(member, "KeepWithGroup");
                    if (string.IsNullOrWhiteSpace(keepWith)) SetAttr(member, "KeepWithGroup", "After");
                    var fixedData = Attr(member, "FixedData");
                    if (string.IsNullOrWhiteSpace(fixedData)) SetAttr(member, "FixedData", "true");
                }
                else
                {
                    SetAttr(member, "RepeatOnNewPage", "true");
                    var keepWith = Attr(member, "KeepWithGroup");
                    if (string.IsNullOrWhiteSpace(keepWith)) SetAttr(member, "KeepWithGroup", "After");
                }
            }
        }

        private static void PromotePageSection(XDocument doc, XNamespace ns, string sectionName)
        {
            var report = doc.Root;
            if (report == null) return;
            var page = Element(report, ns, "Page");
            if (page == null) return;
            var section = Element(page, ns, sectionName);
            if (section == null) return;
            var body = Element(report, ns, "Body");
            if (body == null) return;

            // Ensure the section is not already promoted (avoid duplication)
            string sectionMarker = sectionName + "_PromotedByPreprocessor";
            // Mark the section with an element name suffix via id
            var reportItems = Element(section, ns, "ReportItems");
            if (reportItems == null || !reportItems.HasElements) return;

            // Get current body height (try to read Body/Height)
            var bodyHeightEl = Element(body, ns, "Height");
            var bodyHeight = ParseRdlSize(bodyHeightEl?.Value);
            var sectionHeightEl = Element(section, ns, "Height");
            var sectionHeight = ParseRdlSize(sectionHeightEl?.Value);

            // Clone the report items into a Rectangle appended to body
            var promotedContainer = new XElement(ns + "Rectangle",
                new XAttribute("Name", sectionMarker),
                new XElement(ns + "Top", FormatRdlSize(bodyHeight)),
                new XElement(ns + "Left", "0in"),
                new XElement(ns + "Height", FormatRdlSize(sectionHeight ?? new RdlSize(0.5, Unit.In))),
                new XElement(ns + "Width", "10in"),
                new XElement(ns + "KeepTogether", "true")
            );

            var promotedReportItems = new XElement(ns + "ReportItems");
            foreach (var item in reportItems.Elements())
            {
                var clone = new XElement(item);
                // Make sure we don't reference parent page section space by adjusting Top relative to container
                var topEl = clone.Element(ns + "Top");
                if (topEl == null) clone.Add(new XElement(ns + "Top", "0in"));
                promotedReportItems.Add(clone);
            }
            promotedContainer.Add(promotedReportItems);

            var bodyReportItems = Element(body, ns, "ReportItems");
            if (bodyReportItems == null)
            {
                bodyReportItems = new XElement(ns + "ReportItems");
                body.Add(bodyReportItems);
            }
            bodyReportItems.Add(promotedContainer);

            // Increase body height so Excel renderer doesn't clip promoted footer/header
            var newHeight = AddSizes(AddSizes(bodyHeight ?? new RdlSize(0, Unit.In), sectionHeight ?? new RdlSize(0.5, Unit.In)), new RdlSize(0.05, Unit.In));
            if (bodyHeightEl == null)
            {
                body.Add(new XElement(ns + "Height", FormatRdlSize(newHeight)));
            }
            else
            {
                bodyHeightEl.Value = FormatRdlSize(newHeight);
            }

            // Keep original PageHeader/PageFooter for non-Excel renders, but mark it to not render on print
            var printOnFirst = Element(section, ns, "PrintOnFirstPage");
            if (printOnFirst == null) section.Add(new XElement(ns + "PrintOnFirstPage", "false"));
            else printOnFirst.Value = "false";
            var printOnLast = Element(section, ns, "PrintOnLastPage");
            if (printOnLast == null) section.Add(new XElement(ns + "PrintOnLastPage", "false"));
            else printOnLast.Value = "false";
        }

        private enum Unit { In, Cm, Mm, Pt, Pc, Px }

        private class RdlSize
        {
            public double Value { get; set; }
            public Unit Unit { get; set; }
            public RdlSize(double value, Unit unit) { Value = value; Unit = unit; }
        }

        private static RdlSize ParseRdlSize(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var s = value.Trim();
            if (s.EndsWith("in", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.In);
            if (s.EndsWith("cm", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.Cm);
            if (s.EndsWith("mm", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.Mm);
            if (s.EndsWith("pt", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.Pt);
            if (s.EndsWith("pc", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.Pc);
            if (s.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                return new RdlSize(double.TryParse(s.Substring(0, s.Length - 2), out var d) ? d : 0, Unit.Px);
            return double.TryParse(s, out var dd) ? new RdlSize(dd, Unit.In) : null;
        }

        private static double ToInches(RdlSize size)
        {
            if (size == null) return 0;
            switch (size.Unit)
            {
                case Unit.In: return size.Value;
                case Unit.Cm: return size.Value / 2.54;
                case Unit.Mm: return size.Value / 25.4;
                case Unit.Pt: return size.Value / 72.0;
                case Unit.Pc: return size.Value / 6.0;
                case Unit.Px: return size.Value / 96.0;
                default: return size.Value;
            }
        }

        private static string FormatRdlSize(RdlSize size)
        {
            var inches = ToInches(size);
            return inches.ToString("0.0000", System.Globalization.CultureInfo.InvariantCulture) + "in";
        }

        private static RdlSize AddSizes(RdlSize a, RdlSize b)
        {
            var inches = ToInches(a) + ToInches(b);
            return new RdlSize(inches, Unit.In);
        }
    }
}
