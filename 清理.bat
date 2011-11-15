for /f "tokens=*" %%a in ('dir obj /b /ad /s ^|sort') do rd "%%a" /s/q
for /f "tokens=*" %%a in ('dir bin /b /ad /s ^|sort') do rd "%%a" /s/q

rd Web\Log /s/q
rd Web\App_Data /s/q

md Web\Bin

Copy ..\DLL\XControl.* Web\Bin\