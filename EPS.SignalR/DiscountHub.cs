using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace EPS.SignalR;

public class DiscountHub : Hub
{
    private readonly DiscountOptions _options;
    private static readonly ReaderWriterLockSlim _lock = new();
    private static HashSet<string> _codes = new();
    private readonly string _storagePath;

    public DiscountHub(IOptions<DiscountOptions> options)
    {
        _options = options.Value;
        _storagePath = _options.StoragePath;
        LoadCodes();
    }

    public Task<bool> GenerateCodes(ushort count, byte length)
    {
        Console.WriteLine($"Generating {count} codes...");
        
        if (count > _options.MaxCodesPerRequest) 
            return Task.FromResult(false);
        
        if (length < _options.MinCodeLength || length > _options.MaxCodeLength)
            return Task.FromResult(false);
        
        var newCodes = new HashSet<string>(); //to ensure it will not be repeated, not need check with contains...

        _lock.EnterWriteLock();
        try
        {
            while (newCodes.Count < count)
            {
                var code = GenerateCode(length);
                if (_codes.Add(code))
                {
                    newCodes.Add(code);
                }
            }

            SaveCodes();
        }
        finally
        {
            _lock.ExitWriteLock();
        }

        Console.WriteLine("finish generating code");
        return Task.FromResult(true);
    }

    public Task<byte> UseCode(string code)
    {
        _lock.EnterWriteLock();
        try
        {
            if (_codes.Contains(code))
            {
                _codes.Remove(code);
                SaveCodes();
                return Task.FromResult((byte)1); 
            }
            else
            {
                return Task.FromResult((byte)0); 
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }
    
    public Task<int> GetTotalCodes()
    {
        try
        {
            var desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var filePath = Path.Combine(desktopPath, "discountCodes.json");

            var json = File.ReadAllText(filePath);
            var codes = JsonSerializer.Deserialize<List<string>>(json);
            int count = codes?.Count ?? 0;

            Console.WriteLine($"Found {count} codes in discountCodes.json");
            return Task.FromResult(count);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading JSON file: {ex.Message}");
            return Task.FromResult(0);
        }
    }

    public Task<bool> CheckIfCodeExists(string code)
    {
        _lock.EnterReadLock();
        try
        {
            bool exists = _codes.Contains(code);
            return Task.FromResult(exists);
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
    
    private string GenerateCode(byte length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];

        using (var rng = RandomNumberGenerator.Create())
        {
            byte[] data = new byte[length];
            rng.GetBytes(data);

            for (int i = 0; i < length; i++)
            {
                result[i] = chars[data[i] % chars.Length];
            }
        }

        return new string(result);
    }

    private void SaveCodes()
    {
        try
        {
            var json = JsonSerializer.Serialize(_codes);
            File.WriteAllText(_storagePath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in save code: {ex.Message}");
        }
    }

    private void LoadCodes()
    {
        _lock.EnterReadLock();
        try
        {
            if (File.Exists(_storagePath))
            {
                var json = File.ReadAllText(_storagePath);
                _codes = JsonSerializer.Deserialize<HashSet<string>>(json) ?? new HashSet<string>();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in trying to load Codes: {ex.Message}");
            _codes = new HashSet<string>();
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }
}