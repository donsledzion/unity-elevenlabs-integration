using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ElevenLabs.Models;
using ElevenLabs.Core;

namespace ElevenLabs.OpenAI
{
    public class OpenAIClient : ITranslationProvider
    {
        private readonly OpenAIConfig _config;

        public OpenAIClient(OpenAIConfig config)
        {
            _config = config;
        }

        public IEnumerator Translate(string text, string targetLanguage, Action<string> onSuccess, Action<string> onError, string context = null)
        {
            if (!_config.IsValid)
            {
                onError?.Invoke("OpenAI API Key is missing!");
                yield break;
            }

            var requestData = new OpenAIChatRequest
            {
                model = _config.Model,
                messages = new List<OpenAIChatMessage>
                {
                    new OpenAIChatMessage { role = "user", content = $"Translate the following text to {targetLanguage}. {(string.IsNullOrEmpty(context) ? "" : $"Context: {context}. ")}ONLY return the translated text without any explanations or extra characters.\n\nText: {text}" }
                },
                temperature = _config.Temperature
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            while (!request.isDone) yield return null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"OpenAI Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<OpenAIChatResponse>(request.downloadHandler.text);
                    if (response?.choices != null && response.choices.Count > 0)
                    {
                        onSuccess?.Invoke(response.choices[0].message.content.Trim());
                    }
                    else
                    {
                        onError?.Invoke($"OpenAI Error: Unexpected response format. Raw: {request.downloadHandler.text}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"OpenAI Error parsing response: {ex.Message}. Raw: {request.downloadHandler.text}");
                }
            }

            request.Dispose();
        }

        public IEnumerator TranslateBatch(string sourceLang, string targetLang, List<TranslationEntry> entries, Action<List<TranslationEntry>> onSuccess, Action<string> onError, string context = null)
        {
            if (entries == null || entries.Count == 0)
            {
                onSuccess?.Invoke(new List<TranslationEntry>());
                yield break;
            }

            var listJson = JsonUtility.ToJson(new BatchTranslationResult { results = entries });
            var contextPrompt = string.IsNullOrEmpty(context) ? "" : $" Context for translation: {context}.";
            var prompt = $"Translate the following JSON entries from {sourceLang} to {targetLang}.{contextPrompt} " +
                            "Return ONLY a JSON object with the key 'results' which is an array of objects with 'key' and 'value' (the translated text). " +
                            $"Entries: {listJson}";

            var requestData = new OpenAIChatRequest
            {
                model = _config.Model,
                messages = new List<OpenAIChatMessage>
                {
                    new OpenAIChatMessage { role = "user", content = prompt }
                },
                temperature = _config.Temperature,
                response_format = new OpenAIResponseFormat { type = "json_object" }
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            while (!request.isDone) yield return null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"OpenAI Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<OpenAIChatResponse>(request.downloadHandler.text);
                    if (response?.choices != null && response.choices.Count > 0)
                    {
                        var content = response.choices[0].message.content.Trim();
                        // Handle potential markdown block if AI ignores json_object mode (though unlikely with it enabled)
                        if (content.StartsWith("```json")) content = content.Substring(7);
                        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
                        content = content.Trim();

                        var result = JsonUtility.FromJson<BatchTranslationResult>(content);
                        onSuccess?.Invoke(result.results);
                    }
                    else
                    {
                        onError?.Invoke($"OpenAI Error: Unexpected response format. Raw: {request.downloadHandler.text}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"OpenAI Batch JSON Error: {ex.Message}\nRaw content: {request.downloadHandler.text}");
                }
            }

            request.Dispose();
        }

        [Serializable]
        private class OpenAIChatRequest
        {
            public string model;
            public List<OpenAIChatMessage> messages;
            public float temperature;
            public OpenAIResponseFormat response_format;
        }

        [Serializable]
        private class OpenAIChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class OpenAIResponseFormat
        {
            public string type;
        }

        [Serializable]
        private class OpenAIChatResponse
        {
            public List<OpenAIChoice> choices;
        }

        [Serializable]
        private class OpenAIChoice
        {
            public OpenAIChatMessage message;
        }
    }
}
