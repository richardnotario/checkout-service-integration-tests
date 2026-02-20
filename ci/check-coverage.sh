#!/usr/bin/env bash
set -euo pipefail

THRESHOLD="${1:-0.70}"

COV_FILE="$(find . -type f -name "coverage.cobertura.xml" -print0 2>/dev/null | xargs -0 ls -t 2>/dev/null | head -n 1 || true)"
if [[ -z "${COV_FILE}" ]]; then
  echo "ERROR: Could not find coverage.cobertura.xml. Did you run dotnet test with --collect:\"XPlat Code Coverage\"?"
  exit 1
fi

echo "Coverage file: ${COV_FILE}"
echo "Threshold: ${THRESHOLD}"

LINE_RATE="$(python3 - <<PY
import xml.etree.ElementTree as ET
root = ET.parse("${COV_FILE}").getroot()
print(root.attrib.get("line-rate", "0"))
PY
)"

echo "Cobertura line-rate: ${LINE_RATE}"

python3 - <<PY
line_rate = float("${LINE_RATE}")
threshold = float("${THRESHOLD}")
if line_rate < threshold:
    raise SystemExit(f"ERROR: Coverage gate failed: {line_rate:.4f} < {threshold:.2f}")
print(f"OK: Coverage gate passed: {line_rate:.4f} >= {threshold:.2f}")
PY
