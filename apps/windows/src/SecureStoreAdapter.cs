namespace ClipboardSync.Windows;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

public sealed class SecureStoreAdapter : ISecureStore
{
    private readonly string _storePath;
    private readonly Dictionary<string, string> _kv;
    private readonly object _sync = new();

    public SecureStoreAdapter()
    {
        var root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipboardSync");
        Directory.CreateDirectory(root);
        _storePath = Path.Combine(root, "secure-store.json");
        _kv = Load();
    }

    public string? Get(string key)
    {
        lock (_sync)
        {
            return _kv.TryGetValue(key, out var value) ? value : null;
        }
    }

    public void Set(string key, string value)
    {
        lock (_sync)
        {
            _kv[key] = value;
            Save();
        }
    }

    public void Delete(string key)
    {
        lock (_sync)
        {
            if (_kv.Remove(key))
            {
                Save();
            }
        }
    }

    private Dictionary<string, string> Load()
    {
        if (!File.Exists(_storePath))
        {
            return new Dictionary<string, string>();
        }

        try
        {
            var encrypted = File.ReadAllBytes(_storePath);
            var raw = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            var json = Encoding.UTF8.GetString(raw);
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
        }
        catch
        {
            return new Dictionary<string, string>();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_kv);
        var raw = Encoding.UTF8.GetBytes(json);
        var encrypted = ProtectedData.Protect(raw, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(_storePath, encrypted);
    }
}
