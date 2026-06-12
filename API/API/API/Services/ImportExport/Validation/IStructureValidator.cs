using API.DTOs;
using API.Services.ImportExport.AI;

namespace API.Services.ImportExport.Validation;

public interface IStructureValidator
{
    ValidationResult Validate(FileParseResult data, IEntityImportHandler handler);
    List<ValidationError> CheckSchema(FileParseResult data, List<TemplateFieldInfo> templateFields);
}
