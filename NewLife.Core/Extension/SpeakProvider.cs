using System;
using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Extension
{
    class SpeakProvider
    {
        private static readonly String typeName = "System.Speech.Synthesis.SpeechSynthesizer";
        private Type _type;

        public SpeakProvider()
        {
            try
            {
                // 新版系统内置
                Assembly asm = null;
                if (Environment.OSVersion.Version.Major >= 6)
                    asm = Assembly.Load("System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                //_type = typeName.GetTypeEx(true);
                _type = Type.GetType(typeName) ?? asm?.GetType(typeName);
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
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

        /// <summary>
        /// 停止话音播报
        /// </summary>
        public void SpeakAsyncCancelAll()
        {
            if (_type == null) return;

            EnsureSynth();
            if (synth != null) synth.Invoke("SpeakAsyncCancelAll");
        }
    }
}