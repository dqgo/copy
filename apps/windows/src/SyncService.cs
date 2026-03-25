namespace ClipboardSync.Windows;

public interface IClipboardReader
{
    string? ReadText();
}

public interface IClipboardWriter
{
    void WriteText(string text);
}

public interface ISecureStore
{
    string? Get(string key);
    void Set(string key, string value);
    void Delete(string key);
}

public sealed class SyncService
{
    private readonly IClipboardReader _reader;
    private readonly IClipboardWriter _writer;
    private readonly ISecureStore _store;

    public SyncService(IClipboardReader reader, IClipboardWriter writer, ISecureStore store)
    {
        _reader = reader;
        _writer = writer;
        _store = store;
    }

    public string? CaptureClipboard()
    {
        return _reader.ReadText();
    }

    public void ApplyRemoteText(string text)
    {
        _writer.WriteText(text);
    }

    public void SaveWorkspaceKey(string key)
    {
        _store.Set("workspace_key", key);
    }

    public string? LoadWorkspaceKey()
    {
        return _store.Get("workspace_key");
    }

    public void ClearWorkspaceKey()
    {
        _store.Delete("workspace_key");
    }
}
