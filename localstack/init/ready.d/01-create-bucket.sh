#!/usr/bin/env bash
set -euo pipefail

BUCKET_NAME="${BUCKET_NAME:-todo-api-attachments}"

if command -v awslocal >/dev/null 2>&1; then
  awslocal s3 mb "s3://${BUCKET_NAME}" >/dev/null 2>&1 || true
else
  aws --endpoint-url="http://localhost:4566" s3 mb "s3://${BUCKET_NAME}" >/dev/null 2>&1 || true
fi
