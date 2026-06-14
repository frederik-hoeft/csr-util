using System.Text;

namespace CsrUtil.Cli;

internal sealed class CsrGenerator
{
    public async Task<CsrGenerationResult> GenerateAsync(CsrRequest request, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(request.OutputDirectory);

        bool usingExistingKey = request.ExistingKeyPath is not null;
        string privateKeyPath = usingExistingKey
            ? request.ExistingKeyPath!
            : Path.Combine(request.OutputDirectory, $"{request.Prefix}.key.pem");
        string csrPath = Path.Combine(request.OutputDirectory, $"{request.Prefix}.csr.pem");
        string configPath = Path.Combine(request.OutputDirectory, $".{request.Prefix}.{Guid.NewGuid():N}.openssl.cnf");

        if (!usingExistingKey)
        {
            if (File.Exists(privateKeyPath))
            {
                throw new IOException($"Private key already exists: {privateKeyPath}. Delete or move the existing key file before generating a new one.");
            }
        }

        EnsureWritable(csrPath, request.Overwrite);

        await EnsureOpenSslAvailableAsync(request.OpenSslPath, cancellationToken);

        if (!usingExistingKey)
        {
            await GeneratePrivateKeyAsync(
                request.OpenSslPath,
                request.KeyType,
                request.RsaBits,
                request.EcCurve,
                privateKeyPath,
                cancellationToken);
        }

        await File.WriteAllTextAsync(
            configPath,
            OpenSslRequestConfig.Build(request),
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: false),
            cancellationToken);

        try
        {
            await OpenSslRunner.RunAsync(
                request.OpenSslPath,
                ["req", "-new", "-sha256", "-key", privateKeyPath, "-out", csrPath, "-config", configPath],
                cancellationToken);
        }
        finally
        {
            if (!request.KeepConfig)
            {
                TryDelete(configPath);
            }
        }

        return new CsrGenerationResult(
            PrivateKeyPath: privateKeyPath,
            CsrPath: csrPath,
            OpenSslConfigPath: request.KeepConfig ? configPath : null,
            SubjectAlternativeNames: request.SubjectAlternativeNames,
            GeneratedPrivateKey: !usingExistingKey);
    }

    private static async Task EnsureOpenSslAvailableAsync(string opensslPath, CancellationToken cancellationToken)
    {
        try
        {
            await OpenSslRunner.RunAsync(opensslPath, ["version"], cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException($"OpenSSL is required but could not be executed from '{opensslPath}': {ex.Message}", ex);
        }
    }

    private static Task GeneratePrivateKeyAsync(
        string opensslPath,
        KeyType keyType,
        int rsaBits,
        string ecCurve,
        string privateKeyPath,
        CancellationToken cancellationToken)
    {
        return keyType switch
        {
            KeyType.Ec => OpenSslRunner.RunAsync(
                opensslPath,
                ["genpkey", "-algorithm", "EC", "-pkeyopt", $"ec_paramgen_curve:{ecCurve}", "-out", privateKeyPath],
                cancellationToken),
            KeyType.Rsa => OpenSslRunner.RunAsync(
                opensslPath,
                ["genpkey", "-algorithm", "RSA", "-pkeyopt", $"rsa_keygen_bits:{rsaBits}", "-out", privateKeyPath],
                cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(keyType), keyType, "Unsupported key type.")
        };
    }

    private static void EnsureWritable(string path, bool overwrite)
    {
        if (!overwrite && File.Exists(path))
        {
            throw new IOException($"File already exists: {path}. Pass --overwrite to replace it.");
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch
        {
            // Best effort cleanup. The generated CSR/key are still usable if temp config deletion fails.
        }
    }
}
