param($installPath, $toolsPath, $package, $project)


try
{
    # 所有包存放地址
    $allPakPath = Split-Path -Parent $installPath
    $allPakPath = $allPakPath + "/"

    # 包文件夹名
    $corePName = "NewLife.Core"
    $xcodePName = "NewLife.XCode"    

    #获取版本号
    
        # nuget包配置
        $packageCfg = $project.ProjectItems.Item("packages.config")

        # nuget包配置文件名 
        $packageCfgPath = $packageCfg.Properties("FullPath").Value

        # 读取节点
        $xmlDoc = New-Object "System.Xml.XmlDocument"  
        $xmlDoc.Load($packageCfgPath)
        $coreNode = $xmlDoc.SelectSingleNode("/packages/package[@id='NewLife.Core']")
        $xcodeNode = $xmlDoc.SelectSingleNode("/packages/package[@id='NewLife.XCode']")

        # 版本号
        $coreV = $coreNode.version
        $xcodeV = $xcodeNode.version
    
    
    # 包内路径
    $pPath = "lib/net40/";

      # 文件名
    $coreDllName = "NewLife.Core.dll"
    $xcodeDllName = "XCode.dll" 

    # 源地址
    $coreSrc = $allPakPath + $corePName + "." + $coreV + "/" + $pPath + $coreDllName
    $xcodeSrc = $allPakPath + $xcodePName + "." + $xcodeV + "/" + $pPath + $xcodeDllName

    # 目标文件夹
    $tarDir = "DLL"
    if(!( Test-Path $tarDir ))
    {
        mkdir $tarDir
    }

    #目标地址
    $coreTar = $tarDir + "/" + $coreDllName
    $xcodeTar = $tarDir + "/" + $xcodeDllName

    #复制文件
    Copy-Item $coreSrc $coreTar
    Copy-Item $xcodeSrc $xcodeTar
}
catch
{
    "复制dll文件出错，请手动复制xcode.dll、newlife.core.dll到项目目录DLL文件夹" | Out-File debug.txt
}