using System;
using System.Collections;
using System.IO;
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
            try
            {
                var url = "http://www.newlifex.com/showtopic-51.aspx";
                _type = PluginHelper.LoadPlugin(typeName, "语音驱动库", "Microsoft.Speech.dll", "Microsoft.Speech", url);

                CheckVoice();
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
                var vs = synth.Invoke("GetInstalledVoices") as IList;
                if (vs != null && vs.Count > 0)
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

            var url = "http://www.newlifex.com/showtopic-51.aspx";
            XTrace.WriteLine("没有找到语音运行时，准备联网获取 {0}", url);

            var dir = ".".GetFullPath();
            if (Runtime.IsWeb) dir = dir.CombinePath("Bin");
            dir.EnsureDirectory();

            var client = new WebClientX(true, true);
            client.Log = XTrace.Log;

            var file2 = client.DownloadLinkAndExtract(url, "SpeechRuntime", dir);

            if (!file2.IsNullOrEmpty())
            {
                // 安装语音库
                var msi = "SpeechPlatformRuntime_x{0}.msi".F(Runtime.Is64BitOperatingSystem ? 64 : 86);
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

        private object synth;
        void EnsureSynth()
        {
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