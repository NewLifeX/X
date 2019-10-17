#if __WIN__
using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.IO;
using NewLife.Log;
using NewLife.Model;
using NewLife.Reflection;

namespace NewLife.Windows
{
    /// <summary>语音识别</summary>
    public class SpeechRecognition : DisposeBase
    {
        #region 属性
        private ISpeech _speech;

        private IDictionary<String, Action> _dic;

        /// <summary>系统名称。用于引导前缀</summary>
        public String Name { get; set; } = "丁丁";

        /// <summary>最后一次进入引导前缀的时间。</summary>
        private DateTime _Tip;

        /// <summary>是否可用</summary>
        public Boolean Enable => _speech != null;
        #endregion

        #region 构造
        private SpeechRecognition()
        {
            _dic = new Dictionary<String, Action>();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _speech.TryDispose();
        }
        #endregion

        #region 静态
        /// <summary>当前实例</summary>
        public static SpeechRecognition Current { get; } = new SpeechRecognition();

        /// <summary>获取已注册的所有键值</summary>
        /// <returns></returns>
        public String[] GetAllKeys()
        {
            return _dic.Keys.ToArray();
        }
        #endregion

        #region 方法
        Boolean Init()
        {
            if (_speech != null) return true;

            _speech = ObjectContainer.Current.Resolve<ISpeech>();
            if (_speech != null) return true;

            try
            {
                Assembly.Load("System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");

                var code = Assembly.GetExecutingAssembly().GetFileResource("MySpeech.cs").ToStr();
                var sc = ScriptEngine.Create(code, false);
                sc.Compile();

                _speech = sc.Type.CreateInstance() as ISpeech;
                if (_speech == null) return false;

                if (!_speech.Init()) return false;

                _speech.SpeechRecognized += _rg_SpeechRecognized;

                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                _speech.TryDispose();
                return false;
            }
        }

        /// <summary>注册要语音识别的关键字到委托</summary>
        /// <param name="text"></param>
        /// <param name="callback"></param>
        public SpeechRecognition Register(String text, Action callback)
        {
            var flag = _dic.ContainsKey(text);

            if (callback != null)
            {
                _dic[text] = callback;

                if (!flag) Change();
            }
            else if (flag)
                _dic.Remove(text);

            return this;
        }

        void Change()
        {
            if (_speech == null) return;

            lock (this)
            {
                if (!Init()) return;

                var list = new List<String>
                {
                    Name
                };
                list.AddRange(_dic.Keys);
                _speech.SetChoices(list);
            }
        }

        //void _rg_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        void _rg_SpeechRecognized(Object sender, RecognitionEventArgs e)
        {
            var conf = e.Confidence;
            if (conf < 0.5) return;

            var txt = e.Text;

            // 语音识别前，必须先识别前缀名称，然后几秒内识别关键字
            if (_Tip.AddSeconds(3) < DateTime.Now)
            {
                // 此时只识别前缀
                if (txt != Name) return;

                XTrace.WriteLine("语音识别：{0} {1}", txt, conf);

                // 现在可以开始识别关键字啦
                _Tip = DateTime.Now;
            }
            else
            {
                XTrace.WriteLine("语音识别：{0} {1}", txt, conf);

                if (_dic.TryGetValue(txt, out var func)) func();
            }
        }
        #endregion
    }

    /// <summary>语音接口</summary>
    public interface ISpeech
    {
        /// <summary>语音识别事件</summary>
        event EventHandler<RecognitionEventArgs> SpeechRecognized;

        /// <summary>初始化</summary>
        /// <returns></returns>
        Boolean Init();

        /// <summary>设置识别短语</summary>
        /// <param name="phrases"></param>
        void SetChoices(IEnumerable<String> phrases);
    }

    /// <summary>识别事件参数</summary>
    public class RecognitionEventArgs : EventArgs
    {
        /// <summary>获取识别器分配的值，此值表示与给定输入匹配的可能性</summary>
        public Single Confidence { get; }

        /// <summary>获取语音识别器从识别的输入生成的规范化文本</summary>
        public String Text { get; }

        /// <summary>实例化</summary>
        /// <param name="conf"></param>
        /// <param name="text"></param>
        public RecognitionEventArgs(Single conf, String text)
        {
            Confidence = conf;
            Text = text;
        }
    }
}
#endif