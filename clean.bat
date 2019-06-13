@echo off
echo Cleaning solution...

if exist ".vs" (
    rmdir ".vs" /s /q
)
echo Cleaned Visual Studio metadata

if exist "logs" (
    rmdir "logs" /s /q
)
echo Cleaned logs

if exist "EnergizeDB.db" (
    del "EnergizeDB.db" /s /q
)
echo Cleaned local database

for %%G in (Energize, Energize.Commands, Energize.Interfaces, Energize.Essentials, Energize.Web) do (
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