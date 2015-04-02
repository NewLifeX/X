using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using NewLife.Compression;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Extension
{
    class SpeakProvider
    {
        private const string typeName = "Microsoft.Speech.Synthesis.SpeechSynthesizer";
        private Type _type;

        public SpeakProvider()
        {
            //AssemblyX.AssemblyPaths.Add("C:\\X\\");
            _type = typeName.GetTypeEx(true);
            if (_type == null)
            {
                var file = "Microsoft.Speech.dll";
                if (Runtime.IsWeb) file = "Bin".CombinePath(file);
                file = file.EnsureDirectory();

                if (!File.Exists(file))
                {
                    var url = "http://www.newlifex.com/showtopic-51.aspx";
                    XTrace.WriteLine("没有找到语音驱动库，准备联网获取 {0}", url);

                    var client = new WebClientX(true, true);
                    var dir = Path.GetDirectoryName(file);
                    var sw = new Stopwatch();
                    sw.Start();
                    var file2 = client.DownloadLink(url, "Speech", dir);
                    sw.Stop();

                    if (!file2.IsNullOrEmpty())
                    {
                        XTrace.WriteLine("下载完成，共{0:n0}字节，耗时{1}毫秒", file2.AsFile().Length, sw.ElapsedMilliseconds);

                        ZipFile.Extract(file2, dir);

                        // 安装语音库
                        var msi = "SpeechPlatformRuntime_x{0}.msi".F(Runtime.Is64BitOperatingSystem ? 64 : 86);
                        msi = dir.CombinePath(msi);
                        if (File.Exists(msi)) "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);

                        msi = "MSSpeech_TTS_zh-CN_HuiHui.msi";
                        msi = dir.CombinePath(msi);
                        if (File.Exists(msi)) "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);
                    }
                }
                if (File.Exists(file))
                {
                    var assembly = Assembly.LoadFrom(file);
                    if (assembly != null) _type = assembly.GetType(typeName);
                }
            }
        }

        private object synth;
        public void SpeakAsync(String value)
        {
            if (_type == null) return;

            if (synth == null)
            {
                try
                {
                    synth = _type.CreateInstance(new object[0]);
                    synth.Invoke("SetOutputToDefaultAudioDevice", new object[0]);
                }
                catch (Exception ex)
                {
                    var msi = "SpeechPlatformRuntime_x{0}.msi".F(Runtime.Is64BitOperatingSystem ? 64 : 86);
                    XTrace.WriteLine("加载语音模块异常，可能未安装语音运行时{0}！", msi);
                    XTrace.WriteException(ex);
                    _type = null;
                }
            }
            if (synth != null) synth.Invoke("SpeakAsync", value);
        }
    }
}