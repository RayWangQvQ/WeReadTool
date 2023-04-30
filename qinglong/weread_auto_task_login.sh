#!/usr/bin/env bash
# new Env("weread扫码登录")
# cron 0 0 1 1 * weread_auto_task_login.sh
. weread_auto_task_base.sh

cd ./src/WeReadTool

export WeReadTool_Run=Login && \
dotnet run --configuration Release
