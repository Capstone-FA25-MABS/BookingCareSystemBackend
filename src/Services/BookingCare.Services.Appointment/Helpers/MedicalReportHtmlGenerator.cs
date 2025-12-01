using System.Text;
using System.Text.RegularExpressions;

namespace BookingCare.Services.Appointment.Helpers;

/// <summary>
/// Generates beautiful, professional HTML medical reports from markdown-like text
/// </summary>
public static class MedicalReportHtmlGenerator
{
    // Timeout for regex operations to prevent ReDoS attacks
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

    // HTML constants to avoid duplication
    private const string HtmlInfoItemStart = "                <div class=\"info-item\">";
    private const string HtmlInfoItemEnd = "                </div>";
    private const string HtmlDivEnd = "        </div>";

    /// <summary>
    /// CSS styles for the medical report HTML
    /// </summary>
    private const string CssStyles =
        @"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            line-height: 1.6;
            color: #333;
        }

        .container {
            max-width: 900px;
            margin: 0 auto;
            background: white;
            border-radius: 16px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            overflow: hidden;
        }

        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
            position: relative;
        }

        .logo-section {
            margin-bottom: 15px;
        }

        .logo {
            font-size: 48px;
            margin-bottom: 10px;
            animation: pulse 2s ease-in-out infinite;
        }

        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.05); }
        }

        .clinic-name {
            font-size: 28px;
            font-weight: bold;
            letter-spacing: 2px;
            text-shadow: 2px 2px 4px rgba(0, 0, 0, 0.2);
        }

        .clinic-subtitle {
            font-size: 14px;
            opacity: 0.9;
            margin-top: 5px;
        }

        .report-id {
            font-size: 14px;
            opacity: 0.95;
            margin-top: 15px;
            padding: 8px 20px;
            background: rgba(255, 255, 255, 0.2);
            border-radius: 20px;
            display: inline-block;
        }

        .report-id span {
            font-weight: bold;
            font-size: 16px;
        }

        .info-card {
            background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
            margin: 30px;
            padding: 25px;
            border-radius: 12px;
            box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
        }

        .info-title {
            font-size: 20px;
            font-weight: bold;
            color: #667eea;
            margin-bottom: 20px;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .info-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
            gap: 15px;
        }

        .info-item {
            display: flex;
            flex-direction: column;
            gap: 5px;
        }

        .info-label {
            font-size: 13px;
            color: #666;
            font-weight: 500;
        }

        .info-value {
            font-size: 16px;
            font-weight: 600;
            color: #333;
        }

        .report-content {
            padding: 30px;
        }

        .section-title {
            font-size: 22px;
            color: #667eea;
            margin-top: 30px;
            margin-bottom: 15px;
            padding-bottom: 10px;
            border-bottom: 3px solid #667eea;
            display: flex;
            align-items: center;
            gap: 10px;
        }

        .section-title:first-child {
            margin-top: 0;
        }

        .subsection-title {
            font-size: 18px;
            color: #764ba2;
            margin-top: 20px;
            margin-bottom: 10px;
            font-weight: 600;
        }

        .separator {
            border: none;
            border-top: 2px dashed #ddd;
            margin: 25px 0;
        }

        p {
            margin-bottom: 12px;
            color: #555;
            font-size: 15px;
        }

        strong {
            color: #333;
            font-weight: 600;
        }

        em {
            color: #888;
            font-style: italic;
        }

        .bullet-list {
            list-style: none;
            margin: 15px 0;
            padding-left: 0;
        }

        .bullet-list li {
            padding: 10px 15px;
            margin-bottom: 8px;
            background: #f8f9fa;
            border-left: 4px solid #667eea;
            border-radius: 4px;
            position: relative;
        }

        .bullet-list li::before {
            content: '✓';
            color: #667eea;
            font-weight: bold;
            margin-right: 10px;
        }

        .numbered-list {
            margin: 15px 0;
            padding-left: 0;
            counter-reset: item;
            list-style: none;
        }

        .numbered-item {
            padding: 12px 15px 12px 50px;
            margin-bottom: 10px;
            background: #fff3cd;
            border-left: 4px solid #ffc107;
            border-radius: 4px;
            position: relative;
            counter-increment: item;
        }

        .numbered-item::before {
            content: counter(item);
            position: absolute;
            left: 15px;
            top: 50%;
            transform: translateY(-50%);
            background: #ffc107;
            color: white;
            width: 28px;
            height: 28px;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            font-size: 14px;
        }

        .footer {
            background: #f8f9fa;
            padding: 25px 30px;
            text-align: center;
            border-top: 3px solid #667eea;
        }

        .footer-note {
            background: #fff3cd;
            padding: 15px;
            border-radius: 8px;
            border-left: 4px solid #ffc107;
            margin-bottom: 15px;
            font-size: 14px;
            color: #856404;
            text-align: left;
        }

        .footer-info {
            font-size: 13px;
            color: #666;
            margin-top: 8px;
        }

        /* Print styles */
        @media print {
            body {
                background: white;
                padding: 0;
            }

            .container {
                box-shadow: none;
                max-width: 100%;
            }

            .header {
                background: #667eea !important;
                -webkit-print-color-adjust: exact;
                print-color-adjust: exact;
            }

            .section-title {
                page-break-after: avoid;
            }

            .bullet-list li,
            .numbered-item {
                page-break-inside: avoid;
            }
        }

        /* Responsive */
        @media (max-width: 768px) {
            .container {
                border-radius: 0;
                margin: -20px;
            }

            .header {
                padding: 30px 20px;
            }

            .clinic-name {
                font-size: 22px;
            }

            .info-card,
            .report-content {
                padding: 20px;
                margin: 20px;
            }

            .info-grid {
                grid-template-columns: 1fr;
            }

            .section-title {
                font-size: 20px;
            }
        }
        ";

    /// <summary>
    /// Converts markdown-like medical report text to a beautiful HTML document
    /// </summary>
    public static string GenerateHtmlReport(
        string markdownContent,
        string appointmentId,
        string doctorName,
        string patientName,
        DateTime appointmentDate
    )
    {
        var htmlContent = ConvertMarkdownToHtml(markdownContent);

        var html = new StringBuilder();

        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"vi\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine(
            "    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">"
        );
        html.AppendLine($"    <title>Kết Quả Khám Bệnh - {appointmentId}</title>");
        html.AppendLine("    <style>");
        html.AppendLine(CssStyles);
        html.AppendLine("    </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // Header
        html.AppendLine("        <div class=\"header\">");
        html.AppendLine("            <div class=\"logo-section\">");
        html.AppendLine(
            "                <div class=\"clinic-name\">BOOKINGCARE MEDICAL CENTER</div>"
        );
        html.AppendLine(
            "                <div class=\"clinic-subtitle\">Trung Tâm Y Tế Chuyên Nghiệp</div>"
        );
        html.AppendLine("            </div>");
        html.AppendLine(
            $"            <div class=\"report-id\">Mã hồ sơ: <span>#{appointmentId}</span></div>"
        );
        html.AppendLine(HtmlDivEnd);

        // Patient Info Card
        html.AppendLine("        <div class=\"info-card\">");
        html.AppendLine("            <div class=\"info-title\">Thông Tin Cuộc Hẹn</div>");
        html.AppendLine("            <div class=\"info-grid\">");
        html.AppendLine(HtmlInfoItemStart);
        html.AppendLine(
            "                    <span class=\"info-label\">\u200D Bác sĩ khám:</span>"
        );
        html.AppendLine($"                    <span class=\"info-value\">{doctorName}</span>");
        html.AppendLine(HtmlInfoItemEnd);
        html.AppendLine(HtmlInfoItemStart);
        html.AppendLine("                    <span class=\"info-label\"> Bệnh nhân:</span>");
        html.AppendLine($"                    <span class=\"info-value\">{patientName}</span>");
        html.AppendLine(HtmlInfoItemEnd);
        html.AppendLine(HtmlInfoItemStart);
        html.AppendLine("                    <span class=\"info-label\"> Ngày khám:</span>");
        html.AppendLine(
            $"                    <span class=\"info-value\">{appointmentDate:dd/MM/yyyy HH:mm}</span>"
        );
        html.AppendLine(HtmlInfoItemEnd);
        html.AppendLine(HtmlInfoItemStart);
        html.AppendLine("                    <span class=\"info-label\"> Ngày in:</span>");
        html.AppendLine(
            $"                    <span class=\"info-value\">{DateTime.Now:dd/MM/yyyy HH:mm}</span>"
        );
        html.AppendLine(HtmlInfoItemEnd);
        html.AppendLine("            </div>");
        html.AppendLine(HtmlDivEnd);

        // Medical Report Content
        html.AppendLine("        <div class=\"report-content\">");
        html.AppendLine(htmlContent);
        html.AppendLine(HtmlDivEnd);

        // Footer
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine(
            "            <div class=\"footer-info\">BookingCare - Nền tảng đặt lịch khám bệnh trực tuyến hàng đầu Việt Nam</div>"
        );
        html.AppendLine(
            $"            <div class=\"footer-info\">Tạo lúc: {DateTime.Now:dd/MM/yyyy HH:mm:ss}</div>"
        );
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        return html.ToString();
    }

    /// <summary>
    /// Converts markdown-like syntax to HTML
    /// </summary>
    private static string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return "";

        var html = markdown;

        // Convert headers with emojis (## 🩺 TITLE -> <h2>🩺 TITLE</h2>)
        html = Regex.Replace(
            html,
            @"##\s+(.+?)(\r?\n|$)",
            "<h2 class=\"section-title\">$1</h2>\n",
            RegexOptions.Multiline,
            RegexTimeout
        );

        // Convert subheaders (### TITLE -> <h3>TITLE</h3>)
        html = Regex.Replace(
            html,
            @"###\s+(.+?)(\r?\n|$)",
            "<h3 class=\"subsection-title\">$1</h3>\n",
            RegexOptions.Multiline,
            RegexTimeout
        );

        // Convert bold text (**text** -> <strong>text</strong>)
        html = Regex.Replace(
            html,
            @"\*\*(.+?)\*\*",
            "<strong>$1</strong>",
            RegexOptions.None,
            RegexTimeout
        );

        // Convert italic text (*text* -> <em>text</em>)
        html = Regex.Replace(html, @"\*(.+?)\*", "<em>$1</em>", RegexOptions.None, RegexTimeout);

        // Convert bullet points (• text -> <li>text</li>)
        html = Regex.Replace(
            html,
            @"^•\s+(.+?)$",
            "<li>$1</li>",
            RegexOptions.Multiline,
            RegexTimeout
        );

        // Wrap consecutive <li> tags in <ul>
        html = Regex.Replace(
            html,
            @"(<li>.*?</li>\s*)+",
            m => @"<ul class=""bullet-list"">" + "\n" + m.Value + @"</ul>" + "\n",
            RegexOptions.Singleline,
            RegexTimeout
        );

        // Convert numbered lists (1. text -> <li>text</li> in <ol>)
        html = Regex.Replace(
            html,
            @"^\d+\.\s+(.+?)$",
            "<li class=\"numbered-item\">$1</li>",
            RegexOptions.Multiline,
            RegexTimeout
        );

        // Wrap consecutive numbered <li> tags in <ol>
        html = Regex.Replace(
            html,
            @"(<li class=""numbered-item"">.*?</li>\s*)+",
            m => @"<ol class=""numbered-list"">" + "\n" + m.Value + @"</ol>" + "\n",
            RegexOptions.Singleline,
            RegexTimeout
        );

        // Convert separator lines (─── or ═══)
        html = Regex.Replace(
            html,
            @"^[─═]{3,}$",
            "<hr class=\"separator\">",
            RegexOptions.Multiline,
            RegexTimeout
        );

        // Convert double line breaks to paragraphs
        html = Regex.Replace(html, @"(\r?\n){2,}", "</p>\n<p>", RegexOptions.None, RegexTimeout);

        // Wrap in paragraph tags
        html = "<p>" + html + "</p>";

        // Clean up empty paragraphs
        html = Regex.Replace(html, @"<p>\s*</p>", "", RegexOptions.None, RegexTimeout);

        // Clean up paragraphs that only contain block elements
        html = Regex.Replace(
            html,
            @"<p>\s*(<h[23]|<hr|<ul|<ol)",
            "$1",
            RegexOptions.Multiline,
            RegexTimeout
        );
        html = Regex.Replace(
            html,
            @"(</h[23]>|</ul>|</ol>)\s*</p>",
            "$1",
            RegexOptions.Multiline,
            RegexTimeout
        );

        return html;
    }
}
