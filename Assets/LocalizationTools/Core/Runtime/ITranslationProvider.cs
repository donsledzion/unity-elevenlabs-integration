using System;
using System.Collections;
using System.Collections.Generic;
using ElevenLabs.Models;

namespace ElevenLabs.Core
{
    public interface ITranslationProvider
    {
        IEnumerator Translate(string text, string targetLanguage, Action<string> onSuccess, Action<string> onError, string context = null);
        IEnumerator TranslateBatch(string sourceLang, string targetLang, List<TranslationEntry> entries, Action<List<TranslationEntry>> onSuccess, Action<string> onError, string context = null);
    }
}
