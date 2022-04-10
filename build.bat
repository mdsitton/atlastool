cd atlastool
dotnet publish /p:DebugType=None -c Release -r win-x64 --self-contained --output .\bin\build\win-x64
dotnet publish /p:DebugType=None -c Release -r win-x86 --self-contained --output .\bin\build\win-x86
pause