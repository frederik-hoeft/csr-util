FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Install Native AOT prerequisites
RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        clang \
        zlib1g-dev \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Restore dependencies as a separate layer for better caching
COPY source/CsrUtil/CsrUtil.Cli/CsrUtil.Cli.csproj CsrUtil/CsrUtil.Cli/
COPY source/Directory.Build.props .

RUN dotnet restore CsrUtil/CsrUtil.Cli/CsrUtil.Cli.csproj -r linux-x64

# Copy remaining source and publish
COPY source/ .

RUN dotnet publish CsrUtil/CsrUtil.Cli/CsrUtil.Cli.csproj \
    -c Release \
    -r linux-x64 \
    --self-contained true \
    -o /publish

FROM scratch AS artifact
# Rename CsrUtil.Cli (the default AOT binary name) to csr-util for usability
COPY --from=build /publish/CsrUtil.Cli /csr-util
