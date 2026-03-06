using System;
using System.Collections.Generic;

namespace ElevenLabs.Models
{
    [Serializable]
    public class TranslationEntry
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class BatchTranslationResult
    {
        public List<TranslationEntry> results;
    }
}
