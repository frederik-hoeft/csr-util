namespace CsrUtil.Cli;

internal sealed record CsrGenerationResult(
    string PrivateKeyPath,
    string CsrPath,
    string? OpenSslConfigPath,
    IReadOnlyList<string> SubjectAlternativeNames);
