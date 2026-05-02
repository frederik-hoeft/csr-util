namespace CsrUtil.Cli;

internal sealed record CsrRequest(
    IReadOnlyList<string> SubjectAlternativeNames,
    string CommonName,
    KeyType KeyType,
    int RsaBits,
    string EcCurve,
    string OutputDirectory,
    string Prefix,
    string OpenSslPath,
    bool Overwrite,
    bool KeepConfig,
    DistinguishedNameOptions DistinguishedName,
    string? ExistingKeyPath);
