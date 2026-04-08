using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ElevenLabs.Models;
using ElevenLabs.Core;

namespace ElevenLabs.Zai
{
    public class ZaiClient : ITranslationProvider
    {
        private readonly ZaiConfig _config;

        public ZaiClient(ZaiConfig config)
        {
            _config = config;
        }

        public IEnumerator Translate(string text, string targetLanguage, Action<string> onSuccess, Action<string> onError, string context = null)
        {
            if (!_config.IsValid)
            {
                onError?.Invoke("Z.ai API Key is missing!");
                yield break;
            }

            var requestData = new ZaiChatRequest
            {
                model = _config.Model,
                messages = new[]
                {
                    new ZaiMessage { role = "user", content = $"Translate the following text to {targetLanguage}. {(string.IsNullOrEmpty(context) ? "" : $"Context: {context}. ")}ONLY return the translated text without any explanations or extra characters.\n\nText: {text}" }
                },
                temperature = 0.1f // Low temperature for stable translation
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return SendRequest(request, (responseText) => 
            {
                try
                {
                    Debug.Log($"Z.ai Raw Response: {responseText}");
                    var response = JsonUtility.FromJson<ZaiChatResponse>(responseText);
                    if (response?.choices != null && response.choices.Length > 0)
                    {
                        onSuccess?.Invoke(response.choices[0].message.content.Trim());
                    }
                    else
                    {
                        onError?.Invoke($"Z.ai Error: Unexpected response format or empty choices. Raw: {responseText}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Z.ai Error parsing response: {ex.Message}. Raw: {responseText}");
                }
            }, onError);
        }

        public IEnumerator TranslateBatch(string sourceLang, string targetLang, List<TranslationEntry> entries, Action<List<TranslationEntry>> onSuccess, Action<string> onError, string context = null)
        {
            if (entries == null || entries.Count == 0)
            {
                onSuccess?.Invoke(new List<TranslationEntry>());
                yield break;
            }

            string listJson = JsonUtility.ToJson(new BatchTranslationResult { results = entries });
            string contextPrompt = string.IsNullOrEmpty(context) ? "" : $" Context for translation: {context}.";
            string prompt = $"Translate the following JSON entries from {sourceLang} to {targetLang}.{contextPrompt} " +
                            "Return ONLY a JSON object with the key 'results' which is an array of objects with 'key' and 'value' (the translated text). " +
                            $"Entries: {listJson}";

            var requestData = new ZaiChatRequest
            {
                model = _config.Model,
                messages = new[]
                {
                    new ZaiMessage { role = "user", content = prompt }
                },
                temperature = 0.1f
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", $"Bearer {_config.ApiKey}");
            request.SetRequestHeader("Content-Type", "application/json");

            yield return SendRequest(request, (responseText) => 
            {
                try
                {
                    var response = JsonUtility.FromJson<ZaiChatResponse>(responseText);
                    if (response?.choices != null && response.choices.Length > 0)
                    {
                        var content = response.choices[0].message.content.Trim();
                        // Handle potential markdown block from LLM
                        if (content.StartsWith("```json")) content = content.Substring(7);
                        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
                        content = content.Trim();

                        var result = JsonUtility.FromJson<BatchTranslationResult>(content);
                        onSuccess?.Invoke(result.results);
                    }
                    else
                    {
                        onError?.Invoke($"Z.ai Error: Unexpected response format. Raw: {responseText}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Z.ai Batch JSON Error: {ex.Message}\nRaw content: {responseText}");
                }
            }, onError);
        }

        private IEnumerator SendRequest(UnityWebRequest request, Action<string> onSuccess, Action<string> onError)
        {
            request.SendWebRequest();

            while (!request.isDone)
            {
                yield return null;
            }

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Z.ai Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                onSuccess?.Invoke(request.downloadHandler.text);
            }

            request.Dispose();
        }

        [Serializable]
        private class ZaiChatRequest
        {
            public string model;
            public ZaiMessage[] messages;
            public float temperature;
        }

        [Serializable]
        private class ZaiMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ZaiChatResponse
        {
            public ZaiChoice[] choices;
        }

        [Serializable]
        private class ZaiChoice
        {
            public ZaiMessage message;
        }
    }
}
