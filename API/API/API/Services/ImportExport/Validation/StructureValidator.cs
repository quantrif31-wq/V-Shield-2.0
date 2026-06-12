using API.DTOs;
using API.Services.ImportExport.AI;

namespace API.Services.ImportExport.Validation;

public class StructureValidator : IStructureValidator
{
    public ValidationResult Validate(FileParseResult data, IEntityImportHandler handler)
    {
        var result = new ValidationResult();
        var templateFields = handler.GetTemplateFields();

        var schemaErrors = CheckSchema(data, templateFields);
        result.Errors.AddRange(schemaErrors);

        if (data.Headers.Count == 0)
        {
            result.Errors.Add(new ValidationError
            {
                Row = 0, Column = null, Message = "File không có header hoặc dữ liệu trống.",
                ErrorCode = "NO_HEADERS", IsAIFixable = false,
            });
            result.IsValid = false;
            return result;
        }

        foreach (var field in templateFields.Where(f => f.IsRequired))
        {
            if (!data.Headers.Any(h => string.Equals(h, field.FieldName, StringComparison.OrdinalIgnoreCase)))
            {
                var synonym = FindSynonymHeader(data.Headers, field.FieldName);
                if (synonym == null)
                {
                    result.Errors.Add(new ValidationError
                    {
                        Row = 0,
                        Column = field.FieldName,
                        Message = $"Thiếu cột bắt buộc '{field.FieldName}' ({field.DisplayName})",
                        ErrorCode = "MISSING_REQUIRED_COLUMN",
                        IsAIFixable = false,
                    });
                    result.HasStructuralIssues = true;
                }
            }
        }

        var unknownHeaders = data.Headers
            .Where(h => !templateFields.Any(f =>
                string.Equals(f.FieldName, h, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (unknownHeaders.Count > 0)
        {
            result.Warnings.Add(new ValidationWarning
            {
                Row = 0,
                Column = null,
                Message = $"Cột không xác định: {string.Join(", ", unknownHeaders)}",
            });
        }

        result.IsValid = result.Errors.Count == 0;
        return result;
    }

    public List<ValidationError> CheckSchema(FileParseResult data, List<TemplateFieldInfo> templateFields)
    {
        var errors = new List<ValidationError>();

        foreach (var row in data.Rows.Select((r, i) => new { r, i }))
        {
            foreach (var field in templateFields)
            {
                if (!row.r.TryGetValue(field.FieldName, out var value) || value == null)
                {
                    if (field.IsRequired)
                    {
                        errors.Add(new ValidationError
                        {
                            Row = row.i + 1,
                            Column = field.FieldName,
                            Message = $"Dòng {row.i + 1}: '{field.DisplayName}' không được để trống",
                            ErrorCode = "REQUIRED_FIELD_EMPTY",
                            IsAIFixable = false,
                        });
                    }
                    continue;
                }

                var strVal = value.ToString()?.Trim();

                if (field.DataType == "int" && !int.TryParse(strVal, out _) && !string.IsNullOrEmpty(strVal))
                {
                    errors.Add(new ValidationError
                    {
                        Row = row.i + 1,
                        Column = field.FieldName,
                        Message = $"Dòng {row.i + 1}: '{field.DisplayName}' phải là số nguyên",
                        ErrorCode = "INVALID_INT",
                        IsAIFixable = strVal != null && strVal.Any(char.IsLetter),
                    });
                }

                if (field.DataType == "bool" && strVal is not (null or "" or "true" or "false" or "True" or "False"))
                {
                    errors.Add(new ValidationError
                    {
                        Row = row.i + 1,
                        Column = field.FieldName,
                        Message = $"Dòng {row.i + 1}: '{field.DisplayName}' phải là true/false",
                        ErrorCode = "INVALID_BOOL",
                        IsAIFixable = true,
                    });
                }

                if (field.AllowedValues is { Count: > 0 } && strVal != null)
                {
                    var match = field.AllowedValues.Any(av =>
                        string.Equals(av, strVal, StringComparison.OrdinalIgnoreCase));
                    if (!match)
                    {
                        errors.Add(new ValidationError
                        {
                            Row = row.i + 1,
                            Column = field.FieldName,
                            Message = $"Dòng {row.i + 1}: '{strVal}' không hợp lệ cho '{field.DisplayName}'",
                            ErrorCode = "INVALID_VALUE",
                            IsAIFixable = true,
                        });
                    }
                }
            }
        }

        return errors;
    }

    private static string? FindSynonymHeader(List<string> headers, string standardField)
    {
        var reg = new SynonymRegistry();
        foreach (var header in headers)
        {
            var found = reg.FindStandard(header);
            if (found != null && string.Equals(found, standardField, StringComparison.OrdinalIgnoreCase))
                return header;
        }
        return null;
    }
}
