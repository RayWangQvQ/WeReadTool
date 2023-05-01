#!/usr/bin/env bash
# new Env("weread_auto_task_base")
# cron 0 0 1 1 * weread_auto_task_base.sh

dir_shell=${QL_DIR-'/ql'}/shell
. $dir_shell/share.sh
. /root/.bashrc

## 安装dotnet（如果未安装过）
dotnetVersion=$(dotnet --version)
if [[ $dotnetVersion == 6.* ]]; then
    echo "已安装dotnet，当前版本：$dotnetVersion"
else
    echo "which dotnet: $(which dotnet)"
    echo "开始安装dotnet"
    rayInstallShell="https://ghproxy.com/https://raw.githubusercontent.com/RayWangQvQ/autoautoToolPro/main/qinglong/ray-dotnet-install.sh"
    {
        echo "------尝试使用apt安装------"
        wget https://packages.microsoft.com/config/ubuntu/20.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
        sudo dpkg -i packages-microsoft-prod.deb
        rm packages-microsoft-prod.deb
        apt-get update && apt-get install -y dotnet-sdk-6.0
        dotnet --version && echo "安装成功"
    } || {
        echo "------尝试使用apk安装------"
        apk add dotnet6-sdk
        dotnet --version && echo "安装成功"
    } || {
        echo "------再尝试使用官方脚本安装------"
        curl -sSL $rayInstallShell | bash /dev/stdin
        . /root/.bashrc
        dotnet --version && echo "安装成功"
    } || {
        echo "------再尝试使用二进制包安装------"
        curl -sSL $rayInstallShell | bash /dev/stdin --no-official
        . /root/.bashrc
        dotnet --version && echo "安装成功"
    } || {
        echo "安装失败，没办法了，毁灭吧，自己解决吧：https://learn.microsoft.com/zh-cn/dotnet/core/install/linux-alpine"
        exit 1
    }
fi

auto_repo="raywangqvq_wereadtool"

echo -e "\nrepo目录: $dir_repo"
auto_repo_dir="$(find $dir_repo -type d -iname $auto_repo | head -1)"
echo -e "auto仓库目录: $auto_repo_dir\n"

cd $auto_repo_dir
export WeReadTool_Platform=QingLong
export DOTNET_ENVIRONMENT=Production
