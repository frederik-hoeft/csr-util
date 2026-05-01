using System.Text;

namespace CsrUtil.Cli;

internal static class OpenSslRequestConfig
{
    public static string Build(CsrRequest request)
    {
        StringBuilder builder = new();

        builder.AppendLine("[ req ]");
        builder.AppendLine("prompt = no");
        builder.AppendLine("default_md = sha256");
        builder.AppendLine("string_mask = utf8only");
        builder.AppendLine("distinguished_name = req_distinguished_name");
        builder.AppendLine("req_extensions = req_ext");
        builder.AppendLine();

        builder.AppendLine("[ req_distinguished_name ]");
        AppendOptional(builder, "C", request.DistinguishedName.Country);
        AppendOptional(builder, "ST", request.DistinguishedName.State);
        AppendOptional(builder, "L", request.DistinguishedName.Locality);
        AppendOptional(builder, "O", request.DistinguishedName.Organization);
        AppendOptional(builder, "OU", request.DistinguishedName.OrganizationalUnit);
        builder.AppendLine($"CN = {NameParser.EscapeOpenSslConfigValue(request.CommonName)}");
        AppendOptional(builder, "emailAddress", request.DistinguishedName.EmailAddress);
        builder.AppendLine();

        builder.AppendLine("[ req_ext ]");
        builder.AppendLine("subjectAltName = @alt_names");
        builder.AppendLine();

        builder.AppendLine("[ alt_names ]");
        for (int i = 0; i < request.SubjectAlternativeNames.Count; i++)
        {
            builder.AppendLine($"DNS.{i + 1} = {NameParser.EscapeOpenSslConfigValue(request.SubjectAlternativeNames[i])}");
        }

        return builder.ToString();
    }

    private static void AppendOptional(StringBuilder builder, string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        builder.AppendLine($"{key} = {NameParser.EscapeOpenSslConfigValue(value.Trim())}");
    }
}
