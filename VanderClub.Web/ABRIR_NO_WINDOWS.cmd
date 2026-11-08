@echo off
cd /d "%~dp0"
where node >nul 2>nul
if errorlevel 1 (
  echo Instale o Node.js 20 ou superior e execute este arquivo novamente.
  echo Alternativa: consulte COMECE_AQUI.txt para usar Python.
  pause
  exit /b 1
)
node servir.cjs
pause
