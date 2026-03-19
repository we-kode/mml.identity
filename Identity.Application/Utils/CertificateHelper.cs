using Identity.Application.Types;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Identity.Application.Utils;

/// <summary>
/// Util class to generate certificates.
/// </summary>
public static class CertificateHelper
{
  /// <summary>
  /// Checks if key exists. If key does not exists create new one.
  /// </summary>
  /// <param name="type"></param>
  public static void Ensure(KeyType type)
  {
    if (!File.Exists(GetFilePath(type)))
    {
      Generate(type, validDays: 18262);
    }

    if (!File.Exists(GetFilePath(type)))
    {
      Generate(type, validDays: 18262);
    }
  }

  /// <summary>
  /// Loads the certificate of the given type.
  /// </summary>
  /// <param name="type">The type of key to load.</param>
  /// <returns><see cref="X509Certificate2"/></returns>
  public static X509Certificate2 Load(KeyType type)
  {
    return X509CertificateLoader.LoadPkcs12(File.ReadAllBytes(GetFilePath(type)), GetPassword(type));
  }

  /// <summary>
  /// Generates a <see cref="X509Certificate2"/>.
  /// </summary>
  /// <param name="keyType">The type of the key..</param>
  /// <param name="validDays">Days the certificate stay valid. Deafult is 90 days.</param>
  /// <param name="keySize">The key size in bits. Defaults to 4096 bits.</param>
  /// <returns><see cref="X509Certificate2"/></returns>
  private static void Generate(KeyType keyType, int validDays = 90, int keySize = 4096)
  {
    using var algorithm = RSA.Create(keySizeInBits: keySize);
    var subject = new X500DistinguishedName($"CN={GetCN(keyType)}");
    var request = new CertificateRequest(subject, algorithm, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
    request.CertificateExtensions.Add(new X509KeyUsageExtension(GetKeyUsage(keyType), critical: true));
    var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(validDays));
    File.WriteAllBytes(GetFilePath(keyType), certificate.Export(X509ContentType.Pfx, GetPassword(keyType)));
  }

  private static X509KeyUsageFlags GetKeyUsage(KeyType type)
  {
    return type switch
    {
      KeyType.Signing => X509KeyUsageFlags.DigitalSignature,
      KeyType.Encryption => X509KeyUsageFlags.KeyEncipherment,
      _ => X509KeyUsageFlags.None,
    };
  }

  private static string? GetPassword(KeyType type)
  {
    return type switch
    {
      KeyType.Encryption => Environment.GetEnvironmentVariable("ENCRYPTION_SECRET"),
      KeyType.Signing => Environment.GetEnvironmentVariable("SIGN_SECRET"),
      _ => null,
    };
  }

  private static string GetCN(KeyType type)
  {
    return type switch
    {
      KeyType.Encryption => $"{IdentityConstants.Scopes.IdentityService}/{IdentityConstants.Env.INSTANCE} Encryption",
      KeyType.Signing => $"{IdentityConstants.Scopes.IdentityService}/{IdentityConstants.Env.INSTANCE} Signing",
      _ => string.Empty,
    };
  }

  private static string GetFilePath(KeyType type)
  {
    return type switch
    {
      KeyType.Encryption => "/secrets/wekode.mml.encryption.pfx",
      KeyType.Signing => "/secrets/wekode.mml.signing.pfx",
      _ => string.Empty,
    };
  }
}
