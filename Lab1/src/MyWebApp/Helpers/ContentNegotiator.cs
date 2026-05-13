using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;

namespace MyWebApp.Helpers;

public static class ContentNegotiator
{
    public static bool PrefersHtml(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();
        if (string.IsNullOrWhiteSpace(accept)) return false;

        var parsed = MediaTypeHeaderValue.ParseList(request.Headers[HeaderNames.Accept]);
        double htmlQ = 0, jsonQ = 0;
        foreach (var m in parsed)
        {
            var type = m.MediaType.ToString();
            var q = m.Quality ?? 1.0;
            if (type is "text/html" or "text/*") htmlQ = Math.Max(htmlQ, q);
            if (type is "application/json" or "*/*") jsonQ = Math.Max(jsonQ, q);
        }
        return htmlQ >= jsonQ && htmlQ > 0;
    }

    public static IResult HtmlResult(string html) =>
        Results.Content(html, "text/html; charset=utf-8");

    public static string Page(string title, string body) => $"""
        <!DOCTYPE html>
        <html lang="uk">
        <head><meta charset="UTF-8"><title>{title}</title></head>
        <body>
        <h1>{title}</h1>
        {body}
        </body>
        </html>
        """;

    public static string Table(string[] headers, IEnumerable<string[]> rows)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">");
        sb.Append("<thead><tr>");
        foreach (var h in headers) sb.Append($"<th>{HE(h)}</th>");
        sb.AppendLine("</tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var row in rows)
        {
            sb.Append("<tr>");
            foreach (var cell in row) sb.Append($"<td>{HE(cell)}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string HE(string s) =>
        System.Net.WebUtility.HtmlEncode(s);
}