#!/usr/bin/env bash
# Run all non-WPF suites even if one fails, then preserve the overall status.
set -u
status=0
for project in \
  tests/Sahand.ClinicPro.Domain.UnitTests/Sahand.ClinicPro.Domain.UnitTests.csproj \
  tests/Sahand.ClinicPro.Application.UnitTests/Sahand.ClinicPro.Application.UnitTests.csproj \
  tests/Sahand.ClinicPro.Infrastructure.IntegrationTests/Sahand.ClinicPro.Infrastructure.IntegrationTests.csproj \
  tests/Sahand.ClinicPro.API.IntegrationTests/Sahand.ClinicPro.API.IntegrationTests.csproj; do
  echo "::group::$(basename "$project")"
  dotnet test "$project" --no-build -c Release \
    --logger "trx" --collect:"XPlat Code Coverage" --results-directory ./TestResults || status=1
  echo "::endgroup::"
done
exit "$status"
