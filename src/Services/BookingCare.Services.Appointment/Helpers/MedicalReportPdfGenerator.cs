using System.Text.RegularExpressions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BookingCare.Services.Appointment.Helpers;

/// <summary>
/// Generates professional, minimalist PDF medical reports from markdown-like text
/// Design: Clean, black & white with subtle blue accents
/// </summary>
public static class MedicalReportPdfGenerator
{
    // Timeout for regex operations to prevent ReDoS attacks
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    // Color scheme - Professional & Minimalist
    private static class Colors
    {
        public static readonly string White = "#ffffff"; // White background
        public static readonly string Primary = "#1a1a1a"; // Near black for text
        public static readonly string Secondary = "#666666"; // Gray for labels
        public static readonly string Accent = "#2563eb"; // Professional blue
        public static readonly string Border = "#e5e7eb"; // Light gray for borders
        public static readonly string Background = "#f9fafb"; // Very light gray
    }

    static MedicalReportPdfGenerator()
    {
        // Configure QuestPDF license
        QuestPDF.Settings.License = LicenseType.Community;
    }

    /// <summary>
    /// Generates a professional PDF medical report
    /// </summary>
    public static byte[] GeneratePdfReport(
        string markdownContent,
        string appointmentId,
        string doctorName,
        string patientName,
        DateTime appointmentDate
    )
    {
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.PageColor(Colors.White);
                page.DefaultTextStyle(x => x.FontSize(11).FontColor(Colors.Primary).FontFamily("Arial"));

                page.Header().Element(c => ComposeHeader(c, appointmentId));
                page.Content().Element(c => ComposeContent(
                    c,
                    markdownContent,
                    doctorName,
                    patientName,
                    appointmentDate
                ));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, string appointmentId)
    {
        container.Column(column =>
        {
            column.Spacing(8);

            // Clinic name and logo section
            column.Item().BorderBottom(2).BorderColor(Colors.Accent).PaddingBottom(12).Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("BOOKINGCARE MEDICAL CENTER")
                        .FontSize(18)
                        .Bold()
                        .FontColor(Colors.Primary);

                    col.Item().PaddingTop(4).Text("Trung Tâm Y Tế Chuyên Nghiệp")
                        .FontSize(10)
                        .FontColor(Colors.Secondary);
                });

                row.ConstantItem(150).AlignRight().Text($"Mã hồ sơ: #{appointmentId}")
                    .FontSize(10)
                    .FontColor(Colors.Secondary);
            });

            // Document title
            column.Item().PaddingTop(16).Text("KẾT QUẢ KHÁM BỆNH")
                .FontSize(16)
                .Bold()
                .FontColor(Colors.Accent)
                .AlignCenter();
        });
    }

    private static void ComposeContent(
        IContainer container,
        string markdownContent,
        string doctorName,
        string patientName,
        DateTime appointmentDate
    )
    {
        container.PaddingTop(20).Column(column =>
        {
            column.Spacing(12);

            // Appointment information box
            column.Item().Element(c => ComposeInfoBox(c, doctorName, patientName, appointmentDate));

            // Medical report content
            column.Item().PaddingTop(8).Element(c => ComposeMarkdownContent(c, markdownContent));
        });
    }

    private static void ComposeInfoBox(
        IContainer container,
        string doctorName,
        string patientName,
        DateTime appointmentDate
    )
    {
        container.Background(Colors.Background).Padding(16).Column(column =>
        {
            column.Spacing(10);

            column.Item().Text("THÔNG TIN CUỘC HẸN")
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Accent);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => InfoField(c, "Bác sĩ khám:", doctorName));
                row.RelativeItem().Element(c => InfoField(c, "Bệnh nhân:", patientName));
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(c => InfoField(
                    c,
                    "Ngày khám:",
                    appointmentDate.ToString("dd/MM/yyyy HH:mm")
                ));
                row.RelativeItem().Element(c => InfoField(
                    c,
                    "Ngày in:",
                    DateTime.Now.ToString("dd/MM/yyyy HH:mm")
                ));
            });
        });
    }

    private static void InfoField(IContainer container, string label, string value)
    {
        container.Column(column =>
        {
            column.Item().Text(label)
                .FontSize(9)
                .FontColor(Colors.Secondary);

            column.Item().PaddingTop(2).Text(value)
                .FontSize(11)
                .Bold()
                .FontColor(Colors.Primary);
        });
    }

    private static void ComposeMarkdownContent(IContainer container, string markdownContent)
    {
        if (string.IsNullOrWhiteSpace(markdownContent))
        {
            container.Text("Không có nội dung").FontColor(Colors.Secondary).Italic();
            return;
        }

        var elements = ParseMarkdown(markdownContent);

        container.Column(column =>
        {
            column.Spacing(8);

            foreach (var element in elements)
            {
                column.Item().Element(c => RenderElement(c, element));
            }
        });
    }

    private static void RenderElement(IContainer container, MarkdownElement element)
    {
        switch (element.Type)
        {
            case ElementType.Header2:
                container.PaddingTop(12).BorderBottom(1).BorderColor(Colors.Accent)
                    .PaddingBottom(6).Text(element.Content)
                    .FontSize(14)
                    .Bold()
                    .FontColor(Colors.Accent);
                break;

            case ElementType.Header3:
                container.PaddingTop(8).Text(element.Content)
                    .FontSize(12)
                    .Bold()
                    .FontColor(Colors.Primary);
                break;

            case ElementType.Paragraph:
                container.Text(text =>
                {
                    text.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Colors.Primary).LineHeight(1.5f));
                    ApplyInlineFormatting(text, element.Content);
                });
                break;

            case ElementType.BulletList:
                container.PaddingLeft(16).Column(column =>
                {
                    foreach (var item in element.Items)
                    {
                        column.Item().PaddingBottom(4).Row(row =>
                        {
                            row.ConstantItem(12).Text("•").FontColor(Colors.Accent);
                            row.RelativeItem().Text(text =>
                            {
                                text.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Colors.Primary));
                                ApplyInlineFormatting(text, item);
                            });
                        });
                    }
                });
                break;

            case ElementType.NumberedList:
                container.PaddingLeft(16).Column(column =>
                {
                    for (int i = 0; i < element.Items.Count; i++)
                    {
                        var item = element.Items[i];
                        column.Item().PaddingBottom(4).Row(row =>
                        {
                            row.ConstantItem(20).Text($"{i + 1}.")
                                .FontColor(Colors.Accent)
                                .Bold();
                            row.RelativeItem().Text(text =>
                            {
                                text.DefaultTextStyle(TextStyle.Default.FontSize(11).FontColor(Colors.Primary));
                                ApplyInlineFormatting(text, item);
                            });
                        });
                    }
                });
                break;

            case ElementType.Separator:
                container.PaddingVertical(8).Height(1).Background(Colors.Border);
                break;
        }
    }

    private static void ApplyInlineFormatting(TextDescriptor text, string content)
    {
        // Parse bold (**text**) and italic (*text*)
        var parts = SplitInlineFormatting(content);

        foreach (var part in parts)
        {
            if (part.IsBold)
            {
                text.Span(part.Text).Bold();
            }
            else if (part.IsItalic)
            {
                text.Span(part.Text).Italic().FontColor(Colors.Secondary);
            }
            else
            {
                text.Span(part.Text);
            }
        }
    }

    private static List<InlinePart> SplitInlineFormatting(string content)
    {
        var parts = new List<InlinePart>();
        var current = content;

        // Simple regex to find **bold** and *italic*
        var boldPattern = new Regex(@"\*\*(.+?)\*\*", RegexOptions.None, RegexTimeout);
        var italicPattern = new Regex(@"\*(.+?)\*", RegexOptions.None, RegexTimeout);

        var lastIndex = 0;
        var matches = new List<(int Start, int Length, string Text, bool IsBold, bool IsItalic)>();

        // Find all bold matches
        foreach (Match match in boldPattern.Matches(current))
        {
            matches.Add((match.Index, match.Length, match.Groups[1].Value, true, false));
        }

        // Find all italic matches (not already bold)
        var italicMatches = italicPattern.Matches(current)
            .Where(match => !matches.Any(m => match.Index >= m.Start && match.Index < m.Start + m.Length));

        foreach (Match match in italicMatches)
        {
            matches.Add((match.Index, match.Length, match.Groups[1].Value, false, true));
        }

        // Sort by position
        matches = matches.OrderBy(m => m.Start).ToList();

        foreach (var match in matches)
        {
            // Add text before match
            if (match.Start > lastIndex)
            {
                var plainText = current.Substring(lastIndex, match.Start - lastIndex);
                if (!string.IsNullOrEmpty(plainText))
                {
                    parts.Add(new InlinePart { Text = plainText });
                }
            }

            // Add formatted text
            parts.Add(new InlinePart
            {
                Text = match.Text,
                IsBold = match.IsBold,
                IsItalic = match.IsItalic
            });

            lastIndex = match.Start + match.Length;
        }

        // Add remaining text
        if (lastIndex < current.Length)
        {
            var remaining = current.Substring(lastIndex);
            if (!string.IsNullOrEmpty(remaining))
            {
                parts.Add(new InlinePart { Text = remaining });
            }
        }

        // If no formatting found, return original
        if (parts.Count == 0)
        {
            parts.Add(new InlinePart { Text = content });
        }

        return parts;
    }

    private static List<MarkdownElement> ParseMarkdown(string markdown)
    {
        var elements = new List<MarkdownElement>();
        var lines = markdown.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

        var i = 0;
        while (i < lines.Length)
        {
            var line = lines[i];

            // Skip empty lines
            if (string.IsNullOrWhiteSpace(line))
            {
                i++;
                continue;
            }

            // Try to parse as specific element types
            if (TryParseHeader(line, out var headerElement))
            {
                elements.Add(headerElement);
                i++;
                continue;
            }

            if (TryParseSeparator(line, out var separatorElement))
            {
                elements.Add(separatorElement);
                i++;
                continue;
            }

            if (TryParseBulletList(lines, ref i, out var bulletListElement))
            {
                elements.Add(bulletListElement);
                continue;
            }

            if (TryParseNumberedList(lines, ref i, out var numberedListElement))
            {
                elements.Add(numberedListElement);
                continue;
            }

            // Parse as paragraph
            var paragraphElement = ParseParagraph(lines, ref i);
            elements.Add(paragraphElement);
        }

        return elements;
    }

    private static bool TryParseHeader(string line, out MarkdownElement element)
    {
        element = null!;
        var trimmed = line.TrimStart();

        // Header 2 (##)
        if (trimmed.StartsWith("## "))
        {
            element = new MarkdownElement
            {
                Type = ElementType.Header2,
                Content = trimmed.Substring(3).Trim()
            };
            return true;
        }

        // Header 3 (###)
        if (trimmed.StartsWith("### "))
        {
            element = new MarkdownElement
            {
                Type = ElementType.Header3,
                Content = trimmed.Substring(4).Trim()
            };
            return true;
        }

        return false;
    }

    private static bool TryParseSeparator(string line, out MarkdownElement element)
    {
        element = null!;
        if (Regex.IsMatch(line.Trim(), @"^[-=─═]{3,}$", RegexOptions.None, RegexTimeout))
        {
            element = new MarkdownElement { Type = ElementType.Separator };
            return true;
        }
        return false;
    }

    private static bool TryParseBulletList(string[] lines, ref int i, out MarkdownElement element)
    {
        element = null!;
        var line = lines[i];

        if (!line.TrimStart().StartsWith("• "))
        {
            return false;
        }

        var items = new List<string>();
        while (i < lines.Length && lines[i].TrimStart().StartsWith("• "))
        {
            items.Add(lines[i].TrimStart().Substring(2).Trim());
            i++;
        }

        element = new MarkdownElement
        {
            Type = ElementType.BulletList,
            Items = items
        };
        return true;
    }

    private static bool TryParseNumberedList(string[] lines, ref int i, out MarkdownElement element)
    {
        element = null!;
        var line = lines[i];

        if (!Regex.IsMatch(line.TrimStart(), @"^\d+\.\s", RegexOptions.None, RegexTimeout))
        {
            return false;
        }

        var items = new List<string>();
        while (i < lines.Length && Regex.IsMatch(
            lines[i].TrimStart(),
            @"^\d+\.\s",
            RegexOptions.None,
            RegexTimeout
        ))
        {
            var match = Regex.Match(
                lines[i].TrimStart(),
                @"^\d+\.\s(.+)$",
                RegexOptions.None,
                RegexTimeout
            );
            if (match.Success)
            {
                items.Add(match.Groups[1].Value.Trim());
            }
            i++;
        }

        element = new MarkdownElement
        {
            Type = ElementType.NumberedList,
            Items = items
        };
        return true;
    }

    private static MarkdownElement ParseParagraph(string[] lines, ref int i)
    {
        var paragraphLines = new List<string> { lines[i] };
        i++;

        while (i < lines.Length && IsParagraphContinuation(lines[i]))
        {
            paragraphLines.Add(lines[i]);
            i++;
        }

        return new MarkdownElement
        {
            Type = ElementType.Paragraph,
            Content = string.Join(" ", paragraphLines)
        };
    }

    private static bool IsParagraphContinuation(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var trimmed = line.TrimStart();

        // Check if line starts with special markdown characters
        if (trimmed.StartsWith('#') || trimmed.StartsWith("• "))
            return false;

        // Check if it's a numbered list or separator
        if (Regex.IsMatch(trimmed, @"^\d+\.\s", RegexOptions.None, RegexTimeout))
            return false;

        if (Regex.IsMatch(line.Trim(), @"^[-=─═]{3,}$", RegexOptions.None, RegexTimeout))
            return false;

        return true;
    }

    private static void ComposeFooter(IContainer container)
    {
        container.AlignCenter().Column(column =>
        {
            column.Spacing(4);

            column.Item().BorderTop(1).BorderColor(Colors.Border).PaddingTop(12);

            column.Item().Text("BookingCare - Nền tảng đặt lịch khám bệnh trực tuyến hàng đầu Việt Nam")
                .FontSize(9)
                .FontColor(Colors.Secondary);

            column.Item().Text($"Tạo lúc: {DateTime.Now:dd/MM/yyyy HH:mm:ss}")
                .FontSize(8)
                .FontColor(Colors.Secondary);
        });
    }

    // Helper classes
    private sealed class MarkdownElement
    {
        public ElementType Type { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string> Items { get; set; } = new();
    }

    private enum ElementType
    {
        Header2,
        Header3,
        Paragraph,
        BulletList,
        NumberedList,
        Separator
    }

    private sealed class InlinePart
    {
        public string Text { get; set; } = string.Empty;
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
    }
}
