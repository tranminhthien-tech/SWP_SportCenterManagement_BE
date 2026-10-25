/* CODE CŨ B41: giữ nguyên triển khai cũ để đối chiếu, không thực thi.
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SWP_SportCenter.Service.Until;

public static class Argon2Hasher
{
    // Cấu hình tham số độ khó (Có thể tinh chỉnh tùy cấu hình Server của bạn)
    private const int DegreeOfParallelism = 8; // Số luồng CPU sử dụng
    private const int MemorySize = 1024 * 128; // RAM sử dụng (128 MB)
    private const int Iterations = 4; // Số vòng lặp

    public static string HashPassword(string password)
    {
        // 1. Tạo chuỗi Salt ngẫu nhiên (16 byte)
        byte[] salt = new byte[16];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(salt);
        }

        // 2. Tiến hành băm mật khẩu bằng thuật toán Argon2id
        using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
        {
            argon2.Salt = salt;
            argon2.DegreeOfParallelism = DegreeOfParallelism;
            argon2.Iterations = Iterations;
            argon2.MemorySize = MemorySize;

            byte[] hash = argon2.GetBytes(16);

            // 3. Nối Salt và Hash lại bằng dấu ":" để lưu vào 1 cột duy nhất
            // Format: chuỗi_Base64(Salt) : chuỗi_Base64(Hash)
            return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
        }
    }

    public static bool VerifyHash(string password, string hashedPasswordFromDb)
    {
        try
        {
            // 1. Tách Salt và Hash từ chuỗi lưu trong Database
            var parts = hashedPasswordFromDb.Split(':');
            if (parts.Length != 2) return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);

            // 2. Băm mật khẩu người dùng vừa nhập với Salt cũ lấy từ DB
            using (var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password)))
            {
                argon2.Salt = salt;
                argon2.DegreeOfParallelism = DegreeOfParallelism;
                argon2.Iterations = Iterations;
                argon2.MemorySize = MemorySize;

                byte[] actualHash = argon2.GetBytes(16);

                // 3. So sánh 2 chuỗi Hash một cách an toàn (chống Timing Attacks)
                return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
            }
        }
        catch
        {
            return false; // Nếu chuỗi sai format hoặc có lỗi thì trả về false
        }
    }
}
*/
using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace SWP_SportCenter.Service.Until;

// FIX B41: Argon2id theo baseline OWASP, format tự mô tả tham số.
// Legacy salt:hash vẫn verify bằng 128 MiB / 4 vòng / 8 luồng.
public static class Argon2Hasher
{
    private const int Memory = 19456, Iterations = 2, Parallelism = 1;
    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var hash = Derive(password, salt, Memory, Iterations, Parallelism, 32);
        return $"$argon2id$v=19$m={Memory},t={Iterations},p={Parallelism}$" +
            Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
    }
    public static bool NeedsRehash(string stored) => !stored.StartsWith(
        "$argon2id$v=19$m=19456,t=2,p=1$", StringComparison.Ordinal);

    public static bool VerifyHash(string password, string stored)
    {
        try
        {
            int memory, iterations, parallelism; byte[] salt, expected;
            if (stored.StartsWith("$argon2id$", StringComparison.Ordinal))
            {
                var parts = stored.Split('$');
                if (parts.Length != 6 || parts[2] != "v=19") return false;
                var parameters = parts[3].Split(',');
                if (parameters.Length != 3 || !parameters[0].StartsWith("m=") ||
                    !parameters[1].StartsWith("t=") || !parameters[2].StartsWith("p=")) return false;
                memory = int.Parse(parameters[0][2..]); iterations = int.Parse(parameters[1][2..]);
                parallelism = int.Parse(parameters[2][2..]);
                // Không cho hash malformed yêu cầu tài nguyên vô hạn.
                if (memory < 8192 || memory > 131072 || iterations < 1 || iterations > 4 ||
                    parallelism < 1 || parallelism > 8) return false;
                salt = Convert.FromBase64String(parts[4]); expected = Convert.FromBase64String(parts[5]);
            }
            else
            {
                var parts = stored.Split(':');
                if (parts.Length != 2) return false;
                salt = Convert.FromBase64String(parts[0]); expected = Convert.FromBase64String(parts[1]);
                memory = 131072; iterations = 4; parallelism = 8;
                if (expected.Length != 16) return false;
            }
            if (salt.Length != 16 || expected.Length is not (16 or 32)) return false;
            return CryptographicOperations.FixedTimeEquals(expected,
                Derive(password, salt, memory, iterations, parallelism, expected.Length));
        }
        catch (FormatException) { return false; }
        catch (OverflowException) { return false; }
        catch (ArgumentException) { return false; }
    }
    private static byte[] Derive(string password, byte[] salt, int memory, int iterations, int parallelism, int length)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password)) {
            Salt = salt, MemorySize = memory, Iterations = iterations, DegreeOfParallelism = parallelism
        };
        return argon.GetBytes(length);
    }
}
