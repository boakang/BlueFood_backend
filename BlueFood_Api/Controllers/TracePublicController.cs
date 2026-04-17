using System.Net;
using System.Text;
using BlueFood.Api.Models;
using BlueFood.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace BlueFood.Api.Controllers;

[ApiController]
[Route("trace/public")]
public class TracePublicController : ControllerBase
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);

    private readonly IBatchService _batchService;

    public TracePublicController(IBatchService batchService)
    {
        _batchService = batchService;
    }

    [HttpGet("/t/{qrToken}")]
    public IActionResult ShortLink(string qrToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return BadRequest("qrToken is required.");
        }

        return Redirect($"/trace/public/{WebUtility.UrlEncode(qrToken)}");
    }

    [HttpGet("{qrToken}")]
    public async Task<IActionResult> Get(string qrToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(qrToken))
        {
            return Content(RenderErrorPage("Mã QR không hợp lệ. Vui lòng quét lại.", 400), "text/html; charset=utf-8", Encoding.UTF8);
        }

        try
        {
            var traceRows = await _batchService.GetTraceByQrTokenAsync(qrToken, cancellationToken);
            if (traceRows.Count == 0)
            {
                return Content(RenderErrorPage($"Không tìm thấy thông tin lô hàng cho mã: {WebUtility.HtmlEncode(qrToken)}", 404), "text/html; charset=utf-8", Encoding.UTF8);
            }

            var certificates = await _batchService.GetCertificatesByBatchCodeAsync(traceRows[0].BatchCode, cancellationToken);
            var html = RenderPage(traceRows[0], traceRows, certificates);
            return Content(html, "text/html; charset=utf-8", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            var errorHtml = RenderErrorPage($"Lỗi hệ thống: {ex.Message}. Vui lòng liên hệ hỗ trợ.", 500);
            return Content(errorHtml, "text/html; charset=utf-8", Encoding.UTF8);
        }
    }

    private static string RenderPage(TraceEventDto firstRow, IReadOnlyList<TraceEventDto> traceRows, IReadOnlyList<CertificateDto> certificates)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang='vi'>");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset='UTF-8' />");
        builder.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0' />");
        builder.AppendLine("<title>BlueFood Trace</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{margin:0;font-family:Arial,sans-serif;background:#08111f;color:#e5eef8;padding:16px;}");
        builder.AppendLine(".card{max-width:720px;margin:0 auto;background:#0b1526;border:1px solid #24344d;border-radius:18px;padding:16px;}");
        builder.AppendLine("h1{margin:0 0 8px;font-size:24px;color:#f7fbff;}");
        builder.AppendLine(".meta,.timeline{display:grid;gap:10px;}");
        builder.AppendLine(".meta{margin:14px 0;padding:14px;background:#101c31;border-radius:14px;}");
        builder.AppendLine(".label{display:block;color:#8ea7c1;font-size:12px;}");
        builder.AppendLine(".value{display:block;font-weight:700;color:#fff;word-break:break-word;margin-top:2px;}");
        builder.AppendLine(".item{padding:12px 14px;border-radius:14px;background:#101c31;border:1px solid #21334c;}");
        builder.AppendLine(".pill{display:inline-block;padding:4px 10px;border-radius:999px;background:#18324f;color:#7dd3fc;font-size:12px;font-weight:700;}");
        builder.AppendLine(".small{display:block;margin-top:6px;color:#b7c8d8;font-size:14px;line-height:1.5;}");
        builder.AppendLine(".certs{margin-top:14px;display:grid;gap:10px;}");
        builder.AppendLine(".cert{padding:12px 14px;border-radius:14px;background:#101c31;border:1px solid #21334c;}");
        builder.AppendLine(".cert-title{display:flex;justify-content:space-between;align-items:center;gap:10px;color:#fff;font-weight:700;}");
        builder.AppendLine(".cert-meta{display:block;margin-top:6px;color:#b7c8d8;font-size:14px;line-height:1.5;word-break:break-word;}");
        builder.AppendLine(".footer{margin-top:14px;color:#8ea7c1;font-size:12px;}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class='card'>");
        builder.AppendLine("<h1>BlueFood Traceability</h1>");
        builder.AppendLine("<div>Scan QR to view the public batch trace.</div>");
        builder.AppendLine("<div class='meta'>");
        builder.AppendLine($"<div><span class='label'>Batch code</span><span class='value'>{WebUtility.HtmlEncode(firstRow.BatchCode)}</span></div>");
        builder.AppendLine($"<div><span class='label'>Product</span><span class='value'>{WebUtility.HtmlEncode(firstRow.ProductName)}</span></div>");
        builder.AppendLine($"<div><span class='label'>Status</span><span class='value'>{WebUtility.HtmlEncode(firstRow.CurrentStatus)}</span></div>");
        builder.AppendLine($"<div><span class='label'>QR token</span><span class='value'>{WebUtility.HtmlEncode(firstRow.QRToken)}</span></div>");
        builder.AppendLine($"<div><span class='label'>Trace URL</span><span class='value'>{WebUtility.HtmlEncode(firstRow.TraceUrl)}</span></div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class='timeline'>");

        foreach (var row in traceRows)
        {
            var eventTimeText = ToVietnamTime(row.EventTime).ToString("dd/MM/yyyy HH:mm:ss");
            var fromPartnerText = row.FromPartnerName ?? "-";
            var toPartnerText = row.ToPartnerName ?? "-";

            builder.AppendLine("<div class='item'>");
            builder.AppendLine($"<span class='pill'>{WebUtility.HtmlEncode(row.EventType)}</span> <strong>#{row.EventNo}</strong>");
            builder.AppendLine($"<span class='small'>{WebUtility.HtmlEncode(row.ProductName)} - {WebUtility.HtmlEncode(eventTimeText)}</span>");
            builder.AppendLine($"<span class='small'>{WebUtility.HtmlEncode(fromPartnerText)} -> {WebUtility.HtmlEncode(toPartnerText)}</span>");

            if (!string.IsNullOrWhiteSpace(row.LocationText))
            {
                builder.AppendLine($"<span class='small'>Location: {WebUtility.HtmlEncode(row.LocationText)}</span>");
            }

            if (!string.IsNullOrWhiteSpace(row.NoteText))
            {
                builder.AppendLine($"<span class='small'>Note: {WebUtility.HtmlEncode(row.NoteText)}</span>");
            }

            builder.AppendLine("</div>");
        }

        builder.AppendLine("</div>");

        builder.AppendLine("<div class='certs'>");
        builder.AppendLine("<h2 style='margin:0;color:#f7fbff;font-size:18px;'>Chứng chỉ đính kèm</h2>");

        if (certificates.Count == 0)
        {
            builder.AppendLine("<div class='item'><span class='small'>Chưa có chứng chỉ nào được gắn vào lô hàng này.</span></div>");
        }
        else
        {
            foreach (var certificate in certificates)
            {
                var attachedAtText = ToVietnamTime(certificate.AttachedAt).ToString("dd/MM/yyyy HH:mm:ss");
                var issuedDateText = certificate.IssuedDate?.ToString("dd/MM/yyyy") ?? "-";
                var expiredDateText = certificate.ExpiredDate?.ToString("dd/MM/yyyy") ?? "-";
                var issuedByText = string.IsNullOrWhiteSpace(certificate.IssuedBy) ? "-" : certificate.IssuedBy;
                var fileUrlText = string.IsNullOrWhiteSpace(certificate.FileUrl) ? null : certificate.FileUrl;

                builder.AppendLine("<div class='cert'>");
                builder.AppendLine($"<div class='cert-title'><span>{WebUtility.HtmlEncode(certificate.CertificateName)}</span><span>{WebUtility.HtmlEncode(certificate.CertificateCode)}</span></div>");
                builder.AppendLine($"<span class='cert-meta'>Cấp bởi: {WebUtility.HtmlEncode(issuedByText)}</span>");
                builder.AppendLine($"<span class='cert-meta'>Hiệu lực: {WebUtility.HtmlEncode(issuedDateText)} - {WebUtility.HtmlEncode(expiredDateText)}</span>");
                builder.AppendLine($"<span class='cert-meta'>Gắn lúc: {WebUtility.HtmlEncode(attachedAtText)} bởi {WebUtility.HtmlEncode(certificate.AttachedBy)}</span>");

                if (fileUrlText is not null)
                {
                    builder.AppendLine($"<span class='cert-meta'>File: <a style='color:#7dd3fc' href='{WebUtility.HtmlEncode(fileUrlText)}' target='_blank' rel='noopener noreferrer'>{WebUtility.HtmlEncode(fileUrlText)}</a></span>");
                }

                builder.AppendLine("</div>");
            }
        }

        builder.AppendLine("</div>");
        builder.AppendLine($"<div class='footer'>Public page generated from SQL Server data at {ToVietnamTime(DateTime.UtcNow):dd/MM/yyyy HH:mm:ss} (UTC+07:00).</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string RenderErrorPage(string message, int statusCode)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html>");
        builder.AppendLine("<html lang='vi'>");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset='UTF-8' />");
        builder.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0' />");
        builder.AppendLine("<title>BlueFood - Lỗi</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("body{margin:0;font-family:Arial,sans-serif;background:#08111f;color:#e5eef8;padding:16px;display:flex;align-items:center;justify-content:center;min-height:100vh;}");
        builder.AppendLine(".card{max-width:600px;background:#0b1526;border:1px solid #d97706;border-radius:18px;padding:32px;text-align:center;}");
        builder.AppendLine("h1{margin:0 0 16px;font-size:28px;color:#f87171;}");
        builder.AppendLine("p{margin:0 0 24px;font-size:16px;line-height:1.6;}");
        builder.AppendLine("a{display:inline-block;padding:12px 24px;background:#06b6d4;color:#fff;border-radius:8px;text-decoration:none;font-weight:700;margin-top:16px;}");
        builder.AppendLine(".code{font-size:12px;color:#8ea7c1;margin-top:24px;padding-top:24px;border-top:1px solid #24344d;}");
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class='card'>");
        builder.AppendLine("<h1>⚠️ Không thể tải thông tin</h1>");
        builder.AppendLine($"<p>{WebUtility.HtmlEncode(message)}</p>");
        builder.AppendLine("<a href='javascript:history.back()'>← Quay lại</a>");
        builder.AppendLine($"<div class='code'>Mã lỗi: {statusCode} | Thời gian: {ToVietnamTime(DateTime.UtcNow):dd/MM/yyyy HH:mm:ss}</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static DateTimeOffset ToVietnamTime(DateTime value)
    {
        if (value.Kind == DateTimeKind.Utc)
        {
            return new DateTimeOffset(value, TimeSpan.Zero).ToOffset(VietnamOffset);
        }

        if (value.Kind == DateTimeKind.Local)
        {
            return new DateTimeOffset(value).ToOffset(VietnamOffset);
        }

        return new DateTimeOffset(value, VietnamOffset);
    }
}
