@echo off
echo ========================================
echo Meow AI 系统监控 - 独立发布脚本
echo ========================================
echo.

echo 请选择发布架构：
echo 1. x64 (推荐，适用于大多数现代电脑)
echo 2. x86 (32位系统)
echo 3. ARM64 (ARM设备，如Surface Pro X)
echo 4. 全部架构
echo.
set /p choice="请输入选择 (1-4): "

if "%choice%"=="1" goto x64
if "%choice%"=="2" goto x86
if "%choice%"=="3" goto arm64
if "%choice%"=="4" goto all
echo 无效选择！
goto end

:x64
echo.
echo 正在发布 x64 版本...
dotnet publish -p:Platform=x64 -p:Configuration=Release -p:PublishProfile=win-x64-standalone.pubxml
if %errorlevel%==0 (
    echo.
    echo ✅ x64 版本发布成功！
    echo 输出目录: bin\publish-standalone\
) else (
    echo.
    echo ❌ x64 版本发布失败！
)
goto end

:x86
echo.
echo 正在发布 x86 版本...
dotnet publish -p:Platform=x86 -p:Configuration=Release -p:PublishProfile=win-x86-standalone.pubxml
if %errorlevel%==0 (
    echo.
    echo ✅ x86 版本发布成功！
    echo 输出目录: bin\publish-standalone-x86\
) else (
    echo.
    echo ❌ x86 版本发布失败！
)
goto end

:arm64
echo.
echo 正在发布 ARM64 版本...
dotnet publish -p:Platform=ARM64 -p:Configuration=Release -p:PublishProfile=win-arm64-standalone.pubxml
if %errorlevel%==0 (
    echo.
    echo ✅ ARM64 版本发布成功！
    echo 输出目录: bin\publish-standalone-arm64\
) else (
    echo.
    echo ❌ ARM64 版本发布失败！
)
goto end

:all
echo.
echo 正在发布所有架构版本...
echo.

echo [1/3] 发布 x64 版本...
dotnet publish -p:Platform=x64 -p:Configuration=Release -p:PublishProfile=win-x64-standalone.pubxml
if %errorlevel%==0 (
    echo ✅ x64 版本发布成功！
) else (
    echo ❌ x64 版本发布失败！
)

echo.
echo [2/3] 发布 x86 版本...
dotnet publish -p:Platform=x86 -p:Configuration=Release -p:PublishProfile=win-x86-standalone.pubxml
if %errorlevel%==0 (
    echo ✅ x86 版本发布成功！
) else (
    echo ❌ x86 版本发布失败！
)

echo.
echo [3/3] 发布 ARM64 版本...
dotnet publish -p:Platform=ARM64 -p:Configuration=Release -p:PublishProfile=win-arm64-standalone.pubxml
if %errorlevel%==0 (
    echo ✅ ARM64 版本发布成功！
) else (
    echo ❌ ARM64 版本发布失败！
)

:end
echo.
echo ========================================
echo 发布完成！
echo ========================================
pause
