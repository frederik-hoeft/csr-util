using ConsoleAppFramework;

namespace CsrUtil.Cli;

public sealed class CsrCommands
{
    private const string EXISTING_KEY_LABEL = " (existing)";
    private const int DEFAULT_RSA_BITS = 2048;
    private const int MINIMUM_RSA_BITS = 2048;
    private const string DEFAULT_EC_CURVE = "prime256v1";
    private const string DEFAULT_OPENSSL_PATH = "openssl";
    private const string DEFAULT_OUTPUT_DIRECTORY = ".";

    /// <summary>
    /// Generate an OpenSSL private key and PKCS#10 CSR with DNS subjectAltName entries.
    /// </summary>
    /// <param name="keyType">-k, Key algorithm to generate: ec or rsa. Ignored when --existing-key is provided.</param>
    /// <param name="rsaBits">RSA key size. Used only when --key-type rsa is selected. Ignored when --existing-key is provided.</param>
    /// <param name="ecCurve">OpenSSL EC curve name. Used only when --key-type ec is selected. Ignored when --existing-key is provided.</param>
    /// <param name="commonName">CSR subject common name. Defaults to the first subject name.</param>
    /// <param name="outputDirectory">-o, Directory for generated files.</param>
    /// <param name="prefix">-p, Output file prefix. Defaults to the first subject name with invalid filename characters replaced.</param>
    /// <param name="opensslPath">Path to the openssl executable.</param>
    /// <param name="overwrite">Replace existing output files. When generating a new key, applies to both the private key and CSR files; when --existing-key is provided, applies only to the CSR output file.</param>
    /// <param name="keepConfig">Keep the generated OpenSSL request config next to the output files.</param>
    /// <param name="existingKey">-e, Path to an existing private key file to use instead of generating a new key.</param>
    /// <param name="country">Optional subject country code.</param>
    /// <param name="state">Optional subject state or province.</param>
    /// <param name="locality">Optional subject locality.</param>
    /// <param name="organization">Optional subject organization.</param>
    /// <param name="organizationalUnit">Optional subject organizational unit.</param>
    /// <param name="emailAddress">Optional subject email address.</param>
    /// <param name="cancellationToken">Cancellation token provided by ConsoleAppFramework.</param>
    /// <param name="subjectNames">DNS names to put into the CSR SAN extension. Comma-separated values are also accepted.</param>
    [Command("generate")]
    public async Task<int> GenerateAsync(
        KeyType keyType = KeyType.Ec,
        int rsaBits = DEFAULT_RSA_BITS,
        string ecCurve = DEFAULT_EC_CURVE,
        string? commonName = null,
        string outputDirectory = DEFAULT_OUTPUT_DIRECTORY,
        string? prefix = null,
        string opensslPath = DEFAULT_OPENSSL_PATH,
        bool overwrite = false,
        bool keepConfig = false,
        string? existingKey = null,
        string? country = null,
        string? state = null,
        string? locality = null,
        string? organization = null,
        string? organizationalUnit = null,
        string? emailAddress = null,
        CancellationToken cancellationToken = default,
        [Argument] params string[] subjectNames)
    {
        ArgumentNullException.ThrowIfNull(subjectNames);
        try
        {
            List<string> names = NameParser.NormalizeSubjectNames(subjectNames);
            if (names.Count == 0)
            {
                await Console.Error.WriteLineAsync("Specify at least one DNS subject name, for example: csr-util generate app.local api.app.local");
                return 1;
            }

            string? resolvedExistingKey = null;
            if (existingKey is not null)
            {
                resolvedExistingKey = Path.GetFullPath(existingKey);
                if (!File.Exists(resolvedExistingKey))
                {
                    await Console.Error.WriteLineAsync($"Existing key file not found: {resolvedExistingKey}");
                    return 1;
                }
            }

            if (keyType == KeyType.Rsa && rsaBits < MINIMUM_RSA_BITS)
            {
                await Console.Error.WriteLineAsync($"RSA keys must be at least {MINIMUM_RSA_BITS} bits. Pass --rsa-bits {MINIMUM_RSA_BITS} or higher.");
                return 1;
            }

            string outputPath = Path.GetFullPath(outputDirectory);
            string finalCommonName = string.IsNullOrWhiteSpace(commonName) ? names[0] : commonName.Trim();
            NameParser.ThrowIfUnsafeConfigValue(finalCommonName, nameof(commonName));

            string finalPrefix = string.IsNullOrWhiteSpace(prefix)
                ? NameParser.SanitizeFileName(names[0])
                : NameParser.SanitizeFileName(prefix.Trim());

            if (string.IsNullOrWhiteSpace(finalPrefix))
            {
                await Console.Error.WriteLineAsync("Unable to derive an output file prefix. Pass --prefix explicitly.");
                return 1;
            }

            CsrRequest request = new(
                SubjectAlternativeNames: names,
                CommonName: finalCommonName,
                KeyType: keyType,
                RsaBits: rsaBits,
                EcCurve: ecCurve,
                OutputDirectory: outputPath,
                Prefix: finalPrefix,
                OpenSslPath: opensslPath,
                Overwrite: overwrite,
                KeepConfig: keepConfig,
                DistinguishedName: new DistinguishedNameOptions(
                    Country: country,
                    State: state,
                    Locality: locality,
                    Organization: organization,
                    OrganizationalUnit: organizationalUnit,
                    EmailAddress: emailAddress),
                ExistingKeyPath: resolvedExistingKey);

            CsrGenerator generator = new();
            CsrGenerationResult result = await generator.GenerateAsync(request, cancellationToken);

            Console.WriteLine("CSR workflow completed.");
            string keyStatus = result.GeneratedPrivateKey ? string.Empty : EXISTING_KEY_LABEL;
            Console.WriteLine($"Private key: {result.PrivateKeyPath}{keyStatus}");

            Console.WriteLine($"CSR:         {result.CsrPath}");
            if (result.OpenSslConfigPath is not null)
            {
                Console.WriteLine($"Config:      {result.OpenSslConfigPath}");
            }

            Console.WriteLine($"SANs:        {string.Join(", ", result.SubjectAlternativeNames)}");
            Console.WriteLine("EasyRSA:     easyrsa import-req <csr-path> <short-name>");
            return 0;
        }
        catch (OperationCanceledException)
        {
            await Console.Error.WriteLineAsync("Cancelled.");
            return 130;
        }
        catch (Exception ex)
        {
            await Console.Error.WriteLineAsync(ex.Message);
            return 1;
        }
    }
}
