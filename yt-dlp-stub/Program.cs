using System.Net.Http.Headers;
using System.Net.Sockets;

namespace yt_dlp;

internal static class Program
{
    private static string _logFilePath = string.Empty;
    private const string BaseUrl = "http://127.0.0.1:9696";
    
    private static class SourceApps
    {
        public const string Unknown = "unknown";
        public const string VRChat = "vrchat";
        public const string Resonite = "resonite";
        public const string ChilloutVR = "chilloutvr";
    }

    private static void WriteLog(string message)
    {
        if (string.IsNullOrEmpty(_logFilePath))
            return;

        try
        {
            using var sw = new StreamWriter(_logFilePath, true);
            sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}");
        }
        catch
        {
            // ignore
        }
    }

    public static async Task Main(string[] args)
    {
        string processPath = Environment.ProcessPath ?? string.Empty;
        string source = GetSourceApp(processPath);
        _logFilePath = GetYtdlpLogFilePath(source);

        string url = string.Empty;
        bool avPro = true;
        bool dumpJson = false;

        foreach (string arg in args)
        {
            if (arg.Contains("[protocol^=http]"))
            {
                avPro = false;
                continue;
            }

            // Resonites arguments:
            // --flat-playlist -i -J -s --no-playlist
            
            // ChilloutVR arguments:
            // -f --no-playlist --dump-json

            // TODO: Verify HDR videos don't crash CVR anymore. As the arguments used by the source app are not fully respected,
            // we may be returning videos which the app tries to explicitly prevent from being loaded (need ask kafe).

            if (arg.Equals("-J", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("--dump-json", StringComparison.OrdinalIgnoreCase) ||
                arg.StartsWith("--dump-single-json", StringComparison.OrdinalIgnoreCase))
            {
                dumpJson = true;
                continue;
            }

            if (!arg.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                continue;

            url = arg;
            break;
        }

        WriteLog($"Starting with args: {string.Join(" ", args)}, avPro: {avPro}, dumpJson: {dumpJson}, source: {source}");

        if (string.IsNullOrEmpty(url))
        {
            WriteLog("[Error] No URL found in arguments");
            await Console.Error.WriteLineAsync("ERROR: [VRCVideoCacher] No URL found in arguments");
            Environment.ExitCode = 1;
            return;
        }

        try
        {
            using HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("VRCVideoCacher", "1.0"));

            string inputUrl = Uri.EscapeDataString(url);
            HttpResponseMessage response = await httpClient.GetAsync(
                $"{BaseUrl}/api/getvideo?url={inputUrl}&avpro={avPro}&source={source}&dumpJson={dumpJson}");

            string output = await response.Content.ReadAsStringAsync();
            WriteLog($"[Response] {output}");

            if (!response.IsSuccessStatusCode)
                throw new Exception(output);

            Console.WriteLine(output);
        }
        catch (HttpRequestException ex) when (ex.InnerException is SocketException { SocketErrorCode: SocketError.ConnectionRefused })
        {
            WriteLog("[Error] Connection refused. Is the server running?");
            await Console.Error.WriteLineAsync("ERROR: [VRCVideoCacher] Connection refused. Is VRCVideoCacher running?");
            Environment.ExitCode = 1;
        }
        catch (Exception ex)
        {
            WriteLog($"[Error] {ex}");
            await Console.Error.WriteLineAsync($"ERROR: [VRCVideoCacher] {ex.GetType().Name}: {ex.Message}");
            Environment.ExitCode = 1;
        }
    }

    private static string GetSourceApp(string processPath)
    {
        if (processPath.Contains("VRChat", StringComparison.OrdinalIgnoreCase))
            return SourceApps.VRChat;

        if (processPath.Contains("Resonite", StringComparison.OrdinalIgnoreCase))
            return SourceApps.Resonite;
        
        if (processPath.Contains("ChilloutVR", StringComparison.OrdinalIgnoreCase))
            return SourceApps.ChilloutVR;
        
        return SourceApps.Unknown;
    }

    // Seems silly to write to VRChats log file... ?
    // I assume this is for debugging or VRCX to parse, but not sure as source wasn't commented :(
    private static string GetYtdlpLogFilePath(string source)
    {
        if (source == SourceApps.VRChat)
        {
            string localLow = Path.Combine(
                Directory.GetParent(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData))!.FullName,
                "LocalLow");

            string appDataPath = Path.Combine(localLow, "VRChat", "VRChat", "Tools");
            Directory.CreateDirectory(appDataPath);
            return Path.Combine(appDataPath, "ytdl.log");
        }

        // Not writing to other apps log files
        return string.Empty;
    }
}