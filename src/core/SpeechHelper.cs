using System;
using System.Reflection;

namespace GenOffice.Core
{
    public static class SpeechHelper
    {
        private static object _synth = null;
        private static MethodInfo _speakAsyncMethod = null;
        private static MethodInfo _speakAsyncCancelAllMethod = null;
        private static PropertyInfo _stateProp = null;
        private static bool _initialized = false;
        private static bool _isSpeaking = false;

        private static void Init()
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                Type synthType = Type.GetType("System.Speech.Synthesis.SpeechSynthesizer, System.Speech, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
                if (synthType != null)
                {
                    _synth = Activator.CreateInstance(synthType);
                    _speakAsyncMethod = synthType.GetMethod("SpeakAsync", new Type[] { typeof(string) });
                    _speakAsyncCancelAllMethod = synthType.GetMethod("SpeakAsyncCancelAll", Type.EmptyTypes);
                    _stateProp = synthType.GetProperty("State");
                }
            }
            catch { }
        }

        public static void SpeakAsync(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Init();
            if (_synth != null && _speakAsyncMethod != null)
            {
                try
                {
                    Stop();
                    _speakAsyncMethod.Invoke(_synth, new object[] { text });
                    _isSpeaking = true;
                }
                catch { }
            }
        }

        public static void Stop()
        {
            Init();
            if (_synth != null && _speakAsyncCancelAllMethod != null)
            {
                try
                {
                    _speakAsyncCancelAllMethod.Invoke(_synth, null);
                    _isSpeaking = false;
                }
                catch { }
            }
        }

        public static bool IsSpeaking()
        {
            Init();
            if (_synth != null && _stateProp != null)
            {
                try
                {
                    object val = _stateProp.GetValue(_synth, null);
                    if (val != null)
                    {
                        string stateStr = val.ToString();
                        return stateStr.IndexOf("Speaking", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch { }
            }
            return _isSpeaking;
        }

        public static void ToggleSpeech(string text)
        {
            if (IsSpeaking())
            {
                Stop();
            }
            else
            {
                SpeakAsync(text);
            }
        }
    }
}
