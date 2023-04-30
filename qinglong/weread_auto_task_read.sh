#!/usr/bin/env bash
# new Env("weread每日阅读任务")
# cron 0 7,20,21 * * * weread_auto_task_read.sh
. weread_auto_task_base.sh

cd ./src/WeReadTool

export WeReadTool_Run=Read && \
dotnet run --configuration Release
