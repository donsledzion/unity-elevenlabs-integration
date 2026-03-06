using System;
using System.Collections.Generic;
using UnityEngine;

namespace ElevenLabs.Models
{
    [Serializable]
    public class ElevenLabsVoicesResponse
    {
        public List<ElevenLabsVoice> voices;
    }

    [Serializable]
    public class ElevenLabsModelsResponse
    {
        public List<ElevenLabsModel> models;
    }

    [Serializable]
    public class ElevenLabsModel
    {
        public string model_id;
        public string name;
        public string description;
        public List<string> languages;
    }

    [Serializable]
    public class ElevenLabsVoice
    {
        public string voice_id;
        public string name;
        public string category;
        public string description;
        // Note: JsonUtility doesn't support Dictionary. 
        // We'll use this string to check for language clues if needed, or just rely on name/category.
        // ElevenLabs API returns a labels object.
        public ElevenLabsVoiceSettings settings;
    }

    [Serializable]
    public class ElevenLabsVoiceSettings
    {
        [Range(0, 1)] public float stability = 0.5f;
        [Range(0, 1)] public float similarity_boost = 0.75f;
        [Range(0, 1)] public float style = 0.0f;
        [Range(0, 1)] public bool use_speaker_boost = true;
    }

    [Serializable]
    public class TextToSpeechRequest
    {
        public string text;
        public string model_id = "eleven_multilingual_v2";
        public ElevenLabsVoiceSettings voice_settings;
    }

    [Serializable]
    public class ElevenLabsErrorResponse
    {
        public ErrorDetail detail;

        [Serializable]
        public class ErrorDetail
        {
            public string status;
            public string message;
            public string type;
        }
    }
}
