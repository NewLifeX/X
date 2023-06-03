using System.Reflection;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Extension;

class SpeakProvider
{
    private static readonly String typeName = "System.Speech.Synthesis.SpeechSynthesizer";
    private Type _type;

    public SpeakProvider()
    {
        try
        {
            //_type = typeName.GetTypeEx(true);
            _type = Type.GetType(typeName);
            if (_type == null)
            {
                Assembly asm = null;
                try
                {
                    // 新版系统内置
                    if (Environment.OSVersion.Version.Major >= 6)
                    {
                        asm ??= Assembly.Load("System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                    }
                }
                catch { }
                try
                {
                    asm ??= Assembly.Load("System.Speech");
                }
                catch { }
                _type = asm?.GetType(typeName);
            }
        }
        catch (Exception ex)
        {
            XTrace.WriteException(ex);
        }

        if (_type == null) XTrace.WriteLine("找不到语音库System.Speech，需要从nuget引用");
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
        synth?.Invoke("Speak", value);
    }

    public void SpeakAsync(String value)
    {
        if (_type == null) return;

        EnsureSynth();
        synth?.Invoke("SpeakAsync", value);
    }

    /// <summary>
    /// 停止话音播报
    /// </summary>
    public void SpeakAsyncCancelAll()
    {
        if (_type == null) return;

        EnsureSynth();
        synth?.Invoke("SpeakAsyncCancelAll");
    }
}