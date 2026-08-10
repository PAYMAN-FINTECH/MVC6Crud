using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

public class AgreementPdf : IDocument
{
    public byte[] WatermarkBytes { get; set; }
    public AgreementData Data { get; set; }

    public AgreementPdf(byte[] watermark, AgreementData data)
    {
        WatermarkBytes = watermark;
        Data = data;
    }

    public DocumentMetadata GetMetadata() => DocumentMetadata.Default;
    public DocumentSettings GetSettings() => DocumentSettings.Default;

    public void Compose(IDocumentContainer container)
    {
        var wmSmallImg = ApplyWatermarkOpacity(WatermarkBytes, 0.15f);

        container.Page(page =>
        {
            page.Margin(40);

            // ⭐ Small, centered watermark
            page.Background().AlignCenter().AlignMiddle().Width(200).Height(200)
                .Image(wmSmallImg, ImageScaling.FitArea);

            // ⭐ Company Details (top)
            page.Header().Element(WriteCompanyDetails);

            page.Content().PaddingVertical(10).Column(col =>
            {
                col.Item().PaddingBottom(15).AlignCenter().Text("E-Signature Agreement")
                    .Bold().FontSize(22);

                col.Item().PaddingTop(10).Element(WriteKycTable);
                col.Item().PaddingTop(20).Element(WriteAgreementSummary);
            });

            page.Footer().AlignCenter().Text("© PAYMAN FINTECH SOLUTIONS PVT LTD");
        });
    }

    private void WriteCompanyDetails(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("PAYMAN FINTECH SOLUTIONS PVT LTD").Bold().FontSize(14);
            col.Item().Text("CIN: U74999TS2021PTC123456");
            col.Item().Text("Email: support@paymanfintech.in");
            col.Item().Text("Phone: +91-9876543210");
            col.Item().PaddingBottom(10).LineHorizontal(1);
        });
    }

    private void WriteKycTable(IContainer container)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(150);
                cols.RelativeColumn();
            });

            table.Header(h =>
            {
                h.Cell().Text("Field").Bold();
                h.Cell().Text("Value").Bold();
            });

            AddRow(table, "Full Name", Data.FullName);
            AddRow(table, "PAN Number", Data.Pan);
            AddRow(table, "Aadhaar Number", Data.Aadhaar);
            AddRow(table, "Registered Address", Data.Address);
        });
    }

    private void AddRow(TableDescriptor table, string field, string value)
    {
        table.Cell().Text(field);
        table.Cell().Text(value);
    }

    private void WriteAgreementSummary(IContainer container)
    {
        container.Column(col =>
        {
            col.Item().Text("Agreement Summary").Bold().FontSize(18);

            col.Item().Text("This Agreement outlines your responsibilities when using services provided by PAYMAN FINTECH SOLUTIONS PVT LTD.");

            col.Item().PaddingTop(10).Text("1. Wallet & Settlement Usage").Bold();
            col.Item().Text("• Services are strictly for genuine business use only.");
            col.Item().Text("• Any settlement to bank is your responsibility.");

            col.Item().PaddingTop(10).Text("2. Fraud & Suspicious Activity").Bold();
            col.Item().Text("• PAYMAN may block or deduct funds upon detecting suspicious activity.");
            col.Item().Text("• Fraud recovery is your liability when balance is insufficient.");

            col.Item().PaddingTop(10).Text("3. Compliance Responsibility").Bold();
            col.Item().Text("• You must follow regulations defined by authorities.");
        });
    }

    private byte[] ApplyWatermarkOpacity(byte[] imageBytes, float opacity)
    {
        using var input = SKBitmap.Decode(imageBytes);
        using var surface = SKSurface.Create(new SKImageInfo(input.Width, input.Height));
        var canvas = surface.Canvas;

        canvas.Clear(SKColors.Transparent);

        var paint = new SKPaint
        {
            Color = SKColors.White.WithAlpha((byte)(255 * opacity)),
            IsAntialias = true
        };

        canvas.DrawBitmap(input, 0, 0, paint);

        using var result = surface.Snapshot();
        using var data = result.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    // ⭐ PDF GENERATOR (required for QuestPDF 2024)
    public byte[] GeneratePdf()
    {
        return Document.Create(container =>
        {
            Compose(container);
        }).GeneratePdf();
    }
}

public class AgreementData
{
    public string FullName { get; set; }
    public int Age { get; set; }
    public string Pan { get; set; }
    public string Aadhaar { get; set; }
    public string Address { get; set; }
}
