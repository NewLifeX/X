:: 自动编译X组件，并更新到DLL中去
:: 1，更新所有源码Src
:: 2，更新DLL
:: 3，编译所有组件
:: 4，拷贝DLL
:: 5，提交DLL更新
:: 6，打包Src和DLL到FTP

::@echo off
::cls
setlocal enabledelayedexpansion
title 自动编译

:: 1，更新所有源码Src
:: 2，更新DLL
:: 保存当前目录，并切换目录
pushd ..
set svn=https://svn.newlifex.com/svn/X/trunk
:: do else 等关键字前后都应该预留空格
for %%i in (Src DLL DLL4 XCoder) do (
	if not exist %%i (
		svn checkout %svn%/%%i %%i
	) else (
		svn info %svn%/%%i
		svn revert %%i
		svn update %%i
	)
)
:: 恢复目录
popd

:: 3，编译所有组件
::"D:\MS\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" X组件.sln /Build Release
set vs="B:\MS\Microsoft Visual Studio 12.0\Common7\IDE\devenv.com"
for %%i in (NewLife.Core XCode NewLife.CommonEntity NewLife.Mvc NewLife.Net XAgent XControl XTemplate) do (
	%vs% X组件.sln /Build Release /Project %%i
	%vs% X组件.sln /Build Net4Release /Project %%i
)
for %%i in (XCoder) do (
	%vs% X组件.sln /Build Release /Project %%i
)

:: 4，拷贝DLL
copy ..\Bin\N*.* ..\DLL\ /y
copy ..\Bin\X*.* ..\DLL\ /y
del ..\DLL\*.config /f/s/q
copy ..\Bin4\N*.* ..\DLL4\ /y
copy ..\Bin4\X*.* ..\DLL4\ /y
del ..\DLL4\*.config /f/s/q

if not exist ..\XCoder\Zip (
	md ..\XCoder\Zip
)
for %%i in (XCoder.exe XCoder.exe.config NewLife.Core.dll XCode.dll XTemplate.dll NewLife.Net.dll) do (
	copy ..\XCoder\%%i ..\XCoder\Zip\%%i /y
)

:: 5，提交DLL更新
svn commit -m "自动编译" ..\DLL
svn commit -m "自动编译" ..\DLL4
svn commit -m "自动编译" ..\XCoder

:: 6，打包Src和DLL到FTP
set zipexe="B:\Pro\WinRAR\WinRAR.exe"
set zip=%zipexe% a -m5 -s -z..\Src\Readme.txt -ibck
::set zipexe="D:\Pro\7-zip\7z.exe"
::set zip=%zipexe% a -tzip -mx9
set dest=E:\XX\X

:: 发布Src源码
rd XCoder\bin /s/q
rd XCoder\obj /s/q
set zipfile=Src.zip
del Src*.zip /f/q
%zip% -r %zipfile% XCoder\*.*
move /y Src*.zip %dest%\%zipfile%

:: 发布XCode例子源码
rd YWS\bin /s/q
rd YWS\obj /s/q
rd Web\bin /s/q
rd Web\Log /s/q
rd Web\App_Data /s/q
md Web\Bin
Copy ..\DLL\XControl.* Web\Bin\ /y
set zipfile=XCodeSample.zip
del XCodeSample*.zip /f/q
%zip% -r %zipfile% YWS\*.* Web\*.* XCodeSample.sln
move /y XCodeSample*.zip %dest%\%zipfile%

:: 发布DLL压缩包
:: 保存当前目录，并切换目录
pushd ..\DLL
set zipfile=DLL.zip
del DLL*.zip /f/q
%zip% %zipfile% *.dll *.exe *.pdb *.xml
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布DLL4压缩包
:: 保存当前目录，并切换目录
pushd ..\DLL4
set zipfile=DLL4.zip
del DLL*.zip /f/q
set zip4=%zipexe% a -m5 -s -z..\Src\Readme4.txt -ibck
%zip4% %zipfile% *.dll *.exe *.pdb *.xml
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布代码生成器XCoder
:: 保存当前目录，并切换目录
pushd ..\XCoder\Zip
set zipfile=XCoder.zip
del XCoder*.zip /f/q
::del *.vshost.* /f/q
::del Setting.config /f/q
set zip=%zipexe% a -m5 -s -z..\..\Src\Readme.txt -ibck
%zip% %zipfile% *.dll *.exe *.config
move /y XCoder*.zip %dest%\%zipfile%
:: 恢复目录
popd

::pause