using System.Net;
using System.Text;

namespace SIGENRD.Core.Application.Services
{
    /// <summary>
    /// Plantilla HTML corporativa de los correos de EnergyMap: encabezado con la marca, cuerpo, botón de acción,
    /// firma del equipo y pie legal. Hecha con tablas y estilos en línea, que es lo que respetan Gmail y Outlook.
    /// Los textos que llegan como parámetro se codifican aquí; solo <paramref name="extraHtml"/> va sin codificar.
    /// </summary>
    public static class EmailTemplate
    {
        private const string Navy = "#12355b";
        private const string Teal = "#1b8a8f";
        private const string Ink = "#1f2933";
        private const string Muted = "#5f6b7a";
        private const string Page = "#eef2f6";
        private const string Line = "#dde3ea";

        /// <param name="title">Título visible del mensaje.</param>
        /// <param name="greetingName">Nombre de la persona; sin él se saluda de forma general.</param>
        /// <param name="paragraphs">Párrafos de texto plano (se codifican).</param>
        /// <param name="actionText">Texto del botón; sin él no hay botón.</param>
        /// <param name="actionUrl">Enlace del botón.</param>
        /// <param name="note">Aviso final en letra pequeña (por ejemplo, "si no fuiste tú, ignora este mensaje").</param>
        /// <param name="extraHtml">HTML de confianza que se agrega después de los párrafos (por ejemplo, una tabla de datos).</param>
        public static string Render(
            string title,
            string? greetingName,
            IEnumerable<string> paragraphs,
            string? actionText = null,
            string? actionUrl = null,
            string? note = null,
            string? extraHtml = null)
        {
            static string E(string s) => WebUtility.HtmlEncode(s);

            var body = new StringBuilder();
            body.Append("<p style=\"margin:0 0 16px;font-size:16px;line-height:24px;color:").Append(Ink).Append(";\">")
                .Append(string.IsNullOrWhiteSpace(greetingName) ? "Hola:" : "Hola, " + E(greetingName.Trim()) + ":")
                .Append("</p>");

            foreach (var p in paragraphs)
                body.Append("<p style=\"margin:0 0 16px;font-size:15px;line-height:24px;color:").Append(Ink).Append(";\">")
                    .Append(E(p)).Append("</p>");

            if (!string.IsNullOrWhiteSpace(extraHtml))
                body.Append(extraHtml);

            if (!string.IsNullOrWhiteSpace(actionText) && !string.IsNullOrWhiteSpace(actionUrl))
            {
                var url = E(actionUrl);
                body.Append("<table role=\"presentation\" cellspacing=\"0\" cellpadding=\"0\" border=\"0\" style=\"margin:8px 0 24px;\"><tr>")
                    .Append("<td style=\"border-radius:8px;background:").Append(Navy).Append(";\">")
                    .Append("<a href=\"").Append(url).Append("\" target=\"_blank\" style=\"display:inline-block;padding:13px 28px;")
                    .Append("font-family:Segoe UI,Arial,sans-serif;font-size:15px;font-weight:600;color:#ffffff;text-decoration:none;border-radius:8px;\">")
                    .Append(E(actionText)).Append("</a></td></tr></table>")
                    .Append("<p style=\"margin:0 0 16px;font-size:12px;line-height:18px;color:").Append(Muted).Append(";\">")
                    .Append("Si el botón no funciona, copia y pega este enlace en tu navegador:<br>")
                    .Append("<a href=\"").Append(url).Append("\" style=\"color:").Append(Teal).Append(";word-break:break-all;\">").Append(url).Append("</a></p>");
            }

            if (!string.IsNullOrWhiteSpace(note))
                body.Append("<p style=\"margin:0 0 8px;padding:12px 14px;background:#f6f8fa;border-left:3px solid ").Append(Teal)
                    .Append(";font-size:13px;line-height:20px;color:").Append(Muted).Append(";\">").Append(E(note)).Append("</p>");

            var year = DateTime.UtcNow.Year;

            return $"""
<!DOCTYPE html>
<html lang="es">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<meta name="color-scheme" content="light">
<title>{E(title)}</title>
</head>
<body style="margin:0;padding:0;background:{Page};-webkit-text-size-adjust:100%;">
<div style="display:none;max-height:0;overflow:hidden;opacity:0;">{E(title)} · EnergyMap</div>
<table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="background:{Page};">
<tr><td align="center" style="padding:32px 12px;">
  <table role="presentation" width="600" cellspacing="0" cellpadding="0" border="0" style="width:100%;max-width:600px;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid {Line};font-family:Segoe UI,Arial,sans-serif;">
    <tr><td style="background:{Navy};padding:22px 32px;">
      <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
        <td style="width:36px;height:36px;background:{Teal};border-radius:8px;text-align:center;vertical-align:middle;font-size:20px;line-height:36px;color:#ffffff;">&#9889;</td>
        <td style="padding-left:12px;">
          <div style="font-size:20px;font-weight:700;color:#ffffff;letter-spacing:.3px;">EnergyMap</div>
          <div style="font-size:12px;color:#c9d6e6;">Disponibilidad de la red y solicitudes de conexión</div>
        </td>
      </tr></table>
    </td></tr>
    <tr><td style="height:4px;background:{Teal};line-height:4px;font-size:0;">&nbsp;</td></tr>
    <tr><td style="padding:32px 32px 8px;">
      <h1 style="margin:0 0 20px;font-size:22px;line-height:30px;font-weight:700;color:{Navy};">{E(title)}</h1>
      {body}
    </td></tr>
    <tr><td style="padding:8px 32px 28px;">
      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="border-top:1px solid {Line};">
        <tr><td style="padding-top:20px;">
          <table role="presentation" cellspacing="0" cellpadding="0" border="0"><tr>
            <td style="border-left:3px solid {Teal};padding-left:14px;">
              <div style="font-size:14px;color:{Muted};">Atentamente,</div>
              <div style="font-size:16px;font-weight:700;color:{Navy};margin-top:2px;">Equipo de EnergyMap</div>
              <div style="font-size:13px;color:{Muted};margin-top:2px;">Plataforma de interconexión y medición neta</div>
              <div style="font-size:13px;color:{Teal};margin-top:6px;font-weight:600;">Energía clara para cada conexión</div>
            </td>
          </tr></table>
        </td></tr>
      </table>
    </td></tr>
    <tr><td style="background:#f6f8fa;border-top:1px solid {Line};padding:18px 32px;font-size:11px;line-height:17px;color:{Muted};">
      Este es un mensaje automático de EnergyMap; por favor no respondas a este correo.<br>
      Recibes este correo porque tienes una cuenta o un trámite en la plataforma.<br>
      &copy; {year} EnergyMap · República Dominicana
    </td></tr>
  </table>
</td></tr>
</table>
</body>
</html>
""";
        }
    }
}
