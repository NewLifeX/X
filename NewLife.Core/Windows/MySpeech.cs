using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using NewLife.Log;

namespace NewLife.Windows
{
    class MySpeech : ISpeech
    {
        private SpeechRecognitionEngine _rg;

        /// <summary>语音识别事件</summary>
        public event EventHandler<RecognitionEventArgs> SpeechRecognized;

        public MySpeech()
        {
        }

        public Boolean Init()
        {
            try
            {
                var sr = new SpeechRecognitionEngine();

                sr.SetInputToDefaultAudioDevice();
                sr.SpeechRecognized += _rg_SpeechRecognized;
                //sr.RecognizeAsync(RecognizeMode.Multiple);

                _rg = sr;

                return true;
            }
#if DEBUG
            catch (Exception ex)
            {
                XTrace.WriteException(ex);

                return false;
            }
#else
            catch { return false; }
#endif
        }

        public void SetChoices(IEnumerable<String> phrases)
        {
            var gc = _rg.Grammars.Count;
            //_rg.RecognizeAsyncCancel();
            _rg.UnloadAllGrammars();

            var cs = new Choices();
            cs.Add(phrases.ToArray());

            var gb = new GrammarBuilder();
            gb.Append(cs);

            var gr = new Grammar(gb);

            // 不能加载自然语法，否则关键字识别率大大下降
            //_rg.LoadGrammarAsync(new DictationGrammar());
            _rg.LoadGrammarAsync(gr);

            // 首次启动
            if (gc == 0) _rg.RecognizeAsync(RecognizeMode.Multiple);
        }

        void _rg_SpeechRecognized(Object sender, SpeechRecognizedEventArgs e)
        {
            if (e.Result == null) return;

            var ev = new RecognitionEventArgs(e.Result.Confidence, e.Result.Text);

            if (SpeechRecognized != null) SpeechRecognized.Invoke(sender, ev);
        }

        public static void Main() { }
    }
}