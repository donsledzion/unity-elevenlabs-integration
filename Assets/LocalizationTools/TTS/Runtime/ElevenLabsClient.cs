using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ElevenLabs.Models;

namespace ElevenLabs
{
    public class ElevenLabsClient
    {
        private const string BaseUrl = "https://api.elevenlabs.io/v1";
        private readonly ElevenLabsConfig _config;

        public ElevenLabsClient(ElevenLabsConfig config)
        {
            _config = config;
        }

        public IEnumerator GetVoices(Action<ElevenLabsVoicesResponse> onSuccess, Action<string> onError)
        {
            var request = UnityWebRequest.Get($"{BaseUrl}/voices");
            SetHeaders(request);
            yield return SendRequest(request, (text) => onSuccess?.Invoke(JsonUtility.FromJson<ElevenLabsVoicesResponse>(text)), onError);
        }

        public IEnumerator GetModels(Action<ElevenLabsModelsResponse> onSuccess, Action<string> onError)
        {
            var request = UnityWebRequest.Get($"{BaseUrl}/models");
            SetHeaders(request);
            yield return SendRequest(request, (text) => 
            {
                // ElevenLabs API returns a JSON array for models, but Unity's JsonUtility 
                // requires a wrapper object to parse arrays.
                string wrappedJson = "{\"models\":" + text + "}";
                onSuccess?.Invoke(JsonUtility.FromJson<ElevenLabsModelsResponse>(wrappedJson));
            }, onError);
        }

        public IEnumerator TextToSpeech(string text, string voiceId, string modelId, string languageCode, ElevenLabsVoiceSettings settings, Action<byte[]> onSuccess, Action<string> onError)
        {
            var ttsRequest = new TextToSpeechRequest
            {
                text = text,
                model_id = string.IsNullOrEmpty(modelId) ? _config.DefaultModelId : modelId,
                voice_settings = settings
            };

            string json = JsonUtility.ToJson(ttsRequest);
            
            // Inject language_code only if it's not empty, to avoid API errors with empty strings
            if (!string.IsNullOrEmpty(languageCode))
            {
                json = json.Insert(json.Length - 1, $",\"language_code\":\"{languageCode}\"");
            }

            string url = $"{BaseUrl}/text-to-speech/{voiceId}?output_format={_config.OutputFormat}";

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            SetHeaders(request);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return SendRequest(request, null, onError, (data) => onSuccess?.Invoke(data));
        }

        private IEnumerator SendRequest(UnityWebRequest request, Action<string> onSuccessText, Action<string> onError, Action<byte[]> onSuccessData = null)
        {
            request.SendWebRequest();

            // Support for Edit Mode: Wait for request to finish without yielding if not in Play Mode
            while (!request.isDone)
            {
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke(HandleError(request));
            }
            else
            {
                try
                {
                    if (onSuccessData != null) onSuccessData.Invoke(request.downloadHandler.data);
                    else onSuccessText?.Invoke(request.downloadHandler.text);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Error processing ElevenLabs response: {ex.Message}");
                }
            }
            
            request.Dispose();
        }

        private void SetHeaders(UnityWebRequest request)
        {
            if (string.IsNullOrEmpty(_config.ApiKey))
            {
                Debug.LogWarning("ElevenLabs API Key is missing!");
            }
            request.SetRequestHeader("xi-api-key", _config.ApiKey);
        }

        private string HandleError(UnityWebRequest request)
        {
            string errorJson = request.downloadHandler.text;
            if (!string.IsNullOrEmpty(errorJson))
            {
                try
                {
                    var errorResponse = JsonUtility.FromJson<ElevenLabsErrorResponse>(errorJson);
                    if (errorResponse?.detail != null)
                    {
                        return $"ElevenLabs Error: {errorResponse.detail.message} ({errorResponse.detail.status})";
                    }
                }
                catch
                {
                    // Fallback if JSON parsing fails
                }
            }
            return $"Request Error: {request.error}";
        }
    }
}
