param(
    [Parameter(Mandatory = $true)]
    [string]$SourceDirectory,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

$ErrorActionPreference = 'Stop'
$source = (Resolve-Path -LiteralPath $SourceDirectory).Path
$output = [System.IO.Path]::GetFullPath($OutputPath)

if (-not (Test-Path -LiteralPath $source -PathType Container)) {
    throw "Source directory does not exist: $source"
}

if ($output.StartsWith($source + [System.IO.Path]::DirectorySeparatorChar, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'Output archive must be outside the source directory.'
}

$outputDirectory = Split-Path -Parent $output
if (-not (Test-Path -LiteralPath $outputDirectory)) {
    New-Item -ItemType Directory -Path $outputDirectory | Out-Null
}

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

$fixedTimestamp = [DateTimeOffset]::Parse('2026-08-05T00:00:00Z')
$stream = [System.IO.File]::Open($output, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite, [System.IO.FileShare]::None)

try {
    $archive = [System.IO.Compression.ZipArchive]::new($stream, [System.IO.Compression.ZipArchiveMode]::Create, $false)
    try {
        Get-ChildItem -LiteralPath $source -Recurse -File |
            Sort-Object { $_.FullName.Substring($source.Length).Replace('\', '/') } |
            ForEach-Object {
                $relative = $_.FullName.Substring($source.Length).TrimStart('\', '/').Replace('\', '/')
                $entry = $archive.CreateEntry("dist/$relative", [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = $fixedTimestamp
                $input = [System.IO.File]::OpenRead($_.FullName)
                try {
                    $entryStream = $entry.Open()
                    try { $input.CopyTo($entryStream) } finally { $entryStream.Dispose() }
                } finally { $input.Dispose() }
            }
    } finally {
        $archive.Dispose()
    }
} finally {
    $stream.Dispose()
}

$digest = (Get-FileHash -LiteralPath $output -Algorithm SHA256).Hash.ToLowerInvariant()
$size = (Get-Item -LiteralPath $output).Length
Write-Output "Artifact: $output"
Write-Output "Bytes: $size"
Write-Output "SHA-256: $digest"
