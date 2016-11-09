using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
//using System.Speech.Recognition;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Windows
{
    /// <summary>语音识别</summary>
    public class SpeechRecognition : DisposeBase
    {
        #region 属性
        //private SpeechRecognitionEngine _rg;
        private Object _rg;

        private IDictionary<String, Action> _dic;

        /// <summary>系统名称。用于引导前缀</summary>
        private String _Name = "丁丁";

        /// <summary>最后一次进入引导前缀的时间。</summary>
        private DateTime _Tip;
        #endregion

        #region 构造
        private SpeechRecognition()
        {
            _dic = new Dictionary<String, Action>();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (_rg != null) _rg.TryDispose();
        }
        #endregion

        #region 静态
        private static SpeechRecognition _instance = new SpeechRecognition();

        /// <summary>系统名称。用于引导前缀</summary>
        public static String Name { get { return _instance._Name; } set { _instance._Name = value; } }

        /// <summary>注册要语音识别的关键字到委托</summary>
        /// <param name="text"></param>
        /// <param name="callback"></param>
        public static void Register(String text, Action callback)
        {
            if (_instance == null) _instance = new SpeechRecognition();

            _instance.RegisterInternal(text, callback);
        }

        /// <summary>获取已注册的所有键值</summary>
        /// <returns></returns>
        public static String[] GetAllKeys()
        {
            if (_instance == null) _instance = new SpeechRecognition();

            return _instance._dic.Keys.ToArray();
        }
        #endregion

        #region 方法
        Boolean Init()
        {
            if (_rg != null) return true;

            //var sr = new SpeechRecognitionEngine();

            Assembly.Load("System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            var type = "SpeechRecognitionEngine".GetTypeEx(true);
            var sr = type.CreateInstance();

            try
            {
                //sr.SetInputToDefaultAudioDevice();
                //sr.SpeechRecognized += _rg_SpeechRecognized;
                ////_rg.RecognizeAsync(RecognizeMode.Multiple);

                sr.Invoke("SetInputToDefaultAudioDevice");
                sr.Invoke("add_SpeechRecognized", new EventHandler(_rg_SpeechRecognized));

                _rg = sr;

                return true;
            }
            catch
            {
                sr.TryDispose();
                return false;
            }
        }

        void RegisterInternal(String text, Action callback)
        {
            var flag = _dic.ContainsKey(text);

            if (callback != null)
            {
                _dic[text] = callback;

                if (!flag) Change();
            }
            else if (flag)
                _dic.Remove(text);
        }

        void Change()
        {
            lock (this)
            {
                if (!Init()) return;

                //var gc = _rg.Grammars.Count;
                ////_rg.RecognizeAsyncCancel();
                //_rg.UnloadAllGrammars();

                //var cs = new Choices();
                //cs.Add(_Name);
                //cs.Add(_dic.Keys.ToArray());

                //var gb = new GrammarBuilder();
                //gb.Append(cs);

                //var gr = new Grammar(gb);

                //// 不能加载自然语法，否则关键字识别率大大下降
                ////_rg.LoadGrammarAsync(new DictationGrammar());
                //_rg.LoadGrammarAsync(gr);

                //// 首次启动
                //if (gc == 0) _rg.RecognizeAsync(RecognizeMode.Multiple);

                var gc = (Int32)_rg.GetValue("Grammars").GetValue("Count");
                _rg.Invoke("UnloadAllGrammars");

                var cs = "Choices".GetTypeEx().CreateInstance() as IList;
                cs.Add(_Name);
                cs.Add(_dic.Keys.ToArray());

                var gb = "GrammarBuilder".GetTypeEx().CreateInstance();
                gb.Invoke("Append", cs);

                var gr = "Grammar".GetTypeEx().CreateInstance(gb);

                _rg.Invoke("LoadGrammarAsync", gr);

                // 首次启动
                if (gc == 0) _rg.Invoke("RecognizeAsync", "RecognizeMode".GetTypeEx().GetFieldEx("Multiple"));
            }
        }

        //void _rg_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        void _rg_SpeechRecognized(object sender, EventArgs e)
        {
            //var rs = e.Result;
            var rs = e.GetValue("Result");
            if (rs == null) return;

            var conf = (Double)rs.GetValue("Confidence");
            if (conf < 0.5) return;

            var txt = (String)rs.GetValue("Text");

            // 语音识别前，必须先识别前缀名称，然后几秒内识别关键字
            if (_Tip.AddSeconds(3) < DateTime.Now)
            {
                // 此时只识别前缀
                if (txt != _Name) return;

                XTrace.WriteLine("语音识别：{0} {1}", txt, conf);

                // 现在可以开始识别关键字啦
                _Tip = DateTime.Now;
            }
            else
            {
                XTrace.WriteLine("语音识别：{0} {1}", txt, conf);

                Action func = null;
                if (_dic.TryGetValue(txt, out func)) func();
            }
        }
        #endregion
    }
}