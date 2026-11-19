using System.Net;
using System.Text.RegularExpressions;

namespace SIGENRD.UnitTests.Integration;

/// <summary>Saca el enlace de acción (el primer href) del HTML de un correo, ya decodificado (&amp;amp; → &amp;).</summary>
public static class EmailLinks
{
    private static readonly Regex Href = new("href=[\"']([^\"']+)[\"']", RegexOptions.Compiled);

    public static string First(string body) => WebUtility.HtmlDecode(Href.Match(body).Groups[1].Value);
}
