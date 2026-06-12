using API.DTOs;
using API.Services.ImportExport.AI;

namespace API.Services.ImportExport.Validation;

public class SynonymDetector
{
    private readonly SynonymRegistry _registry;

    public SynonymDetector(SynonymRegistry registry)
    {
        _registry = registry;
    }

    public List<SynonymIssue> DetectIssues(FileParseResult parsedData, List<TemplateFieldInfo> templateFields)
    {
        var issues = new List<SynonymIssue>();
        var standardFields = templateFields.ToDictionary(f => f.FieldName, StringComparer.OrdinalIgnoreCase);
        var headerMap = BuildHeaderMap(parsedData.Headers, standardFields.Keys.ToList());

        foreach (var (fileCol, standardCol) in headerMap.Mappings)
        {
            if (fileCol == standardCol) continue;
            var colIndex = parsedData.Headers.IndexOf(fileCol);
            if (colIndex < 0) continue;

            issues.Add(new SynonymIssue
            {
                Row = 0,
                Column = fileCol,
                OriginalValue = fileCol,
                SuggestedValue = standardCol,
                Confidence = 0.95,
                Category = "column_name",
            });
        }

        foreach (var (fileCol, _) in headerMap.Mappings)
        {
            var standardCol = headerMap.Mappings[fileCol];
            var colIndex = parsedData.Headers.IndexOf(fileCol);
            if (colIndex < 0) continue;

            if (!standardFields.TryGetValue(standardCol, out var fieldInfo)) continue;

            for (int i = 0; i < parsedData.Rows.Count; i++)
            {
                var row = parsedData.Rows[i];
                if (!row.TryGetValue(fileCol, out var rawValue) || rawValue == null) continue;

                var strVal = rawValue.ToString()?.Trim();
                if (string.IsNullOrEmpty(strVal)) continue;

                if (fieldInfo.AllowedValues is { Count: > 0 })
                {
                    var match = fieldInfo.AllowedValues.FirstOrDefault(av =>
                        string.Equals(av, strVal, StringComparison.OrdinalIgnoreCase));
                    if (match != null && match != strVal)
                    {
                        issues.Add(new SynonymIssue
                        {
                            Row = i + 1,
                            Column = fileCol,
                            OriginalValue = strVal,
                            SuggestedValue = match,
                            Confidence = 0.98,
                            Category = "case",
                        });
                        continue;
                    }

                    var standard = _registry.FindStandard(strVal);
                    if (standard != null && fieldInfo.AllowedValues.Any(av =>
                            string.Equals(av, standard, StringComparison.OrdinalIgnoreCase)))
                    {
                        issues.Add(new SynonymIssue
                        {
                            Row = i + 1,
                            Column = fileCol,
                            OriginalValue = strVal,
                            SuggestedValue = standard,
                            Confidence = 0.9,
                            Category = "synonym",
                        });
                        continue;
                    }

                    if (fieldInfo.DataType == "bool")
                    {
                        var boolStd = _registry.FindStandard(strVal);
                        if (boolStd is "true" or "false")
                        {
                            issues.Add(new SynonymIssue
                            {
                                Row = i + 1,
                                Column = fileCol,
                                OriginalValue = strVal,
                                SuggestedValue = boolStd,
                                Confidence = 0.85,
                                Category = "boolean_synonym",
                            });
                        }
                    }
                }

                if (fieldInfo.ForeignKeyEntity is { } fkEntity)
                {
                    var standard = _registry.FindStandard(strVal);
                    if (standard != null && standard != strVal)
                    {
                        issues.Add(new SynonymIssue
                        {
                            Row = i + 1,
                            Column = fileCol,
                            OriginalValue = strVal,
                            SuggestedValue = standard,
                            Confidence = 0.88,
                            Category = "fk_synonym",
                        });
                    }
                }

                if (fieldInfo.DataType == "bool" && strVal is not ("true" or "false" or "True" or "False" or "TRUE" or "FALSE"))
                {
                    var boolStd = _registry.FindStandard(strVal);
                    if (boolStd is "true" or "false")
                    {
                        issues.Add(new SynonymIssue
                        {
                            Row = i + 1,
                            Column = fileCol,
                            OriginalValue = strVal,
                            SuggestedValue = boolStd,
                            Confidence = 0.8,
                            Category = "boolean_format",
                        });
                    }
                }
            }
        }

        return issues;
    }

    public (List<string> Headers, Dictionary<string, string> Mappings) BuildHeaderMap(List<string> fileHeaders, List<string> standardHeaders)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var matchedStandardHeaders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var fileHeader in fileHeaders)
        {
            var exactMatch = standardHeaders.FirstOrDefault(sh =>
                string.Equals(sh, fileHeader, StringComparison.OrdinalIgnoreCase));
            if (exactMatch != null)
            {
                mappings[fileHeader] = exactMatch;
                matchedStandardHeaders.Add(exactMatch);
                continue;
            }

            var synonymMatch = _registry.FindStandard(fileHeader);
            if (synonymMatch != null)
            {
                var standardMatch = standardHeaders.FirstOrDefault(sh =>
                    string.Equals(sh, synonymMatch, StringComparison.OrdinalIgnoreCase));
                if (standardMatch != null)
                {
                    mappings[fileHeader] = standardMatch;
                    matchedStandardHeaders.Add(standardMatch);
                    continue;
                }
            }

            var normalized = NormalizeForMatch(fileHeader);
            var fuzzyMatch = standardHeaders.FirstOrDefault(sh =>
                NormalizeForMatch(sh) == normalized);
            if (fuzzyMatch != null)
            {
                mappings[fileHeader] = fuzzyMatch;
                matchedStandardHeaders.Add(fuzzyMatch);
                continue;
            }

            mappings[fileHeader] = fileHeader;
        }

        return (fileHeaders, mappings);
    }

    private static string NormalizeForMatch(string s)
    {
        return s.Trim().ToLowerInvariant()
            .Replace(" ", "").Replace("_", "").Replace("-", "").Replace(".", "");
    }
}
