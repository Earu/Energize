@echo off
echo Cleaning solution...

if exist ".vs" (
    rmdir ".vs" /s /q
)
echo Cleaned Visual Studio metadata

if exist "Energize/Data/Markov" (
    rmdir "Energize/Data/Markov" /s /q
)

if exist "Energize/Lua/LuaSavedScripts" (
    rmdir "Energize/Lua/LuaSavedScripts" /s /q
)

if exist "logs" (
    rmdir "logs" /s /q
)
echo Cleaned generated files

if exist "EnergizeDB.db" (
    del "EnergizeDB.db" /s /q
)
echo Cleaned local database

for %%G in (Energize, Energize.Commands, Energize.Interfaces, Energize.Essentials, Energize.Steam) do (
    if exist "%%G/bin" (
        rmdir "%%G/bin" /s /q
    )
    if exist "%%G/obj" (
        rmdir "%%G/obj" /s /q
    )

    echo Cleaned %%G
)

echo Done cleaning!
pause