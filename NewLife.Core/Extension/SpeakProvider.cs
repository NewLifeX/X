using System;
using System.Collections;
using System.IO;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.Extension
{
    class SpeakProvider
    {
        private static String typeName = "Microsoft.Speech.Synthesis.SpeechSynthesizer";
        private static String typeName2 = "System.Speech.Synthesis.SpeechSynthesizer";
        private Type _type;

        public SpeakProvider()
        {
            try
            {
                // 新版系统内置
                if (Environment.OSVersion.Version.Major >= 6)
                {
                    typeName = "System.Speech.Synthesis.SpeechSynthesizer";

                    Assembly.Load("System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                }

                _type = typeName2.GetTypeEx(true);
                if (_type == null) _type = PluginHelper.LoadPlugin(typeName, "语音驱动库", "Microsoft.Speech.dll", "Microsoft.Speech");

                //_type = typeof(System.Speech.Synthesis.SpeechSynthesizer);

                // 低版本系统需要安装语音库
                if (_type != null && _type.FullName.StartsWith("Microsoft.Speech")) CheckVoice();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }

        /// <summary>检查是否安装了语音库</summary>
        void CheckVoice()
        {
            if (_type == null) return;

            try
            {
                var synth = _type.CreateInstance(new Object[0]);
                if (synth.Invoke("GetInstalledVoices") is IList vs && vs.Count > 0)
                {
                    var flag = false;
                    foreach (var item in vs)
                    {
                        if (XTrace.Debug) XTrace.WriteLine("语音库：{0}", item.GetValue("VoiceInfo").GetValue("Description"));

                        if ((Boolean)item.GetValue("Enabled")) flag = true;
                    }
                    if (flag) return;
                }
            }
            catch { }

            var url = Setting.Current.PluginServer;
            XTrace.WriteLine("没有找到语音运行时，准备联网获取 {0}", url);

            var dir = ".".GetFullPath();
            if (Runtime.IsWeb) dir = dir.CombinePath("Bin");
            dir.EnsureDirectory();

            var client = new WebClientX(true, true)
            {
                Log = XTrace.Log
            };

            var file2 = client.DownloadLinkAndExtract(url, "SpeechRuntime", dir);

            if (!file2.IsNullOrEmpty())
            {
                // 安装语音库
                var msi = "SpeechPlatformRuntime_x{0}.msi".F(Environment.Is64BitProcess ? 64 : 86);
                msi = dir.CombinePath(msi);
                if (File.Exists(msi))
                {
                    XTrace.WriteLine("正在安装语音平台运行时 {0}", msi);
                    "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);
                }

                msi = "MSSpeech_TTS_zh-CN_HuiHui.msi";
                msi = dir.CombinePath(msi);
                if (File.Exists(msi))
                {
                    XTrace.WriteLine("正在安装微软TTS中文语音库 {0}", msi);
                    "msiexec".Run("/i \"" + msi + "\" /passive /norestart", 5000);
                }
            }
        }

        private Object synth;
        void EnsureSynth()
        {
            if (synth == null)
            {
                try
                {
                    synth = _type.CreateInstance(new Object[0]);
                    synth.Invoke("SetOutputToDefaultAudioDevice", new Object[0]);
                }
                catch (Exception ex)
                {
                    var msi = "SpeechPlatformRuntime_x{0}.msi".F(Environment.Is64BitProcess ? 64 : 86);
                    XTrace.WriteLine("加载语音模块异常，可能未安装语音运行时{0}！", msi);
                    XTrace.WriteException(ex);
                    _type = null;
                }
            }
        }

        public void Speak(String value)
        {
            if (_type == null) return;

            EnsureSynth();
            if (synth != null) synth.Invoke("Speak", value);
        }
        public void SpeakAsync(String value)
        {
            if (_type == null) return;

            EnsureSynth();
            if (synth != null) synth.Invoke("SpeakAsync", value);
        }
    }
}