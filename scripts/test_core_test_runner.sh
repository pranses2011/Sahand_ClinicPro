#!/usr/bin/env bash
# Regression: a failed dotnet suite must make the tee pipeline fail, while
# subsequent suites still run. No real SDK or database is needed here.
set -euo pipefail
root=$(cd "$(dirname "$0")/.." && pwd)
tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT
cat > "$tmp/dotnet" <<'MOCK'
#!/usr/bin/env bash
echo "$2" >> "$TRACE_FILE"
if [[ "$2" == *Application.UnitTests* && "${FAIL_APPLICATION:-0}" == 1 ]]; then exit 1; fi
exit 0
MOCK
chmod +x "$tmp/dotnet"
export PATH="$tmp:$PATH" TRACE_FILE="$tmp/calls"
if FAIL_APPLICATION=1 bash "$root/scripts/run_core_tests.sh" | tee "$tmp/log" >/dev/null; then
  echo 'Runner incorrectly reported success after a failed suite.' >&2
  exit 1
fi
[[ $(wc -l < "$TRACE_FILE") -eq 4 ]] || { echo 'Runner did not execute all four suites.' >&2; exit 1; }
: > "$TRACE_FILE"
FAIL_APPLICATION=0 bash "$root/scripts/run_core_tests.sh" | tee "$tmp/log" >/dev/null
[[ $(wc -l < "$TRACE_FILE") -eq 4 ]] || { echo 'Successful run did not execute all suites.' >&2; exit 1; }
echo 'Core test runner correctly reports both failure and success.'
