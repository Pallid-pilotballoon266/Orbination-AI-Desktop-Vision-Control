using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using ModelContextProtocol.Server;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace DesktopControlMcp.Tools;

[McpServerToolType]
public sealed class ScreenTools
{
    [McpServerTool(Name = "get_screen_info"), Description("Get information about all connected monitors (position, size, primary status).")]
    public static string GetScreenInfo()
    {
        var sb = new StringBuilder();
        var screens = System.Windows.Forms.Screen.AllScreens;
        sb.AppendLine($"Monitors: {screens.Length}");
        for (int i = 0; i < screens.Length; i++)
        {
            var s = screens[i];
            sb.AppendLine($"  [{i}] {s.Bounds.Width}x{s.Bounds.Height} @ {s.Bounds.X},{s.Bounds.Y}{(s.Primary ? " (primary)" : "")}");
        }
        return sb.ToString();
    }

    [McpServerTool(Name = "screenshot_to_file"), Description("Take a full screenshot across all monitors and save to a PNG file.")]
    public static string ScreenshotToFile(
        [Description("File path to save the PNG screenshot")] string savePath)
    {
        var vx = NativeInput.GetSystemMetrics(NativeInput.SM_XVIRTUALSCREEN);
        var vy = NativeInput.GetSystemMetrics(NativeInput.SM_YVIRTUALSCREEN);
        var vw = NativeInput.GetSystemMetrics(NativeInput.SM_CXVIRTUALSCREEN);
        var vh = NativeInput.GetSystemMetrics(NativeInput.SM_CYVIRTUALSCREEN);

        using var bmp = new Bitmap(vw, vh, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(vx, vy, 0, 0, new Size(vw, vh));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(savePath))!);
        bmp.Save(savePath, ImageFormat.Png);

