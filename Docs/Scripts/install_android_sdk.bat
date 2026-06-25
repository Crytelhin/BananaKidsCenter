@echo off
set JAVA_HOME=C:\Program Files\Android\Android Studio\jbr
set PATH=%JAVA_HOME%\bin;%PATH%
set SDK_DIR=C:\Users\Valeriy\AppData\Local\Android\Sdk
call "%SDK_DIR%\cmdline-tools\latest\bin\sdkmanager.bat" --sdk_root=%SDK_DIR% "platforms;android-35" "build-tools;35.0.0" "platform-tools"
