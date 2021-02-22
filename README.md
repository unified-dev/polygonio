# Polygon.io C# Libaries
For developnment and debugging the powershell scripts, use the following process:
1. Build the application using Visual Studio
2. Open `powershell`
3. Change directory to `polygonio\src\PolygonIo.PowerShell\bin\Debug\netstandard2.0`
4. Start a new nested `powershell` session from within the current shell, this way when exiting the compiler can rebuild the binaries
5. `Import-Module .\PolygonIo.PowerShell.dll`
6. Get the shell process id with `$pid` use for the next step
7. From within Visual Studio, go to `Debug` -> `Attach To Process` and find the process from previous step
8. Set breakpoints within Visual Studio
9. Run a command from the `powershell` instance i.e. `Get-AggregatesAsJson -StocksTicker TSLA -Timespan Day -Multiplier 1 -From 01/02/2021 -To 22/02/2022 -ApiKey your_key_here`
10. To make changes to code, disconnect the debugger, exit the netst powershell session and repeat the above steps from the start
