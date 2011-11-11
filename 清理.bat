for /f "tokens=*" %%a in ('dir obj /b /ad /s ^|sort') do rd "%%a" /s/q
for /f "tokens=*" %%a in ('dir bin /b /ad /s ^|sort') do rd "%%a" /s/q

rd WebSite1\Log /s/q
rd WebSite1\App_Data /s/q

md WebSite1\Bin

Copy ..\DLL\XControl.* WebSite1\Bin\