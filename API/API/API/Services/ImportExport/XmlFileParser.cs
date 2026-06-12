using System.Xml;
using System.Xml.Linq;

namespace API.Services.ImportExport;

public class XmlFileParser : IFileParser
{
    public string Format => "xml";

    public Task<FileParseResult> ParseAsync(Stream stream, FileParseOptions options)
    {
        var result = new FileParseResult();

        using var reader = new StreamReader(stream, leaveOpen: true);
        var content = reader.ReadToEnd();
        var doc = XDocument.Parse(content);

        if (doc.Root == null) return Task.FromResult(result);

        XElement? collectionElement = null;
        if (doc.Root.HasElements)
        {
            var rootName = doc.Root.Name.LocalName;
            if (rootName is "root" or "items")
                collectionElement = doc.Root;
            else
                collectionElement = doc.Root.Elements().FirstOrDefault()?.Parent;
        }

        var items = collectionElement?.Elements().ToList() ?? [doc.Root];

        var headers = new HashSet<string>();
        foreach (var item in items)
        {
            foreach (var elem in item.Elements())
                headers.Add(elem.Name.LocalName);
        }
        result.Headers = headers.ToList();

        foreach (var item in items.Take(options.MaxRows))
        {
            var row = new Dictionary<string, object?>();
            foreach (var elem in item.Elements())
            {
                row[elem.Name.LocalName] = elem.Value;
            }

            var attrs = item.Attributes().ToList();
            foreach (var attr in attrs)
                row[$"@{attr.Name.LocalName}"] = attr.Value;

            result.Rows.Add(row);
        }

        return Task.FromResult(result);
    }

    public Task<Stream> SerializeAsync(IReadOnlyList<Dictionary<string, object?>> data, FileSerializeOptions options)
    {
        var columns = options.Columns ?? (data.Count > 0 ? data[0].Keys.ToList() : []);

        var doc = new XDocument(new XElement("items",
            data.Select(row => new XElement("item",
                columns.Select(col =>
                {
                    var val = row.GetValueOrDefault(col);
                    return new XElement(XmlConvert.EncodeLocalName(col), val?.ToString());
                })
            ))
        ));

        var memoryStream = new MemoryStream();
        using var writer = XmlWriter.Create(memoryStream, new XmlWriterSettings { Indent = true, Async = false });
        doc.Save(writer);
        writer.Flush();
        memoryStream.Position = 0;
        return Task.FromResult((Stream)memoryStream);
    }
}
