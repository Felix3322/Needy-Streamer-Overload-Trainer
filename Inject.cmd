@echo off
setlocal
cd /d "%~dp0"
if not exist "bin\smi.exe" (echo Injector is missing.& pause & exit /b 1)
if not exist "bin\InjectedTrainer.dll" (echo Trainer DLL is missing.& pause & exit /b 1)
echo Start NEEDY GIRL OVERDOSE and enter a save first.
echo Injecting into Windose.exe...
"bin\smi.exe" inject -p Windose -a "%~dp0bin\InjectedTrainer.dll" -n NGOTrainer -c NumericTrainer -m Install
echo If injection succeeded, switch to the game and press F1.
pause
