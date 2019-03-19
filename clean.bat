@echo off
echo Cleaning solution...

if exist "Energize/.vs" (
    rmdir "Energize/.vs" /s /q
)

for %%G in (Energize, Energize.Commands, Energize.Interfaces, Energize.Essentials) do (
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