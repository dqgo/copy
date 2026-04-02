using ClipboardSync.Windows;
using Windows.ApplicationModel.DataTransfer;

namespace ClipboardSync_Windows_WinUI.Services;

internal sealed class WinUiClipboardReader : IClipboardReader
{
    public string? ReadText()
    {
        try
        {
            var data = Clipboard.GetContent();
            if (!data.Contains(StandardDataFormats.Text))
            {
                return null;
            }

            return data.GetTextAsync().AsTask().GetAwaiter().GetResult();
        }
        catch
        {
            return null;
        }
    }
}

internal sealed class WinUiClipboardWriter : IClipboardWriter
{
    public void WriteText(string text)
    {
        try
        {
            var data = new DataPackage();
            data.SetText(text);
            Clipboard.SetContent(data);
            Clipboard.Flush();
        }
        catch
        {
            // Clipboard can be temporarily unavailable.
        }
    }
}
