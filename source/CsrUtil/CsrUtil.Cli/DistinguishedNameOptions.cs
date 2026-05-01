namespace CsrUtil.Cli;

internal sealed record DistinguishedNameOptions(
    string? Country,
    string? State,
    string? Locality,
    string? Organization,
    string? OrganizationalUnit,
    string? EmailAddress);
