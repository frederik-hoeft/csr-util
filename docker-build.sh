#!/usr/bin/env bash

set -euo pipefail

# builds csr-util in a docker container and installs the built artifact to the specified output directory
# usage: ./docker-build.sh -o <output_directory>

# get -o parameter
output_dir=""
while getopts "o:" opt; do
  case $opt in
    o)
      output_dir="$OPTARG"
      ;;
    \?)
      echo "Invalid option: -$OPTARG" >&2
      exit 1
      ;;
    :)
      echo "Option -$OPTARG requires an argument." >&2
      exit 1
      ;;
  esac
done

# fall back to default output directory if not specified (./artifacts)
build_home="$(
  cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd -P
)"
if [[ -z "$output_dir" ]]; then
  output_dir="${build_home}/artifacts"
fi

DOCKER_BUILDKIT=1 docker build --target artifact --output type=local,dest="${output_dir}" "${build_home}"
