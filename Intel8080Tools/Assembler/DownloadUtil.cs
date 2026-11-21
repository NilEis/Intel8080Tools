using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Intel8080Tools.Assembler;

public static class DownloadUtil
{
    public static bool GetFileStringCached(bool cacheFile, string fileName, string fileLink,
        [NotNullWhen(true)] out string? str)
    {
        str = null;
        if (!GetFileBytesCached(cacheFile, fileName, fileLink, out var buffer, false))
        {
            return false;
        }

        str = Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        return true;

    }

    public static bool GetFileBytesCached(bool cacheFile, string fileName, string fileLink,
        [NotNullWhen(true)] out byte[]? buffer, bool asBytes = true)
    {
        var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        var filePath = Path.Combine(desktopPath, fileName);
        if (File.Exists(filePath))
        {
            Console.Out.WriteLine($"Using cached {fileName}");
            buffer = asBytes ? File.ReadAllBytes(filePath) : Encoding.UTF8.GetBytes(File.ReadAllText(filePath));
        }
        else
        {
            Console.Out.WriteLine($"Downloading {fileName}");
            using var client = new HttpClient();
            try
            {
                buffer = asBytes
                    ? client.GetByteArrayAsync(fileLink).Result
                    : Encoding.UTF8.GetBytes(client.GetStringAsync(fileLink).Result);

                // Optionally cache the file on the desktop
                if (cacheFile)
                {
                    File.WriteAllBytes(filePath, buffer);
                }
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("Error downloading file: " + e.Message);
                buffer = null;
                return false;
            }
        }

        return true;
    }
}