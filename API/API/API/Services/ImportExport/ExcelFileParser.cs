using ClosedXML.Excel;

namespace API.Services.ImportExport;

public class ExcelFileParser : IFileParser
{
    public string Format => "xlsx";

    public Task<FileParseResult> ParseAsync(Stream stream, FileParseOptions options)
    {
        var result = new FileParseResult();
        using var workbook = new XLWorkbook(stream);

        IXLWorksheet worksheet;
        if (!string.IsNullOrEmpty(options.SheetName))
            worksheet = workbook.Worksheet(options.SheetName);
        else
            worksheet = workbook.Worksheet(options.SheetIndex ?? 1);

        var firstRow = worksheet.FirstRowUsed();
        if (firstRow == null) return Task.FromResult(result);

        var range = worksheet.RangeUsed();
        if (range == null) return Task.FromResult(result);

        var rows = range.Rows().ToList();
        if (rows.Count == 0) return Task.FromResult(result);

        if (options.HasHeaders)
        {
            foreach (var cell in rows[0].Cells())
            {
                var val = cell.Value.ToString()?.Trim();
                if (!string.IsNullOrEmpty(val))
                    result.Headers.Add(val);
            }
        }

        var startRow = options.HasHeaders ? 1 : 0;
        for (int i = startRow; i < rows.Count; i++)
        {
            if (result.Rows.Count >= options.MaxRows) break;

            var row = new Dictionary<string, object?>();
            var cells = rows[i].Cells().ToList();

            for (int j = 0; j < cells.Count; j++)
            {
                var header = j < result.Headers.Count ? result.Headers[j] : $"Column{j + 1}";
                var cell = cells[j];
                var val = cell.Value;

                if (val.IsText)
                    row[header] = string.IsNullOrEmpty(val.GetText()) ? null : val.GetText();
                else if (val.IsNumber)
                    row[header] = val.GetNumber();
                else if (val.IsDateTime)
                    row[header] = val.GetDateTime();
                else if (val.IsBoolean)
                    row[header] = val.GetBoolean();
                else
                    row[header] = null;
            }

            if (!options.HasHeaders && i == startRow)
            {
                foreach (var k in row.Keys)
                {
                    if (!result.Headers.Contains(k))
                        result.Headers.Add(k);
                }
            }

            if (options.HasHeaders && row.Values.All(v => v == null || string.IsNullOrEmpty(v?.ToString())))
                continue;

            result.Rows.Add(row);
        }

        return Task.FromResult(result);
    }

    public Task<Stream> SerializeAsync(IReadOnlyList<Dictionary<string, object?>> data, FileSerializeOptions options)
    {
        var memoryStream = new MemoryStream();
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add(options.SheetName ?? "Export");

        var columns = options.Columns ?? (data.Count > 0 ? data[0].Keys.ToList() : []);

        if (options.IncludeHeaders)
        {
            for (int c = 0; c < columns.Count; c++)
                worksheet.Cell(1, c + 1).Value = columns[c];
        }

        var startRow = options.IncludeHeaders ? 2 : 1;
        for (int i = 0; i < data.Count; i++)
        {
            var row = data[i];
            for (int c = 0; c < columns.Count; c++)
            {
                var val = row.GetValueOrDefault(columns[c]);
                var cell = worksheet.Cell(startRow + i, c + 1);
                if (val == null)
                    cell.Value = "";
                else if (val is DateTime dt)
                    cell.Value = dt;
                else if (val is bool b)
                    cell.Value = b;
                else if (val is double dbl)
                    cell.Value = dbl;
                else if (val is int iv)
                    cell.Value = iv;
                else if (val is long lv)
                    cell.Value = lv;
                else
                    cell.Value = val.ToString();
            }
        }

        worksheet.Columns().AdjustToContents();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;
        return Task.FromResult((Stream)memoryStream);
    }
}
