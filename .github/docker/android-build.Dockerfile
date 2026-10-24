# Android 预构建构建环境（仅用于 mobile-android-container 手动 spike）
# 目标：把 .NET SDK、Android workload、Android SDK/NDK、PowerShell 固化进镜像，
# 测试“拉镜像 + 构建”是否比每次在 ubuntu-latest 上安装更快。
FROM mcr.microsoft.com/dotnet/sdk:10.0.402

ARG ANDROID_CMDLINE_TOOLS=16111833
ARG ANDROID_API=36
ARG ANDROID_BUILD_TOOLS=36.1.0
ARG ANDROID_NDK=27.3.13750724
ARG PWSH_VERSION=7.6.7

ENV ANDROID_HOME=/opt/android-sdk \
    ANDROID_SDK_ROOT=/opt/android-sdk \
    JAVA_HOME=/usr/lib/jvm/default-java \
    DOTNET_CLI_TELEMETRY_OPTOUT=1 \
    DOTNET_NOLOGO=1

RUN apt-get update && apt-get install -y --no-install-recommends \
        default-jdk-headless unzip curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

RUN mkdir -p "$ANDROID_HOME/cmdline-tools" \
    && curl -fsSL "https://dl.google.com/android/repository/commandlinetools-linux-${ANDROID_CMDLINE_TOOLS}_latest.zip" -o /tmp/cmdline-tools.zip \
    && unzip -q /tmp/cmdline-tools.zip -d /tmp/cmdline-tools \
    && mv /tmp/cmdline-tools/cmdline-tools "$ANDROID_HOME/cmdline-tools/latest" \
    && rm -rf /tmp/cmdline-tools /tmp/cmdline-tools.zip

ENV PATH=$PATH:/opt/android-sdk/cmdline-tools/latest/bin:/opt/android-sdk/platform-tools:/opt/dotnet-tools

RUN yes | sdkmanager --sdk_root="$ANDROID_HOME" --licenses > /dev/null || true \
    && sdkmanager --sdk_root="$ANDROID_HOME" --install \
        "platform-tools" \
        "platforms;android-${ANDROID_API}" \
        "build-tools;${ANDROID_BUILD_TOOLS}" \
        "ndk;${ANDROID_NDK}" \
        "cmake;3.31.6" \
    && rm -rf "$ANDROID_HOME/.temp" "$ANDROID_HOME/.downloadIntermediates"

RUN dotnet workload install android

RUN dotnet tool install --tool-path /opt/dotnet-tools PowerShell --version "${PWSH_VERSION}" \
    && /opt/dotnet-tools/pwsh -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
