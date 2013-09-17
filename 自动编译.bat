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
		svn update %%i
	)
)
:: 恢复目录
popd

:: 3，编译所有组件
::"D:\MS\Microsoft Visual Studio 10.0\Common7\IDE\devenv.exe" X组件.sln /Build Release
set vs="D:\MS\Microsoft Visual Studio 11.0\Common7\IDE\devenv.com"
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

:: 编译Debug版本
for %%i in (NewLife.Core XCode NewLife.CommonEntity NewLife.Mvc NewLife.Net XAgent XControl XTemplate) do (
	%vs% X组件.sln /Build Debug /Project %%i
	%vs% X组件.sln /Build Net4Debug /Project %%i
)
copy ..\Bin\N*.* ..\DLL\Debug\ /y
copy ..\Bin\X*.* ..\DLL\Debug\ /y
del ..\DLL\Debug\*.config /f/s/q
copy ..\Bin4\N*.* ..\DLL4\Debug\ /y
copy ..\Bin4\X*.* ..\DLL4\Debug\ /y
del ..\DLL4\Debug\*.config /f/s/q

for %%i in (XCoder.exe XCoder.exe.config NewLife.Core.dll XCode.dll XTemplate.dll) do (
	copy ..\代码生成\%%i ..\XCoder\%%i /y
)

:: 5，提交DLL更新
svn commit -m "自动编译" ..\DLL
svn commit -m "自动编译" ..\DLL4
svn commit -m "自动编译" ..\XCoder

:: 6，打包Src和DLL到FTP
set zipexe="C:\Program Files\WinRAR\WinRAR.exe"
set zip=%zipexe% a -m5 -s -z..\Src\Readme.txt -ibck
::set zip="D:\Pro\7-zip\7z.exe" a -tzip -mx9 -mfb258
set zipfile=%date:~0,4%%date:~5,2%%date:~8,2%%time:~0,2%%time:~3,2%%time:~6,2%.rar
set dest=E:\XX\X

:: 发布Src源码
rd XCoder\bin /s/q
rd XCoder\obj /s/q
set zipfile=Src.zip
del Src*.zip /f/q
%zip% -r %zipfile% NewLife.Core\*.cs NewLife.CommonEntity\*.cs XControl\*.cs XAgent\*.cs XCode\Entity\*.cs XCode\DataAccessLayer\Common\*.cs XCoder\*.* XTemplate\Templating\Template.cs
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
%zip% %zipfile% *.dll *.exe *.pdb *.xml *.chm
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布Debug DLL压缩包
:: 保存当前目录，并切换目录
pushd ..\DLL\Debug\
set zipfile=DLL_Debug.zip
del DLL*.zip /f/q
set zip=%zipexe% a -m5 -s -z..\..\Src\Readme.txt -ibck
%zip% %zipfile% *.dll *.exe *.pdb *.xml *.chm
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布DLL4压缩包
:: 保存当前目录，并切换目录
pushd ..\DLL4
set zipfile=DLL4.zip
del DLL*.zip /f/q
set zip4=%zipexe% a -m5 -s -z..\Src\Readme4.txt -ibck
%zip4% %zipfile% *.dll *.exe *.pdb *.xml *.chm
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布Debug DLL4压缩包
:: 保存当前目录，并切换目录
pushd ..\DLL4\Debug\
set zipfile=DLL4_Debug.zip
del DLL*.zip /f/q
set zip4=%zipexe% a -m5 -s -z..\..\Src\Readme4.txt -ibck
%zip4% %zipfile% *.dll *.exe *.pdb *.xml *.chm
move /y DLL*.zip %dest%\%zipfile%
:: 恢复目录
popd

:: 发布代码生成器XCoder
:: 保存当前目录，并切换目录
pushd ..\XCoder
set zipfile=XCoder.zip
del XCoder*.zip /f/q
set zip=%zipexe% a -m5 -s -z..\Src\Readme.txt -ibck
%zip% %zipfile% *.dll *.exe *.config
move /y XCoder*.zip %dest%\%zipfile%
:: 恢复目录
popd