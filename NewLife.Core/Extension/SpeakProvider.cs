using System.Reflection;
using System.Diagnostics.CodeAnalysis;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Extension;

class SpeakProvider
{
    private static readonly String typeName = "System.Speech.Synthesis.SpeechSynthesizer";
    private Type? _type;

    [UnconditionalSuppressMessage("Trimming", "IL2057", Justification = "语音合成经类型名字符串动态加载；AOT 下不受支持")]
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "语音合成经类型名字符串动态加载；AOT 下不受支持")]
    public SpeakProvider()
    {
        try
        {
            //_type = typeName.GetTypeEx(true);
            _type = Type.GetType(typeName);
            if (_type == null)
            {
                Assembly? asm = null;
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

        if (_type == null) XTrace.WriteLine("The System.Speech speech library was not found; reference it from NuGet.");
    }

    private Object? synth;
    void EnsureSynth()
    {
        if (synth == null && _type != null)
        {
            try
            {
                synth = _type.CreateInstance([]);
                synth?.Invoke("SetOutputToDefaultAudioDevice", []);
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