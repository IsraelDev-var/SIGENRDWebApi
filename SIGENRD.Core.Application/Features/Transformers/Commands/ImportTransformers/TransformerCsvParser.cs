using System.Globalization;
using System.Text;

namespace SIGENRD.Core.Application.Features.Transformers.Commands.ImportTransformers
{
    public record TransformerCsvRow(
        int Line, string Code, int ServiceZoneId,
        decimal TotalCapacityKva, decimal AvailableCapacityKva,
        double Latitude, double Longitude);

    public record TransformerCsvParseResult(List<TransformerCsvRow> Rows, List<string> Errors);

    /// <summary>
    /// Lee la plantilla CSV de transformadores que entrega la distribuidora (mitigación de RD-01).
    /// Columnas obligatorias: code, serviceZoneId, totalCapacityKva, availableCapacityKva, latitude, longitude.
    /// Separador coma, punto decimal, UTF-8. La carga es todo o nada: cualquier error rechaza el archivo.
    /// </summary>
    public static class TransformerCsvParser
    {
        public const int MaxRows = 10_000;
        public const int MaxErrors = 100;

        private static readonly string[] RequiredColumns =
            { "code", "serviceZoneId", "totalCapacityKva", "availableCapacityKva", "latitude", "longitude" };

        public static async Task<TransformerCsvParseResult> ParseAsync(Stream content, CancellationToken cancellationToken = default)
        {
            var rows = new List<TransformerCsvRow>();
            var errors = new List<string>();

            using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            var lineNumber = 0;
            Dictionary<string, int>? columns = null;
            var seenCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) is not null)
            {
                lineNumber++;
                if (string.IsNullOrWhiteSpace(line)) continue;

                var fields = SplitLine(line);

                if (columns is null)
                {
                    columns = IndexColumns(fields);
                    var missing = RequiredColumns.Where(c => !columns.ContainsKey(c.ToLowerInvariant())).ToList();
                    if (missing.Count > 0)
                    {
                        errors.Add("Faltan columnas obligatorias: " + string.Join(", ", missing) + ".");
                        return new TransformerCsvParseResult(rows, errors);
                    }
                    continue;
                }

                if (rows.Count + 1 > MaxRows)
                {
                    errors.Add("El archivo supera el máximo de " + MaxRows + " filas.");
                    break;
                }

                var row = ParseRow(lineNumber, fields, columns, errors, seenCodes);
                if (row is not null) rows.Add(row);

                if (errors.Count >= MaxErrors)
                {
                    errors.Add("Se detuvo la lectura tras " + MaxErrors + " errores.");
                    break;
                }
            }

            if (columns is null && errors.Count == 0)
                errors.Add("El archivo está vacío.");

            return new TransformerCsvParseResult(rows, errors);
        }

        private static TransformerCsvRow? ParseRow(
            int line, List<string> fields, Dictionary<string, int> columns,
            List<string> errors, HashSet<string> seenCodes)
        {
            string Field(string name) => columns[name.ToLowerInvariant()] < fields.Count ? fields[columns[name.ToLowerInvariant()]].Trim() : string.Empty;

            var before = errors.Count;
            string Prefix() => "Línea " + line + ": ";

            var code = Field("code");
            if (code.Length == 0) errors.Add(Prefix() + "el código es obligatorio.");
            else if (code.Length > 50) errors.Add(Prefix() + "el código excede los 50 caracteres.");
            else if (!seenCodes.Add(code)) errors.Add(Prefix() + "el código " + code + " está repetido en el archivo.");

            if (!int.TryParse(Field("serviceZoneId"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var zoneId) || zoneId <= 0)
                errors.Add(Prefix() + "serviceZoneId debe ser un entero mayor que cero.");

            if (!TryDecimal(Field("totalCapacityKva"), out var total) || total <= 0)
                errors.Add(Prefix() + "totalCapacityKva debe ser un número mayor que cero.");

            if (!TryDecimal(Field("availableCapacityKva"), out var available) || available < 0)
                errors.Add(Prefix() + "availableCapacityKva debe ser un número mayor o igual a cero.");
            else if (total > 0 && available > total)
                errors.Add(Prefix() + "availableCapacityKva no puede superar totalCapacityKva.");

            if (!double.TryParse(Field("latitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lat) || lat is < -90 or > 90)
                errors.Add(Prefix() + "latitude debe estar entre -90 y 90.");

            if (!double.TryParse(Field("longitude"), NumberStyles.Float, CultureInfo.InvariantCulture, out var lng) || lng is < -180 or > 180)
                errors.Add(Prefix() + "longitude debe estar entre -180 y 180.");

            if (errors.Count > before) return null;

            return new TransformerCsvRow(line, code, zoneId, total, available, lat, lng);
        }

        private static bool TryDecimal(string value, out decimal result) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out result);

        private static Dictionary<string, int> IndexColumns(List<string> header)
        {
            var map = new Dictionary<string, int>();
            for (var i = 0; i < header.Count; i++)
            {
                var name = header[i].Trim().TrimStart('﻿').ToLowerInvariant();
                if (name.Length > 0) map.TryAdd(name, i);
            }
            return map;
        }

        /// <summary>Separa una línea CSV respetando comillas dobles y comillas escapadas ("").</summary>
        public static List<string> SplitLine(string line)
        {
            var fields = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];

                if (inQuotes)
                {
                    if (c == '"' && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                    else if (c == '"') inQuotes = false;
                    else current.Append(c);
                }
                else if (c == '"') inQuotes = true;
                else if (c == ',') { fields.Add(current.ToString()); current.Clear(); }
                else current.Append(c);
            }

            fields.Add(current.ToString());
            return fields;
        }
    }
}
