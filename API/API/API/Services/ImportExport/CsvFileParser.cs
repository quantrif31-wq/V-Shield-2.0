using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace API.Services.ImportExport;

public class CsvFileParser : IFileParser
{
    public string Format => "csv";

    public Task<FileParseResult> ParseAsync(Stream stream, FileParseOptions options)
    {
        var result = new FileParseResult();
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = options.HasHeaders,
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            Delimiter = options.Delimiter ?? ",",
            Encoding = Encoding.UTF8,
        };

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(reader, config);

        if (options.HasHeaders)
        {
            csv.Read();
            csv.ReadHeader();
            result.Headers.AddRange(csv.HeaderRecord ?? []);
        }

        while (csv.Read())
        {
            if (result.Rows.Count >= options.MaxRows) break;

            var row = new Dictionary<string, object?>();

            if (!options.HasHeaders)
            {
                for (int i = 0; i < csv.ColumnCount; i++)
                {
                    var colName = $"Column{i + 1}";
                    csv.TryGetField<string>(i, out var val);
                    row[colName] = string.IsNullOrEmpty(val) ? null : val;
                    if (result.Headers.Count <= i)
                        result.Headers.Add(colName);
                }
            }
            else
            {
                foreach (var header in result.Headers)
                {
                    var raw = csv.GetField(header);
                    row[header] = string.IsNullOrEmpty(raw) ? null : raw;
                }
            }

            result.Rows.Add(row);
        }

        return Task.FromResult(result);
    }

    public Task<Stream> SerializeAsync(IReadOnlyList<Dictionary<string, object?>> data, FileSerializeOptions options)
    {
        var memoryStream = new MemoryStream();
        using var writer = new StreamWriter(memoryStream, Encoding.UTF8, leaveOpen: true);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        var columns = options.Columns ?? (data.Count > 0 ? data[0].Keys.ToList() : []);

        if (options.IncludeHeaders)
        {
            foreach (var col in columns)
                csv.WriteField(col);
            csv.NextRecord();
        }

        foreach (var row in data)
        {
            foreach (var col in columns)
            {
                var val = row.GetValueOrDefault(col);
                csv.WriteField(val?.ToString());
            }
            csv.NextRecord();
        }

        writer.Flush();
        memoryStream.Position = 0;
        return Task.FromResult((Stream)memoryStream);
    }
}
