:: 新项目需要引用一些基础文件，包括：
:: 1，部署基本环境，根目录文件web.config,favicon.ico,Global.asax,index.htm
:: 2，对比更新Web\App_Code,Web\Admin,Css和Scripts
:: 3，引用文件DLL

@echo off
cls
setlocal enabledelayedexpansion
title 更新基础文件

:: 导出来源地址
:: 为了提高速度，可以采用本地地址
if not exist C:\X (
	set svn=https://svn.nnhy.org/svn/X/trunk
) else (set svn=C:\X)
set url=%svn%/trunk

:: 1，部署基本环境
if not exist Web md Web
if not exist WebData md WebData

:: 保存当前目录，并切换目录
pushd Web
set url=%svn%/Src/Web
:: do else 等关键字前后都应该预留空格
for %%i in (Web.config Default.aspx Default.aspx.cs favicon.ico Global.asax index.htm) do (
	if not exist %%i svn export --force %url%/%%i %%i
)

:: 2，对比更新Web\App_Code,Web\Admin,Css和Scripts
set url=%svn%/Src/Web
for %%i in (App_Code Admin Css Scripts) do (
	if exist %%i (
		pushd %%i
		for /r %%f in (*.*) do (
			set name=%%f
			set name=!name:%cd%\%%i\=!
			::echo !name!
			svn export --force %url%/%%i/!name:\=/! !name!
		)
		popd
	) else (
		svn export --force %url%/%%i %%i
	)
)
:: 恢复目录
popd

:: 3，引用文件DLL
set name=DLL
set url=%svn%/%name%
if exist %name% (
	pushd %name%
	for /r %%f in (*.*) do svn export --force %url%/%%~nxf %%~nxf
	popd
) else (
	svn export --force %url% %name%
)

pause