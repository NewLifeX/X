::@echo off
cls
setlocal enabledelayedexpansion
title 自动同步

set url=https://svn.newlifex.com/svn/X/trunk/Src
for %%i in (NewLife.Core NewLife.Net XCode XAgent XCoder XControl XTemplate) do (
	git svn clone %url%/%%i --no-metadata --authors-file=user.txt %%i
	git svn fetch %url%/%%i --authors-file=user.txt

	git remote add origin https://github.com/NewLifeX/%%i.git
	git push --all --force-with-lease --progress "origin"
)
