using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ElevenLabs.Models;
using ElevenLabs.Core;

namespace ElevenLabs.Claude
{
    public class ClaudeClient : ITranslationProvider
    {
        private readonly ClaudeConfig _config;

        public ClaudeClient(ClaudeConfig config)
        {
            _config = config;
        }

        public IEnumerator Translate(string text, string targetLanguage, Action<string> onSuccess, Action<string> onError, string context = null)
        {
            if (!_config.IsValid)
            {
                onError?.Invoke("Claude API Key is missing!");
                yield break;
            }

            var requestData = new ClaudeRequest
            {
                model = _config.Model,
                max_tokens = _config.MaxTokens,
                temperature = _config.Temperature,
                messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { role = "user", content = $"Translate the following text to {targetLanguage}. {(string.IsNullOrEmpty(context) ? "" : $"Context: {context}. ")}ONLY return the translated text without any explanations or extra characters.\n\nText: {text}" }
                }
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("x-api-key", _config.ApiKey);
            request.SetRequestHeader("anthropic-version", _config.AnthropicVersion);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            while (!request.isDone) yield return null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Claude Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<ClaudeResponse>(request.downloadHandler.text);
                    if (response?.content != null && response.content.Count > 0)
                    {
                        onSuccess?.Invoke(response.content[0].text.Trim());
                    }
                    else
                    {
                        onError?.Invoke($"Claude Error: Unexpected response format. Raw: {request.downloadHandler.text}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Claude Error parsing response: {ex.Message}. Raw: {request.downloadHandler.text}");
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

            var requestData = new ClaudeRequest
            {
                model = _config.Model,
                max_tokens = _config.MaxTokens,
                temperature = _config.Temperature,
                messages = new List<ClaudeMessage>
                {
                    new ClaudeMessage { role = "user", content = prompt }
                }
            };

            var json = JsonUtility.ToJson(requestData);
            var request = new UnityWebRequest(_config.BaseUrl, "POST");
            var bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("x-api-key", _config.ApiKey);
            request.SetRequestHeader("anthropic-version", _config.AnthropicVersion);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            while (!request.isDone) yield return null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Claude Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<ClaudeResponse>(request.downloadHandler.text);
                    if (response?.content != null && response.content.Count > 0)
                    {
                        var content = response.content[0].text.Trim();
                        // Handle potential markdown block from LLM
                        if (content.StartsWith("```json")) content = content.Substring(7);
                        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
                        content = content.Trim();

                        var result = JsonUtility.FromJson<BatchTranslationResult>(content);
                        onSuccess?.Invoke(result.results);
                    }
                    else
                    {
                        onError?.Invoke($"Claude Error: Unexpected response format. Raw: {request.downloadHandler.text}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Claude Batch JSON Error: {ex.Message}\nRaw content: {request.downloadHandler.text}");
                }
            }

            request.Dispose();
        }

        [Serializable]
        private class ClaudeRequest
        {
            public string model;
            public int max_tokens;
            public float temperature;
            public List<ClaudeMessage> messages;
        }

        [Serializable]
        private class ClaudeMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private class ClaudeResponse
        {
            public List<ClaudeResponseContent> content;
        }

        [Serializable]
        private class ClaudeResponseContent
        {
            public string type;
            public string text;
        }
    }
}
