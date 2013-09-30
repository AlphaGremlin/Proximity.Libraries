@ECHO OFF

echo -------------------------------------------------------------------------------
echo Cleaning Source Folder

for /R %%F in (*.bak *.map *.pch *.obj *.log *.pbo *.pbt *.vbw *.aps *._ll *._xe *.exp *.pbi *.sbr *.bsc *.plg *.idb *.ilk *.ncb *.wixobj *.mdb) do (
 del /q "%%F"
)

echo -------------------------------------------------------------------------------
echo Building Tarball...

REM 20%date:~-2,2%-%date:~-7,2%-%date:~-10,2%

tar -cj -X Tarball.txt -f "Release/Libraries %date:~-10,4%-%date:~-5,2%-%date:~-2,2%.tar.bz2" *

:fail
echo -------------------------------------------------------------------------------
pause