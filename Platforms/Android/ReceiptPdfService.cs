using System.Globalization;
using Android.Graphics;
using Android.Graphics.Pdf;
using KantinKita.Helpers;
using KantinKita.Models;
using KantinKita.Services;
using Microsoft.Maui.Storage;
using AColor = Android.Graphics.Color;
using APath = System.IO.Path;
using Canvas = Android.Graphics.Canvas;
using Paint = Android.Graphics.Paint;
using Typeface = Android.Graphics.Typeface;

namespace KantinKita.Platforms;

/// <summary>Offline, searchable PDF output using Android's native PDF canvas.</summary>
public sealed class ReceiptPdfService : IReceiptPdfService
{
    public Task<string> CreateAsync(Order order, OrderStatus status, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(order);
        cancellationToken.ThrowIfCancellationRequested();

        // Copy mutable cart lines before leaving the UI thread. A later cart edit
        // cannot change a receipt while its native document is being drawn.
        var snapshot = new Order
        {
            Code = order.Code,
            PlacedAt = order.PlacedAt,
            Items = order.Items.Select(item => new CartItem
            {
                ItemId = item.ItemId,
                Name = item.Name,
                Stall = item.Stall,
                CanteenId = item.CanteenId,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Options = item.Options.ToArray(),
                Note = item.Note,
            }).ToArray(),
            PaymentMethod = order.PaymentMethod,
            Subtotal = order.Subtotal,
            ServiceFee = order.ServiceFee,
            Discount = order.Discount,
            VoucherCode = order.VoucherCode,
            Note = order.Note,
            PickupIn = order.PickupIn,
            Status = order.Status,
            IsLive = order.IsLive,
            Destination = order.Destination,
        };
        var safeCode = new string(order.Code.Where(character =>
            char.IsAsciiLetterOrDigit(character) || character is '-' or '_').Take(64).ToArray());
        if (safeCode.Length == 0) safeCode = "pesanan";
        var directory = APath.Combine(FileSystem.CacheDirectory, "receipts");
        var path = APath.Combine(directory, $"{safeCode}-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.pdf");

        return Task.Run(() =>
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(directory);
                using var document = new PdfDocument();
                using var renderer = new ReceiptRenderer(document, snapshot.Code, cancellationToken);
                renderer.Render(snapshot, status);
                cancellationToken.ThrowIfCancellationRequested();
                using (var stream = File.Create(path)) document.WriteTo(stream);
                cancellationToken.ThrowIfCancellationRequested();
                return path;
            }
            catch
            {
                // Only this invocation's unique, incomplete cache file is removed.
                if (File.Exists(path)) File.Delete(path);
                throw;
            }
        }, cancellationToken);
    }

    private sealed class ReceiptRenderer : IDisposable
    {
        // Narrow receipt pages remain readable at actual size and paginate instead
        // of cutting off long orders, option lists, or customer notes.
        private const int PageWidth = 320;
        private const int PageHeight = 640;
        private const float Margin = 22;
        private const float Right = PageWidth - Margin;
        private const float ContentWidth = PageWidth - Margin * 2;
        private const float ContentBottom = PageHeight - 46;

        private static readonly CultureInfo Indonesian = CultureInfo.GetCultureInfo("id-ID");
        private readonly PdfDocument _document;
        private readonly string _code;
        private readonly CancellationToken _cancellationToken;
        private readonly Typeface _regularTypeface = Typeface.Create("sans-serif", TypefaceStyle.Normal)!;
        private readonly Typeface _boldTypeface = Typeface.Create("sans-serif", TypefaceStyle.Bold)!;
        private readonly Paint _body = new(PaintFlags.AntiAlias) { TextSize = 10.5f, Color = AColor.Rgb(32, 40, 54) };
        private readonly Paint _small = new(PaintFlags.AntiAlias) { TextSize = 9, Color = AColor.Rgb(93, 103, 118) };
        private readonly Paint _bold = new(PaintFlags.AntiAlias) { TextSize = 11, Color = AColor.Rgb(21, 32, 54) };
        private readonly Paint _title = new(PaintFlags.AntiAlias) { TextSize = 18, Color = AColor.Rgb(21, 32, 54) };
        private readonly Paint _line = new(PaintFlags.AntiAlias) { Color = AColor.Rgb(214, 220, 230), StrokeWidth = 0.7f };
        private readonly Paint _surface = new(PaintFlags.AntiAlias) { Color = AColor.Rgb(238, 243, 252) };
        private PdfDocument.Page? _page;
        private float _y;
        private int _pageNumber;

        public ReceiptRenderer(PdfDocument document, string code, CancellationToken cancellationToken)
        {
            _document = document;
            _code = code;
            _cancellationToken = cancellationToken;
            _body.SetTypeface(_regularTypeface);
            _small.SetTypeface(_regularTypeface);
            _bold.SetTypeface(_boldTypeface);
            _title.SetTypeface(_boldTypeface);
        }

        public void Render(Order order, OrderStatus status)
        {
            StartPage();
            try
            {
                Center("KANTIN KITA", _title);
                Center(FulfillmentDisplay.Canteen(order), _small);
                Gap(7);
                Center("STRUK PEMBELIAN", _bold);
                Center(order.Code, _bold);
                Gap(9);
                Detail("Tanggal", order.PlacedAt.ToLocalTime().ToString("dd MMM yyyy, HH.mm", Indonesian));
                Detail("Status pesanan", StatusText(order, status));
                Detail("Jadwal siap", order.PickupAt.ToLocalTime().ToString("dd MMM yyyy, HH.mm", Indonesian));
                Detail("Penerimaan", FulfillmentDisplay.Mode(order));
                Detail("Tujuan", FulfillmentDisplay.Location(order));
                Rule(11);
                AmountRow("RINCIAN PESANAN", "JUMLAH", _small);
                Gap(7);

                foreach (var item in order.Items)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    // Keep the first title and price line together where possible.
                    EnsureSpace(57);
                    Paragraph(item.Name, _bold);
                    if (!string.IsNullOrWhiteSpace(item.Stall)) Paragraph(item.Stall, _small);
                    AmountRow($"{item.Quantity} × {Money.Rupiah(item.UnitPrice)}", Money.Rupiah(item.LineTotal), _body, _bold);
                    if (item.Options.Count > 0)
                    {
                        var options = string.Join(", ", item.Options.Select(option =>
                            option.ExtraPrice > 0 ? $"{option.Name} (+{Money.Rupiah(option.ExtraPrice)})" : option.Name));
                        Paragraph("Pilihan: " + options, _small);
                    }
                    if (!string.IsNullOrWhiteSpace(item.Note)) Paragraph("Catatan item: " + item.Note, _small);
                    Rule(8);
                }

                EnsureSpace(105);
                Gap(3);
                AmountRow("Subtotal", Money.Rupiah(order.Subtotal), _body);
                AmountRow("Biaya layanan", Money.Rupiah(order.ServiceFee), _body);
                if (order.Discount > 0)
                {
                    AmountRow("Diskon", "−" + Money.Rupiah(order.Discount), _body);
                    if (!string.IsNullOrWhiteSpace(order.VoucherCode)) Detail("Voucher", order.VoucherCode);
                }
                Gap(8);
                Total(order.Total);
                Gap(12);
                Detail("Pembayaran", order.PaymentMethod);
                Detail("Jumlah barang", $"{order.ItemCount} item");
                if (!string.IsNullOrWhiteSpace(order.Note))
                {
                    Gap(8);
                    Paragraph("CATATAN PESANAN", _small);
                    Paragraph(order.Note, _body);
                }
                Rule(12);
                Paragraph("Terima kasih telah memesan di Kantin Kita.", _small);
                Paragraph("Simpan struk ini untuk mencocokkan pesanan saat diterima.", _small);
            }
            finally
            {
                // PdfDocument must never be closed with an unfinished page,
                // including when cancellation interrupts a long receipt.
                FinishPage();
            }
        }

        private static string StatusText(Order order, OrderStatus status) => status switch
        {
            OrderStatus.Confirmed => "Dikonfirmasi",
            OrderStatus.Preparing => "Disiapkan",
            OrderStatus.Ready => FulfillmentDisplay.ReadyStatus(order),
            OrderStatus.Completed => "Selesai",
            OrderStatus.Cancelled => "Dibatalkan",
            _ => status.ToString(),
        };

        private Canvas Canvas => _page?.Canvas ?? throw new InvalidOperationException("No active receipt page.");

        private void StartPage()
        {
            _cancellationToken.ThrowIfCancellationRequested();
            using var builder = new PdfDocument.PageInfo.Builder(PageWidth, PageHeight, ++_pageNumber);
            using var info = builder.Create();
            _page = _document.StartPage(info);
            _y = Margin;
            if (_pageNumber > 1)
            {
                Paragraph("KANTIN KITA · LANJUTAN", _bold);
                Paragraph(_code.Length <= 64 ? _code : _code[..64] + "…", _small);
                Rule(10);
            }
        }

        private void FinishPage()
        {
            if (_page is null) return;
            var footer = $"Halaman {_pageNumber}";
            _page.Canvas!.DrawLine(Margin, PageHeight - 34, Right, PageHeight - 34, _line);
            _page.Canvas.DrawText(footer, Right - _small.MeasureText(footer), PageHeight - 20, _small);
            var footerCode = Wrap(_code, _small, ContentWidth - 70).FirstOrDefault() ?? string.Empty;
            _page.Canvas.DrawText(footerCode, Margin, PageHeight - 20, _small);
            _document.FinishPage(_page);
            _page.Dispose();
            _page = null;
        }

        private void EnsureSpace(float height)
        {
            _cancellationToken.ThrowIfCancellationRequested();
            if (_y + height <= ContentBottom) return;
            FinishPage();
            StartPage();
        }

        private void Gap(float amount)
        {
            EnsureSpace(amount);
            _y += amount;
        }

        private void Center(string text, Paint paint)
        {
            foreach (var line in Wrap(text, paint, ContentWidth))
            {
                var height = paint.TextSize * 1.42f;
                EnsureSpace(height);
                Canvas.DrawText(line, Margin + (ContentWidth - paint.MeasureText(line)) / 2, _y + paint.TextSize, paint);
                _y += height;
            }
        }

        private void Paragraph(string? text, Paint paint)
        {
            foreach (var line in Wrap(text, paint, ContentWidth))
            {
                var height = paint.TextSize * 1.42f;
                EnsureSpace(height);
                Canvas.DrawText(line, Margin, _y + paint.TextSize, paint);
                _y += height;
            }
        }

        private void Detail(string label, string? value)
        {
            const float valueOffset = 98;
            var values = Wrap(value, _body, ContentWidth - valueOffset).ToArray();
            EnsureSpace(_body.TextSize * 1.42f);
            Canvas.DrawText(label, Margin, _y + _body.TextSize, _small);
            foreach (var line in values)
            {
                var height = _body.TextSize * 1.42f;
                EnsureSpace(height);
                Canvas.DrawText(line, Margin + valueOffset, _y + _body.TextSize, _body);
                _y += height;
            }
        }

        private void AmountRow(string label, string amount, Paint labelPaint, Paint? amountPaint = null)
        {
            amountPaint ??= labelPaint;
            var height = Math.Max(labelPaint.TextSize, amountPaint.TextSize) * 1.5f;
            var amountWidth = amountPaint.MeasureText(amount);
            var labelWidth = labelPaint.MeasureText(label);
            if (labelWidth + amountWidth + 15 > ContentWidth)
            {
                Paragraph(label, labelPaint);
                foreach (var line in Wrap(amount, amountPaint, ContentWidth))
                {
                    EnsureSpace(height);
                    Canvas.DrawText(line, Right - amountPaint.MeasureText(line), _y + amountPaint.TextSize, amountPaint);
                    _y += height;
                }
                return;
            }
            EnsureSpace(height);
            Canvas.DrawText(label, Margin, _y + labelPaint.TextSize, labelPaint);
            Canvas.DrawText(amount, Right - amountWidth, _y + amountPaint.TextSize, amountPaint);
            _y += height;
        }

        private void Total(decimal total)
        {
            var text = Money.Rupiah(total);
            if (_title.MeasureText(text) + _bold.MeasureText("TOTAL") + 22 > ContentWidth)
            {
                Paragraph("TOTAL", _bold);
                foreach (var line in Wrap(text, _title, ContentWidth))
                {
                    EnsureSpace(_title.TextSize * 1.5f);
                    Canvas.DrawText(line, Right - _title.MeasureText(line), _y + _title.TextSize, _title);
                    _y += _title.TextSize * 1.5f;
                }
                return;
            }
            EnsureSpace(42);
            Canvas.DrawRoundRect(Margin - 8, _y - 4, Right + 8, _y + 34, 5, 5, _surface);
            Canvas.DrawText("TOTAL", Margin, _y + 19, _bold);
            Canvas.DrawText(text, Right - _title.MeasureText(text), _y + 21, _title);
            _y += 38;
        }

        private void Rule(float gap)
        {
            EnsureSpace(gap + 3);
            _y += gap / 2;
            Canvas.DrawLine(Margin, _y, Right, _y, _line);
            _y += gap / 2 + 3;
        }

        private static IEnumerable<string> Wrap(string? value, Paint paint, float width)
        {
            var text = (value ?? string.Empty).Replace("\r\n", "\n").Replace('\r', '\n').Replace('\t', ' ');
            foreach (var paragraph in text.Split('\n'))
            {
                var remaining = paragraph.Trim();
                if (remaining.Length == 0)
                {
                    yield return string.Empty;
                    continue;
                }
                while (remaining.Length > 0)
                {
                    var count = paint.BreakText(remaining, true, width, null);
                    if (count < remaining.Length && count > 0 && char.IsHighSurrogate(remaining[count - 1])) count--;
                    if (count <= 0) count = StringInfo.GetNextTextElement(remaining).Length;
                    if (count < remaining.Length)
                    {
                        var whitespace = remaining.LastIndexOf(' ', count - 1, count);
                        if (whitespace > 0) count = whitespace;
                    }
                    yield return remaining[..count].TrimEnd();
                    remaining = remaining[count..].TrimStart();
                }
            }
        }

        public void Dispose()
        {
            FinishPage();
            _body.Dispose();
            _small.Dispose();
            _bold.Dispose();
            _title.Dispose();
            _line.Dispose();
            _surface.Dispose();
            _regularTypeface.Dispose();
            _boldTypeface.Dispose();
        }
    }
}
