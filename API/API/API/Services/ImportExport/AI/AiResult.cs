namespace API.Services.ImportExport.AI;

public class FileAnalysisResult
{
    public bool IsReadable { get; set; }
    public string DetectedFormat { get; set; } = null!;
    public string? SuggestedAction { get; set; }
    public string? Message { get; set; }
    public FileParseResult? ParsedData { get; set; }
    public List<FileParseWarning>? Warnings { get; set; }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public bool HasSynonymIssues { get; set; }
    public bool HasStructuralIssues { get; set; }
    public List<ValidationError> Errors { get; set; } = [];
    public List<SynonymIssue> SynonymIssues { get; set; } = [];
    public List<ValidationWarning> Warnings { get; set; } = [];
}

public class ValidationError
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = null!;
    public string? ErrorCode { get; set; }
    public bool IsAIFixable { get; set; }
}

public class ValidationWarning
{
    public int Row { get; set; }
    public string? Column { get; set; }
    public string Message { get; set; } = null!;
}

public class SynonymIssue
{
    public int Row { get; set; }
    public string Column { get; set; } = null!;
    public string OriginalValue { get; set; } = null!;
    public string SuggestedValue { get; set; } = null!;
    public double Confidence { get; set; }
    public string Category { get; set; } = null!;
}

public class AiProcessingResult
{
    public string Status { get; set; } = null!;
    public FileParseResult NormalizedData { get; set; } = new();
    public List<SynonymChangeLog> Changes { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class SynonymChangeLog
{
    public int Row { get; set; }
    public string Column { get; set; } = null!;
    public string OriginalValue { get; set; } = null!;
    public string NormalizedValue { get; set; } = null!;
    public string Reason { get; set; } = null!;
}

public class AiImportPreviewResponse
{
    public Guid SessionId { get; set; }
    public FileParseResult PreviewData { get; set; } = new();
    public List<SynonymChangeLog> Changes { get; set; } = [];
    public ValidationResult Validation { get; set; } = new();
    public bool ReadyForImport { get; set; }
    public int ChangeCount { get; set; }
    public int TotalRows { get; set; }
}

public class AiImportRequest
{
    public bool ConfirmNormalization { get; set; }
    public bool OverrideConflicts { get; set; }
}

public class AiSession
{
    public Guid SessionId { get; set; } = Guid.NewGuid();
    public string EntityType { get; set; } = null!;
    public string FileName { get; set; } = null!;
    public string FileFormat { get; set; } = null!;
    public byte[]? OriginalFileContent { get; set; }
    public FileParseResult? ParsedData { get; set; }
    public FileParseResult? NormalizedData { get; set; }
    public List<SynonymChangeLog> Changes { get; set; } = [];
    public ValidationResult? Validation { get; set; }
    public bool AiWasUsed { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class AiConfig
{
    public bool Enabled { get; set; } = true;
    public string OcrEngine { get; set; } = "Tesseract";
    public string? LlmProvider { get; set; }
    public LlmSettings? Llm { get; set; }
    public int MaxOcrFileSize { get; set; } = 20 * 1024 * 1024;
    public double AutoNormalizeConfidence { get; set; } = 0.85;
}

public class LlmSettings
{
    public string? Endpoint { get; set; }
    public string? Model { get; set; }
    public string? ApiKey { get; set; }
}
