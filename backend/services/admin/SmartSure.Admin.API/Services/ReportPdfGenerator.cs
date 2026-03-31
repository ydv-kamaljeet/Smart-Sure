using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SmartSure.Admin.Application.DTOs;

namespace SmartSure.Admin.API.Services;

public static class ReportPdfGenerator
{
    static ReportPdfGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    // ── Claims Summary ────────────────────────────────────────────────────────
    public static byte[] GenerateClaimsReport(IEnumerable<AdminClaimDto> claims, string title)
    {
        var list = claims.ToList();
        var approved   = list.Count(c => c.Status == "Approved");
        var rejected   = list.Count(c => c.Status == "Rejected");
        var pending    = list.Count(c => c.Status is "Submitted" or "Pending");
        var review     = list.Count(c => c.Status is "Under Review" or "UnderReview");
        var totalAmt   = list.Sum(c => c.ClaimAmount);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(BuildHeader(title, "Claims Summary Report"));
            page.Content().Column(col =>
            {
                // KPI row
                col.Item().Row(row =>
                {
                    SummaryCard(row, "Total Claims",  list.Count.ToString(),   "#1a56db");
                    SummaryCard(row, "Approved",      approved.ToString(),     "#10b981");
                    SummaryCard(row, "Rejected",      rejected.ToString(),     "#ef4444");
                    SummaryCard(row, "Under Review",  review.ToString(),       "#3b82f6");
                    SummaryCard(row, "Pending",       pending.ToString(),      "#f59e0b");
                    SummaryCard(row, "Total Amount",  $"₹{totalAmt:N0}",       "#7c3aed");
                });

                col.Item().PaddingTop(16).Text("Claim Details").FontSize(12).SemiBold();
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);   // #
                        c.ConstantColumn(110);  // Claim #
                        c.RelativeColumn(2);    // Customer
                        c.RelativeColumn(1.5f); // Policy #
                        c.ConstantColumn(85);   // Amount
                        c.ConstantColumn(85);   // Status
                        c.ConstantColumn(85);   // Incident Date
                        c.ConstantColumn(85);   // Filed On
                    });
                    table.Header(h =>
                    {
                        foreach (var lbl in new[] { "#", "Claim #", "Customer", "Policy #", "Amount", "Status", "Incident Date", "Filed On" })
                            h.Cell().Background("#1a56db").Padding(6).Text(lbl).FontColor(Colors.White).SemiBold().FontSize(9);
                    });
                    var i = 1; var alt = false;
                    foreach (var c in list)
                    {
                        var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                        alt = !alt;
                        var statusColor = c.Status switch
                        {
                            "Approved"    => "#10b981",
                            "Rejected"    => "#ef4444",
                            "Under Review"=> "#3b82f6",
                            _             => "#f59e0b"
                        };
                        table.Cell().Background(bg).Padding(5).Text(i++.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.ClaimNumber).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.CustomerName).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.PolicyNumber).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text($"₹{c.ClaimAmount:N0}").FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.Status).FontColor(statusColor).SemiBold().FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.IncidentDate.ToString("MMM d, yyyy")).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(c.CreatedAt.ToString("MMM d, yyyy")).FontSize(9);
                    }
                });
            });
            page.Footer().Element(BuildFooter());
        })).GeneratePdf();
    }

    // ── Policy Summary ────────────────────────────────────────────────────────
    public static byte[] GeneratePoliciesReport(IEnumerable<AdminPolicyDto> policies, string title)
    {
        var list   = policies.ToList();
        var active = list.Count(p => p.Status == "Active");
        var total  = list.Sum(p => p.PremiumAmount);

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(BuildHeader(title, "Policy Summary Report"));
            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    SummaryCard(row, "Total Policies", list.Count.ToString(),  "#1a56db");
                    SummaryCard(row, "Active",         active.ToString(),      "#10b981");
                    SummaryCard(row, "Inactive",       (list.Count - active).ToString(), "#ef4444");
                    SummaryCard(row, "Total Revenue",  $"₹{total:N0}",         "#7c3aed");
                });

                col.Item().PaddingTop(16).Text("Policy Details").FontSize(12).SemiBold();
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);
                        c.ConstantColumn(110);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                        c.ConstantColumn(90);
                        c.ConstantColumn(80);
                    });
                    table.Header(h =>
                    {
                        foreach (var lbl in new[] { "#", "Policy #", "Customer", "Insurance Type", "Premium", "Status" })
                            h.Cell().Background("#1a56db").Padding(6).Text(lbl).FontColor(Colors.White).SemiBold().FontSize(9);
                    });
                    var i = 1; var alt = false;
                    foreach (var p in list)
                    {
                        var bg = alt ? Colors.Grey.Lighten4 : Colors.White;
                        alt = !alt;
                        var statusColor = p.Status == "Active" ? "#10b981" : "#ef4444";
                        table.Cell().Background(bg).Padding(5).Text(i++.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.PolicyNumber).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.CustomerName).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.InsuranceType).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text($"₹{p.PremiumAmount:N0}").FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.Status).FontColor(statusColor).SemiBold().FontSize(9);
                    }
                });
            });
            page.Footer().Element(BuildFooter());
        })).GeneratePdf();
    }

    // ── Revenue Report ────────────────────────────────────────────────────────
    public static byte[] GenerateRevenueReport(IEnumerable<AdminPolicyDto> policies, string title)
    {
        var list  = policies.ToList();
        var total = list.Sum(p => p.PremiumAmount);
        var byType = list.GroupBy(p => p.InsuranceType)
            .Select(g => new { Type = g.Key, Count = g.Count(), Revenue = g.Sum(p => p.PremiumAmount) })
            .OrderByDescending(x => x.Revenue).ToList();

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(BuildHeader(title, "Total Revenue Report"));
            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    SummaryCard(row, "Total Revenue",  $"₹{total:N0}",        "#10b981");
                    SummaryCard(row, "Total Policies", list.Count.ToString(), "#1a56db");
                    SummaryCard(row, "Avg Premium",    list.Count > 0 ? $"₹{total / list.Count:N0}" : "₹0", "#7c3aed");
                });

                // Revenue by type breakdown
                col.Item().PaddingTop(16).Text("Revenue by Insurance Type").FontSize(12).SemiBold();
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.RelativeColumn(2);
                        c.ConstantColumn(90);
                        c.ConstantColumn(120);
                        c.ConstantColumn(80);
                    });
                    table.Header(h =>
                    {
                        foreach (var lbl in new[] { "Insurance Type", "Policies", "Revenue", "Share %" })
                            h.Cell().Background("#1a56db").Padding(6).Text(lbl).FontColor(Colors.White).SemiBold().FontSize(9);
                    });
                    var alt = false;
                    foreach (var r in byType)
                    {
                        var bg = alt ? Colors.Grey.Lighten4 : Colors.White; alt = !alt;
                        var share = total > 0 ? (r.Revenue / total * 100).ToString("N1") : "0";
                        table.Cell().Background(bg).Padding(5).Text(r.Type).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(r.Count.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text($"₹{r.Revenue:N0}").FontSize(9).Bold();
                        table.Cell().Background(bg).Padding(5).Text($"{share}%").FontSize(9);
                    }
                    // totals row
                    foreach (var val in new[] { "TOTAL", list.Count.ToString(), $"₹{total:N0}", "100%" })
                        table.Cell().Background("#e8f0fe").Padding(5).Text(val).FontSize(9).SemiBold();
                });

                // Per-policyholder detail
                col.Item().PaddingTop(20).Text("Per-Policyholder Detail").FontSize(12).SemiBold();
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);
                        c.ConstantColumn(110);
                        c.RelativeColumn(2);
                        c.RelativeColumn(1.5f);
                        c.ConstantColumn(90);
                        c.ConstantColumn(80);
                    });
                    table.Header(h =>
                    {
                        foreach (var lbl in new[] { "#", "Policy #", "Policyholder", "Insurance Type", "Premium", "Status" })
                            h.Cell().Background("#1a56db").Padding(6).Text(lbl).FontColor(Colors.White).SemiBold().FontSize(9);
                    });
                    var i = 1; var alt = false;
                    foreach (var p in list.OrderBy(p => p.CustomerName))
                    {
                        var bg = alt ? Colors.Grey.Lighten4 : Colors.White; alt = !alt;
                        table.Cell().Background(bg).Padding(5).Text(i++.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.PolicyNumber).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.CustomerName).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.InsuranceType).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text($"₹{p.PremiumAmount:N0}").FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(p.Status).FontSize(9);
                    }
                });
            });
            page.Footer().Element(BuildFooter());
        })).GeneratePdf();
    }

    // ── Audit Log Report ──────────────────────────────────────────────────────
    public static byte[] GenerateAuditReport(IEnumerable<AuditLogDto> logs, string title)
    {
        var list = logs.ToList();

        return Document.Create(doc => doc.Page(page =>
        {
            page.Size(PageSizes.A4.Landscape());
            page.Margin(30);
            page.DefaultTextStyle(x => x.FontSize(10));
            page.Header().Element(BuildHeader(title, "Audit Log Report"));
            page.Content().Column(col =>
            {
                col.Item().Row(row =>
                {
                    SummaryCard(row, "Total Events", list.Count.ToString(), "#1a56db");
                    SummaryCard(row, "Unique Users", list.Select(l => l.UserId).Distinct().Count().ToString(), "#7c3aed");
                    SummaryCard(row, "Entity Types", list.Select(l => l.EntityName).Distinct().Count().ToString(), "#f59e0b");
                });

                col.Item().PaddingTop(16).Text("Audit Events").FontSize(12).SemiBold();
                col.Item().PaddingTop(8).Table(table =>
                {
                    table.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(30);
                        c.RelativeColumn(1.5f); // User
                        c.ConstantColumn(110);  // Action
                        c.ConstantColumn(100);  // Entity
                        c.ConstantColumn(70);   // Entity ID
                        c.RelativeColumn(3);    // Details
                        c.ConstantColumn(110);  // Timestamp
                    });
                    table.Header(h =>
                    {
                        foreach (var lbl in new[] { "#", "User", "Action", "Entity", "Entity ID", "Details", "Timestamp" })
                            h.Cell().Background("#1a56db").Padding(6).Text(lbl).FontColor(Colors.White).SemiBold().FontSize(9);
                    });
                    var i = 1; var alt = false;
                    foreach (var l in list)
                    {
                        var bg = alt ? Colors.Grey.Lighten4 : Colors.White; alt = !alt;
                        table.Cell().Background(bg).Padding(5).Text(i++.ToString()).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(l.UserName).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(l.Action).FontSize(9).SemiBold();
                        table.Cell().Background(bg).Padding(5).Text(l.EntityName).FontSize(9);
                        table.Cell().Background(bg).Padding(5).Text(l.EntityId).FontSize(8).FontColor(Colors.Grey.Medium);
                        table.Cell().Background(bg).Padding(5).Text(l.Details).FontSize(8);
                        table.Cell().Background(bg).Padding(5).Text(l.Timestamp.ToString("MMM d, yyyy HH:mm")).FontSize(8);
                    }
                });
            });
            page.Footer().Element(BuildFooter());
        })).GeneratePdf();
    }

    // ── Shared helpers ────────────────────────────────────────────────────────
    private static Action<IContainer> BuildHeader(string title, string subtitle) => container =>
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("SmartSure Insurance").FontSize(18).Bold().FontColor("#1a56db");
                    c.Item().Text(title).FontSize(13).SemiBold();
                    c.Item().Text(subtitle).FontSize(10).FontColor(Colors.Grey.Medium);
                });
                row.ConstantItem(200).AlignRight()
                    .Text($"Generated: {DateTime.UtcNow:MMM d, yyyy HH:mm} UTC")
                    .FontSize(9).FontColor(Colors.Grey.Medium);
            });
            col.Item().PaddingTop(8).LineHorizontal(1).LineColor("#1a56db");
        });

    private static Action<IContainer> BuildFooter() => container =>
        container.Row(row =>
        {
            row.RelativeItem().Text("SmartSure — Confidential").FontSize(8).FontColor(Colors.Grey.Medium);
            row.ConstantItem(80).AlignRight().Text(x =>
            {
                x.Span("Page ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.CurrentPageNumber().FontSize(8).FontColor(Colors.Grey.Medium);
                x.Span(" of ").FontSize(8).FontColor(Colors.Grey.Medium);
                x.TotalPages().FontSize(8).FontColor(Colors.Grey.Medium);
            });
        });

    private static void SummaryCard(RowDescriptor row, string label, string value, string color) =>
        row.RelativeItem().Border(1).BorderColor(Colors.Grey.Lighten2).Padding(8).Column(c =>
        {
            c.Item().Text(label).FontSize(8).FontColor(Colors.Grey.Medium);
            c.Item().Text(value).FontSize(14).Bold().FontColor(color);
        });
}