        return $"Saved {vw}x{vh} screenshot to {savePath}";
    }

    [McpServerTool(Name = "screenshot_region"), Description("Take a screenshot of a specific screen region and save to file.")]
    public static string ScreenshotRegion(
        [Description("Left edge X coordinate")] int x,
        [Description("Top edge Y coordinate")] int y,
        [Description("Width of capture region")] int width,
        [Description("Height of capture region")] int height,
        [Description("File path to save the PNG screenshot")] string savePath)
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(savePath))!);
        bmp.Save(savePath, ImageFormat.Png);

        return $"Saved {width}x{height} screenshot to {savePath}";
    }

    [McpServerTool(Name = "ocr_screen_region"), Description("Capture a screen region and run Windows OCR on it. Returns recognized text with positions. Works on any app including custom-rendered UIs (Flutter, Electron, games) where UIAutomation can't see elements.")]
    public static string OcrScreenRegion(
        [Description("Left edge X coordinate")] int x,
        [Description("Top edge Y coordinate")] int y,
        [Description("Width of capture region")] int width,
        [Description("Height of capture region")] int height,
        [Description("Language code (default: en, options: en, el, de, fr, es, it, etc.)")] string language = "en")
    {
        // Capture the screen region
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        }

        // Convert System.Drawing.Bitmap to WinRT SoftwareBitmap
        var result = RunOcrOnBitmap(bmp, language, x, y);
        return result;
    }

    [McpServerTool(Name = "ocr_window"), Description("Run OCR on an entire window. Captures the window region and reads all visible text with positions. Perfect for apps where UIAutomation returns few elements.")]
    public static string OcrWindow(
        [Description("Part of window title (case-insensitive)")] string windowTitle,
        [Description("Language code (default: en)")] string language = "en")
    {
        var hWnd = Native.Win32.FindWindowByTitle(windowTitle);
        if (hWnd == nint.Zero) return $"NOT FOUND: no window matching '{windowTitle}'";

        Native.Win32.FocusWindow(hWnd);
        Thread.Sleep(200);

        Native.Win32.GetWindowRect(hWnd, out var rect);
        int wx = rect.Left, wy = rect.Top, ww = rect.Width, wh = rect.Height;

        // Clamp to virtual screen bounds
        ww = Math.Min(ww, 4000);
        wh = Math.Min(wh, 3000);

        using var bmp = new Bitmap(ww, wh, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(wx, wy, 0, 0, new Size(ww, wh));
        }

        return RunOcrOnBitmap(bmp, language, wx, wy);
    }

    [McpServerTool(Name = "ocr_find_text"), Description("Use OCR to find specific text on screen and return its click coordinates. Works on any app including custom-rendered UIs where UIAutomation can't detect elements.")]
    public static string OcrFindText(
        [Description("Text to search for (case-insensitive)")] string searchText,
        [Description("Left edge X of search region")] int x,
        [Description("Top edge Y of search region")] int y,
        [Description("Width of search region")] int width,
        [Description("Height of search region")] int height,
        [Description("Language code (default: en)")] string language = "en")
    {
        using var bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.CopyFromScreen(x, y, 0, 0, new Size(width, height));
        }

        var ocrResult = RunOcrAsync(bmp, language);
        if (ocrResult == null) return "ERROR: OCR engine not available for language '{language}'";

        var sb = new StringBuilder();
        int found = 0;

        foreach (var line in ocrResult.Lines)
        {
            if (line.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase))
            {
                // Calculate absolute screen coordinates
                int absX = x + (int)(line.Words[0].BoundingRect.X + line.Words[0].BoundingRect.Width / 2);
                int absY = y + (int)(line.Words[0].BoundingRect.Y + line.Words[0].BoundingRect.Height / 2);

                // Get the full line bounding box
                double lx = line.Words.Min(w => w.BoundingRect.X);
                double ly = line.Words.Min(w => w.BoundingRect.Y);
                double lr = line.Words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
                double lb = line.Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);
                int centerX = x + (int)((lx + lr) / 2);
                int centerY = y + (int)((ly + lb) / 2);

                sb.AppendLine($"FOUND: \"{line.Text}\" @ {centerX},{centerY}");
                found++;
            }
        }

        if (found == 0) return $"NOT FOUND: '{searchText}' not visible in region";
        return sb.ToString();
    }

    // ─── OCR Helpers ────────────────────────────────────────────────────────────

    private static string RunOcrOnBitmap(Bitmap bmp, string language, int offsetX, int offsetY)
    {
        var ocrResult = RunOcrAsync(bmp, language);
        if (ocrResult == null) return $"ERROR: OCR engine not available for language '{language}'";

        var sb = new StringBuilder();
        sb.AppendLine($"OCR result ({ocrResult.Lines.Count} lines):");

        foreach (var line in ocrResult.Lines)
        {
            // Calculate absolute screen coordinates for click targeting
            double lx = line.Words.Min(w => w.BoundingRect.X);
            double ly = line.Words.Min(w => w.BoundingRect.Y);
            double lr = line.Words.Max(w => w.BoundingRect.X + w.BoundingRect.Width);
            double lb = line.Words.Max(w => w.BoundingRect.Y + w.BoundingRect.Height);
            int centerX = offsetX + (int)((lx + lr) / 2);
            int centerY = offsetY + (int)((ly + lb) / 2);

            sb.AppendLine($"  \"{line.Text}\" @ {centerX},{centerY}");
        }

        return sb.ToString();
    }

    private static OcrResult? RunOcrAsync(Bitmap bmp, string language)
    {
        // Create OCR engine for the requested language
        OcrEngine? engine;
        try
        {
            var lang = new Windows.Globalization.Language(language);
            engine = OcrEngine.TryCreateFromLanguage(lang);
        }
        catch
        {
            // Fallback to user profile language
            engine = OcrEngine.TryCreateFromUserProfileLanguages();
        }

        if (engine == null) return null;

        // Convert GDI+ Bitmap to WinRT SoftwareBitmap via memory stream
        using var ms = new MemoryStream();
        bmp.Save(ms, ImageFormat.Bmp);
        ms.Position = 0;

        // Use WinRT async APIs synchronously (MCP tools are synchronous)
        var decoder = Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(
            ms.AsRandomAccessStream()).AsTask().GetAwaiter().GetResult();

        var softwareBitmap = decoder.GetSoftwareBitmapAsync().AsTask().GetAwaiter().GetResult();

        // OCR requires BGRA8 or Gray8 pixel format
        if (softwareBitmap.BitmapPixelFormat != BitmapPixelFormat.Bgra8)
        {
            softwareBitmap = SoftwareBitmap.Convert(softwareBitmap, BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
        }

        var result = engine.RecognizeAsync(softwareBitmap).AsTask().GetAwaiter().GetResult();
        return result;
    }
}
