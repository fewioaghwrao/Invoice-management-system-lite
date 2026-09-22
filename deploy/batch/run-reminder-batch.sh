#!/usr/bin/env bash
set -Eeuo pipefail

readonly APP_DIR="/home/deploy/apps/Invoice-management-system-lite-batch"
readonly ENV_FILE="${APP_DIR}/.env.batch.prod"
readonly NETWORK="invoice-management-system-lite_invoice-network"
readonly IMAGE="invoice-system-batch:prod"

EXTERNAL_JOB_ID="REMINDER-SYSTEMD-$(date -u +'%Y%m%dT%H%M%SZ')-$$"

cd "${APP_DIR}"

echo "Starting reminder batch. ExternalJobId=${EXTERNAL_JOB_ID}"

if /usr/bin/docker run --rm \
  --network "${NETWORK}" \
  --env-file "${ENV_FILE}" \
  "${IMAGE}" \
  reminder \
  --job-id "${EXTERNAL_JOB_ID}"
then
  EXIT_CODE=0
else
  EXIT_CODE=$?
fi

echo "Reminder batch process exited. ExternalJobId=${EXTERNAL_JOB_ID}, ExitCode=${EXIT_CODE}"

exit "${EXIT_CODE}"