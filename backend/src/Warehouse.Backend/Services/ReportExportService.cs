using System.Globalization;
using System.Text;
using System.Text.Json;
using Warehouse.Backend.Contracts;

namespace Warehouse.Backend.Services;

public static class ReportExportService
{
    public static byte[] BuildCsv(ConsumptionReportDto report)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Month,ScopeType,ScopeId,Label,MachineId,MachineName,ZoneId,ZoneName,PeriodStart,PeriodEnd,AverageKwh,TotalKwh,CostEuro");

        foreach (var row in report.Rows)
        {
            builder.AppendLine(string.Join(",",
                EscapeCsv(report.Month),
                EscapeCsv(row.ScopeType),
                EscapeCsv(row.ScopeId),
                EscapeCsv(row.Label),
                EscapeCsv(row.MachineId),
                EscapeCsv(row.MachineName),
                EscapeCsv(row.ZoneId),
                EscapeCsv(row.ZoneName),
                EscapeCsv(row.PeriodStart.ToString("O", CultureInfo.InvariantCulture)),
                EscapeCsv(row.PeriodEnd.ToString("O", CultureInfo.InvariantCulture)),
                row.AverageKwh.ToString(CultureInfo.InvariantCulture),
                row.TotalKwh.ToString(CultureInfo.InvariantCulture),
                row.CostEuro.ToString(CultureInfo.InvariantCulture)));
        }

        return Encoding.UTF8.GetBytes(builder.ToString());
    }

    public static byte[] BuildPdf(ConsumptionReportDto report)
    {
        var lines = new List<string>
        {
            "vR-CoreSched Consumption Report",
            $"Month: {report.Month}",
            $"Machine: {report.MachineId ?? "All"}",
            $"Zone: {report.ZoneId ?? "All"}",
            $"Total KWh: {report.TotalKwh.ToString("0.00", CultureInfo.InvariantCulture)}",
            $"Total Cost: {report.TotalCostEuro.ToString("0.00", CultureInfo.InvariantCulture)} EUR",
            string.Empty,
            "Rows:"
        };

        foreach (var row in report.Rows.Take(26))
        {
            lines.Add($"{row.PeriodStart:yyyy-MM-dd} | {row.Label} | {row.TotalKwh:0.00} kWh | {row.CostEuro:0.00} EUR");
        }

        return BuildMinimalPdf(lines);
    }

    private static byte[] BuildMinimalPdf(IReadOnlyList<string> lines)
    {
        var contentLines = new StringBuilder();
        contentLines.AppendLine("BT");
        contentLines.AppendLine("/F1 11 Tf");
        contentLines.AppendLine("72 760 Td");

        var first = true;
        foreach (var line in lines)
        {
            var safeLine = EscapePdfText(ToAscii(line));
            if (first)
            {
                contentLines.AppendLine($"({safeLine}) Tj");
                first = false;
            }
            else
            {
                contentLines.AppendLine("0 -14 Td");
                contentLines.AppendLine($"({safeLine}) Tj");
            }
        }

        contentLines.AppendLine("ET");

        var contentBytes = Encoding.ASCII.GetBytes(contentLines.ToString());
        var pdf = new List<byte>();
        var offsets = new List<int> { 0 };

        void AppendObject(string text)
        {
            offsets.Add(pdf.Count);
            pdf.AddRange(Encoding.ASCII.GetBytes(text));
        }

        pdf.AddRange(Encoding.ASCII.GetBytes("%PDF-1.4\n"));
        AppendObject("1 0 obj\n<< /Type /Catalog /Pages 2 0 R >>\nendobj\n");
        AppendObject("2 0 obj\n<< /Type /Pages /Kids [3 0 R] /Count 1 >>\nendobj\n");
        AppendObject("3 0 obj\n<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>\nendobj\n");
        AppendObject("4 0 obj\n<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>\nendobj\n");
        AppendObject($"5 0 obj\n<< /Length {contentBytes.Length} >>\nstream\n");
        pdf.AddRange(contentBytes);
        pdf.AddRange(Encoding.ASCII.GetBytes("\nendstream\nendobj\n"));

        var xrefStart = pdf.Count;
        var xref = new StringBuilder();
        xref.AppendLine("xref");
        xref.AppendLine($"0 {offsets.Count}");
        xref.AppendLine("0000000000 65535 f ");

        for (var index = 1; index < offsets.Count; index++)
        {
            xref.AppendLine($"{offsets[index]:0000000000} 00000 n ");
        }

        xref.AppendLine("trailer");
        xref.AppendLine($"<< /Size {offsets.Count} /Root 1 0 R >>");
        xref.AppendLine("startxref");
        xref.AppendLine(xrefStart.ToString(CultureInfo.InvariantCulture));
        xref.AppendLine("%%EOF");
        pdf.AddRange(Encoding.ASCII.GetBytes(xref.ToString()));

        return pdf.ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static string EscapePdfText(string value) => value.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");

    private static string ToAscii(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            builder.Append(character <= 127 ? character : '?');
        }

        return builder.ToString();
    }
}