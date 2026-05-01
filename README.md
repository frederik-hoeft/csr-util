# csr-util

Small .NET 10 CLI app that uses ConsoleAppFramework and wraps `openssl` to generate private keys and PKCS#10 CSRs for local services.

## Features

- SDK-style .NET 10 project with Native AOT publishing enabled.
- `csr-util.slnx` solution file.
- EC keys by default (`prime256v1`), RSA keys on request.
- Multiple DNS subject names are written into the CSR `subjectAltName` extension.
- Output is standard PEM and can be imported into EasyRSA.

## Usage

```bash
# EC key, default curve prime256v1
csr-util generate app.local api.app.local
```

```bash
# RSA key
csr-util generate --key-type rsa --rsa-bits 3072 app.local api.app.local
```

```bash
# Custom output directory and file prefix
csr-util generate --output-directory ./certs --prefix app-dev app.local app.internal.local
```

Comma-separated names are also accepted:

```bash
csr-util generate app.local,api.app.local
```

Outputs:

- `<prefix>.key.pem`
- `<prefix>.csr.pem`

`<prefix>` defaults to the first subject name with invalid filename characters replaced. Existing output files are not overwritten unless `--overwrite` is passed.

## EasyRSA

Import the generated CSR with EasyRSA:

```bash
easyrsa import-req ./certs/app-dev.csr.pem app-dev
easyrsa sign-req server app-dev
```

## Build and publish

```bash
dotnet build source/csr-util.slnx
dotnet publish source/CsrUtil/CsrUtil.Cli/CsrUtil.Cli.csproj -c Release -r linux-x64
```

OpenSSL must be available on `PATH`, or pass `--openssl-path /path/to/openssl`.
