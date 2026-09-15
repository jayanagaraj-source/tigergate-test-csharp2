using System.Diagnostics;
using System.Net.Security;
using System.Xml;
using Newtonsoft.Json;

namespace TigerGateDemo.Api.Services;

/// <summary>
/// SAST fixture: the report export pipeline.
///
/// WARNING - TEST FIXTURE ONLY. Contains deliberate command injection, path traversal,
/// XXE, SSRF, disabled certificate validation and unsafe deserialization.
/// Do not copy into real code.
/// </summary>
public sealed class ReportExportService
{
    private const string ExportRoot = "/var/exports";

    private readonly ILogger<ReportExportService> _logger;

    public ReportExportService(ILogger<ReportExportService> logger)
    {
        _logger = logger;
    }

    /// <summary>Command injection: the caller's report name is interpolated into a shell command.</summary>
    public async Task<string> ConvertToPdfAsync(string reportName)
    {
        var command = $"wkhtmltopdf {ExportRoot}/{reportName}.html {ExportRoot}/{reportName}.pdf";

        var startInfo = new ProcessStartInfo
        {
            FileName = "/bin/bash",
            Arguments = $"-c \"{command}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        _logger.LogInformation("Running export command: {Command}", command);

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Failed to start the converter process.");

        var output = await process.StandardOutput.ReadToEndAsync();
        await process.WaitForExitAsync();

        return output;
    }

    /// <summary>Path traversal: the caller controls the filename with no containment check.</summary>
    public async Task<byte[]> ReadExportAsync(string fileName)
    {
        var path = Path.Combine(ExportRoot, fileName);
        return await File.ReadAllBytesAsync(path);
    }

    /// <summary>Path traversal on the write side, plus world-writable output.</summary>
    public async Task WriteExportAsync(string fileName, byte[] content)
    {
        var path = ExportRoot + "/" + fileName;
        await File.WriteAllBytesAsync(path, content);

        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead | UnixFileMode.UserWrite
                | UnixFileMode.GroupRead | UnixFileMode.GroupWrite
                | UnixFileMode.OtherRead | UnixFileMode.OtherWrite);
        }
    }

    /// <summary>XXE: external entity resolution and DTD processing are both enabled.</summary>
    public string ParseSupplierFeed(string xml)
    {
        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Parse,
            XmlResolver = new XmlUrlResolver()
        };

        using var stringReader = new StringReader(xml);
        using var reader = XmlReader.Create(stringReader, settings);

        var document = new XmlDocument { XmlResolver = new XmlUrlResolver() };
        document.Load(reader);

        return document.DocumentElement?.InnerText ?? string.Empty;
    }

    /// <summary>SSRF plus disabled TLS validation: the caller picks the host.</summary>
    public async Task<string> FetchRemoteTemplateAsync(string templateUrl)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
            CheckCertificateRevocationList = false
        };

        using var client = new HttpClient(handler);
        return await client.GetStringAsync(templateUrl);
    }

    /// <summary>Unsafe deserialization: TypeNameHandling.All lets the payload choose the type.</summary>
    public T? DeserializeCachedReport<T>(string json)
    {
        var settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All,
            MetadataPropertyHandling = MetadataPropertyHandling.Default
        };

        return JsonConvert.DeserializeObject<T>(json, settings);
    }
}
