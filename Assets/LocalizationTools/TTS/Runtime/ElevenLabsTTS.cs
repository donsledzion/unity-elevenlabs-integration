using System;
using System.Collections.Generic;
using UnityEngine;
using ElevenLabs.Models;
using ElevenLabs.Utils;

namespace ElevenLabs
{
    public class ElevenLabsTTS : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private ElevenLabsConfig config;
        
        [Header("Voice & Model Settings")]
        [SerializeField] private string voiceId;
        [SerializeField] private string modelId;
        [SerializeField] private string languageCode;
        [SerializeField] private ElevenLabsVoiceSettings voiceSettings = new ElevenLabsVoiceSettings();
        
        private ElevenLabsClient _client;

        public ElevenLabsConfig Config => config;
        public string VoiceId { get => voiceId; set => voiceId = value; }
        public string ModelId { get => modelId; set => modelId = value; }
        public string LanguageCode { get => languageCode; set => languageCode = value; }
        public ElevenLabsVoiceSettings VoiceSettings => voiceSettings;

        private void Awake()
        {
            if (config == null)
            {
                config = Resources.Load<ElevenLabsConfig>("ElevenLabsConfig");
            }
            
            if (config != null)
            {
                _client = new ElevenLabsClient(config);
            }
        }

        public void ConvertTextToSpeech(string text, Action<string> onFileSaved = null, Action<string> onError = null)
        {
            if (_client == null)
            {
                onError?.Invoke("ElevenLabsClient not initialized. Check configuration.");
                return;
            }

            string vId = string.IsNullOrEmpty(voiceId) ? config.DefaultVoiceId : voiceId;
            string mId = string.IsNullOrEmpty(modelId) ? config.DefaultModelId : modelId;

            StartCoroutine(_client.TextToSpeech(text, vId, mId, languageCode, voiceSettings, 
                (audioData) => 
                {
                    string fileName = FileSystemUtils.GetSafeFileName(text);
                    string path = FileSystemUtils.SaveAudioFile(audioData, fileName);
                    Debug.Log($"TTS saved to: {path}");
                    onFileSaved?.Invoke(path);
                }, 
                (error) => 
                {
                    Debug.LogError(error);
                    onError?.Invoke(error);
                }));
        }

        public void FetchVoices(Action<List<ElevenLabsVoice>> onVoicesReceived)
        {
            if (_client == null) Awake();
            if (_client == null) return;

            StartCoroutine(_client.GetVoices(
                (response) => onVoicesReceived?.Invoke(response.voices),
                (error) => Debug.LogError($"Failed to fetch voices: {error}")
            ));
        }

        public void FetchModels(Action<List<ElevenLabsModel>> onModelsReceived)
        {
            if (_client == null) Awake();
            if (_client == null) return;

            StartCoroutine(_client.GetModels(
                (response) => onModelsReceived?.Invoke(response.models),
                (error) => Debug.LogError($"Failed to fetch models: {error}")
            ));
        }
    }
}
