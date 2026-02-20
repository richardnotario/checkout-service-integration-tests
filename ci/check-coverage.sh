#!/usr/bin/env bash
set -euo pipefail

THRESHOLD="${1:-0.70}"

# Restrict search to the intended test results location to avoid picking up
# unrelated cobertura files from other projects or prior runs.
SEARCH_ROOT="./CheckoutService.IntegrationTests/TestResults"

COV_FILE="$(
  find "${SEARCH_ROOT}" -type f -name "coverage.cobertura.xml" -print0 2>/dev/null \
    | xargs -0 ls -t 2>/dev/null \
    | head -n 1 \
    || true
)"

if [[ -z "${COV_FILE}" ]]; then
  echo "ERROR: Could not find coverage.cobertura.xml under ${SEARCH_ROOT}."
  echo "Hint: Ensure dotnet test is run with:"
  echo "  --collect:\"XPlat Code Coverage\" --results-directory ${SEARCH_ROOT}"
  echo ""
  echo "Files under ${SEARCH_ROOT}:"
  (ls -R "${SEARCH_ROOT}" 2>/dev/null || true)
  exit 1
fi

echo "Coverage file: ${COV_FILE}"
echo "Threshold: ${THRESHOLD}"

# Parse Cobertura root attributes
LINE_RATE="$(
  python3 - <<PY
import xml.etree.ElementTree as ET
root = ET.parse("${COV_FILE}").getroot()
print(root.attrib.get("line-rate", "0"))
PY
)"

LINES_VALID="$(
  python3 - <<PY
import xml.etree.ElementTree as ET
root = ET.parse("${COV_FILE}").getroot()
print(root.attrib.get("lines-valid", "0"))
PY
)"

LINES_COVERED="$(
  python3 - <<PY
import xml.etree.ElementTree as ET
root = ET.parse("${COV_FILE}").getroot()
print(root.attrib.get("lines-covered", "0"))
PY
)"

echo "Cobertura line-rate: ${LINE_RATE}"
echo "Cobertura lines-covered/lines-valid: ${LINES_COVERED}/${LINES_VALID}"

python3 - <<PY
line_rate = float("${LINE_RATE}")
threshold = float("${THRESHOLD}")
if line_rate + 1e-12 < threshold:
    raise SystemExit(f"ERROR: Coverage gate failed: {line_rate:.4f} < {threshold:.2f}")
print(f"OK: Coverage gate passed: {line_rate:.4f} >= {threshold:.2f}")
PY