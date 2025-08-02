#!/bin/sh
set -e

if [ -f "package.json" ]; then
    npm install
    npm run watch &
fi

dotnet watch run --urls "http://0.0.0.0:80" --no-launch-profile
