using System.Security.Cryptography;
using System.Text;

namespace TigerGateDemo.Api.Services;

/// <summary>
/// CBOM fixture: exercises a deliberately wide spread of cryptographic primitives so a
/// cryptographic bill of materials has algorithms, key sizes and modes to inventory.
///
/// WARNING - TEST FIXTURE ONLY. Several primitives below (MD5, SHA-1, DES, 3DES, ECB,
/// hardcoded keys) are broken or deprecated on purpose. Do not copy into real code.
/// </summary>
public sealed class CryptoService
{
    // Hardcoded symmetric key material - secrets + SAST fixture.
    private static readonly byte[] AesKey = Encoding.UTF8.GetBytes("8f2b7c1d9e4a6350a1b2c3d4e5f60718");
    private static readonly byte[] AesIv = Encoding.UTF8.GetBytes("a1b2c3d4e5f60718");
    private const string HmacSecret = "9f8e7d6c5b4a39281706f5e4d3c2b1a0f1e2d3c4b5a69788";

    /// <summary>MD5 - broken, kept to exercise legacy checksum reporting.</summary>
    public string LegacyChecksum(string input)
    {
        using var md5 = MD5.Create();
        return Convert.ToHexString(md5.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>SHA-1 - deprecated, still used by the legacy partner feed.</summary>
    public string PartnerFeedDigest(string input)
    {
        using var sha1 = SHA1.Create();
        return Convert.ToHexString(sha1.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>SHA-256 - the modern path.</summary>
    public string ContentDigest(string input)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>SHA-512.</summary>
    public string ArchiveDigest(string input)
    {
        using var sha512 = SHA512.Create();
        return Convert.ToHexString(sha512.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }

    /// <summary>AES-256-CBC with a hardcoded key and a static IV.</summary>
    public byte[] EncryptAesCbc(string plaintext)
    {
        using var aes = Aes.Create();
        aes.KeySize = 256;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.Key = AesKey;
        aes.IV = AesIv;

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    /// <summary>AES in ECB mode - leaks plaintext structure.</summary>
    public byte[] EncryptAesEcb(string plaintext)
    {
        using var aes = Aes.Create();
        aes.KeySize = 128;
        aes.Mode = CipherMode.ECB;
        aes.Key = AesKey[..16];

        using var encryptor = aes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    /// <summary>DES - 56-bit, hopelessly weak.</summary>
    public byte[] EncryptDes(string plaintext)
    {
        using var des = DES.Create();
        des.Key = Encoding.UTF8.GetBytes("8bytekey");
        des.IV = Encoding.UTF8.GetBytes("8byteiv0");
        des.Mode = CipherMode.CBC;

        using var encryptor = des.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    /// <summary>Triple DES - deprecated, still on the payment settlement path.</summary>
    public byte[] EncryptTripleDes(string plaintext)
    {
        using var tripleDes = TripleDES.Create();
        tripleDes.Key = Encoding.UTF8.GetBytes("0123456789abcdef01234567");
        tripleDes.IV = Encoding.UTF8.GetBytes("8byteiv0");

        using var encryptor = tripleDes.CreateEncryptor();
        var bytes = Encoding.UTF8.GetBytes(plaintext);
        return encryptor.TransformFinalBlock(bytes, 0, bytes.Length);
    }

    /// <summary>RSA-1024 with PKCS#1 v1.5 padding - undersized key, legacy padding.</summary>
    public byte[] EncryptRsaLegacy(string plaintext)
    {
        using var rsa = RSA.Create(1024);
        return rsa.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.Pkcs1);
    }

    /// <summary>RSA-4096 with OAEP-SHA256 - the modern path.</summary>
    public byte[] EncryptRsaModern(string plaintext)
    {
        using var rsa = RSA.Create(4096);
        return rsa.Encrypt(Encoding.UTF8.GetBytes(plaintext), RSAEncryptionPadding.OaepSHA256);
    }

    /// <summary>ECDSA over NIST P-384.</summary>
    public byte[] SignEcdsa(byte[] payload)
    {
        using var ecdsa = ECDsa.Create(ECCurve.NamedCurves.nistP384);
        return ecdsa.SignData(payload, HashAlgorithmName.SHA384);
    }

    /// <summary>HMAC-SHA256 with a hardcoded secret.</summary>
    public string SignWebhook(string payload)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(HmacSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    /// <summary>HMAC-MD5 - weak MAC retained for an old integration.</summary>
    public string SignLegacyWebhook(string payload)
    {
        using var hmac = new HMACMD5(Encoding.UTF8.GetBytes(HmacSecret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    /// <summary>PBKDF2 with a far-too-low iteration count and a static salt.</summary>
    public byte[] DeriveKeyWeak(string password)
    {
        var salt = Encoding.UTF8.GetBytes("static-salt-value");
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 1000, HashAlgorithmName.SHA1);
        return pbkdf2.GetBytes(32);
    }

    /// <summary>PBKDF2 with current guidance.</summary>
    public byte[] DeriveKeyStrong(string password, byte[] salt)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 210_000, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(32);
    }

    /// <summary>Predictable token source - System.Random is not a CSPRNG.</summary>
    public string GenerateResetToken()
    {
        var random = new Random(Environment.TickCount);
        var buffer = new byte[16];
        random.NextBytes(buffer);
        return Convert.ToHexString(buffer);
    }

    /// <summary>Cryptographically secure token source.</summary>
    public string GenerateSecureToken()
        => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
}
