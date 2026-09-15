using System.Text;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;

namespace TigerGateDemo.Worker;

/// <summary>
/// Serialises a report payload and stores it as a single-entry zip archive.
/// </summary>
public sealed class ReportArchiver
{
    private readonly ILogger<ReportArchiver> _logger;

    public ReportArchiver(ILogger<ReportArchiver> logger)
    {
        _logger = logger;
    }

    public async Task<string> ArchiveAsync<T>(string reportName, T payload, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(reportName))
        {
            throw new ArgumentException("Report name is required.", nameof(reportName));
        }

        Directory.CreateDirectory(outputDirectory);

        var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
        var archivePath = Path.Combine(outputDirectory, $"{reportName}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip");

        await using var fileStream = File.Create(archivePath);
        using var zipStream = new ZipOutputStream(fileStream);
        zipStream.SetLevel(6);

        var entry = new ZipEntry($"{reportName}.json") { DateTime = DateTime.UtcNow };
        zipStream.PutNextEntry(entry);

        var bytes = Encoding.UTF8.GetBytes(json);
        await zipStream.WriteAsync(bytes);
        zipStream.CloseEntry();

        _logger.LogInformation("Wrote report archive {ArchivePath} ({Bytes} bytes)", archivePath, bytes.Length);

        return archivePath;
    }
}
