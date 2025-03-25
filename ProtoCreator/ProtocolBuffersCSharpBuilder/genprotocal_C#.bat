@echo off
cd %~dp0

rd /s /q protocal-c#

if not exist protocal-c# mkdir protocal-c#

setlocal enabledelayedexpansion

for /f %%i in (file_list_C#.txt) do (
    for /f "tokens=2 delims=\." %%k in ("%%i") do (
        set filename=%%k
    )
    protobuf-bin\protoc.exe --proto_path=proto --descriptor_set_out=protocal-c#\!filename!.bin  %%i
    if errorlevel 1 goto error
    protobuf-bin-c#\protogen -i:protocal-c#\!filename!.bin -o:protocal-c#\!filename!.cs -p:detectMissing
    if errorlevel 1 goto error
    del protocal-c#\!filename!.bin
)

:end
endlocal
echo Done.
pause
exit /b 0

:error
echo An error occurred during the compilation process.
pause
exit /b 1