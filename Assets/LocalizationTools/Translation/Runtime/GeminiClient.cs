using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using ElevenLabs.Models;
using ElevenLabs.Core;

namespace ElevenLabs.Gemini
{
// Local models removed, using ElevenLabs.Models

    [Serializable]
    public class GeminiRequest
    {
        public List<Content> contents;
        public GenerationConfig generationConfig;

        [Serializable]
        public class Content
        {
            public List<Part> parts;
        }

        [Serializable]
        public class Part
        {
            public string text;
        }

        [Serializable]
        public class GenerationConfig
        {
            public float temperature;
            public string responseMimeType = "application/json";
        }
    }

    [Serializable]
    public class GeminiResponse
    {
        public List<Candidate> candidates;

        [Serializable]
        public class Candidate
        {
            public Content content;
        }

        [Serializable]
        public class Content
        {
            public List<Part> parts;
        }

        [Serializable]
        public class Part
        {
            public string text;
        }
    }

    public class GeminiClient : ITranslationProvider
    {
        private GeminiConfig _config;

        public GeminiClient(GeminiConfig config)
        {
            _config = config;
        }

        public IEnumerator Translate(string text, string targetLanguage, Action<string> onSuccess, Action<string> onError, string context = null)
        {
            var entries = new List<TranslationEntry> { new TranslationEntry { key = "single", value = text } };
            return TranslateBatch("", targetLanguage, entries, 
                (results) => onSuccess?.Invoke(results[0].value), 
                onError, context);
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

            var requestBody = new GeminiRequest
            {
                contents = new List<GeminiRequest.Content>
                {
                    new GeminiRequest.Content
                    {
                        parts = new List<GeminiRequest.Part>
                        {
                            new GeminiRequest.Part { text = prompt }
                        }
                    }
                },
                generationConfig = new GeminiRequest.GenerationConfig
                {
                    temperature = _config.temperature
                }
            };

            string json = JsonUtility.ToJson(requestBody);
            
            string baseUrl = (_config.baseUrl ?? "").Trim();
            string modelId = (_config.modelId ?? "").Trim();
            string apiKey = (_config.apiKey ?? "").Trim();

            if (!baseUrl.EndsWith("/models")) baseUrl += "/models";
            string url = $"{baseUrl}/{modelId}:generateContent?key={apiKey}";
            
            Debug.Log($"Gemini Request URL: {baseUrl}/{modelId}:generateContent?key=***");

            var request = new UnityWebRequest(url, "POST");
            byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            while (!request.isDone) yield return null;

            if (request.result != UnityWebRequest.Result.Success)
            {
                onError?.Invoke($"Gemini Error: {request.error}\nRaw: {request.downloadHandler.text}");
            }
            else
            {
                try
                {
                    var response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                    if (response?.candidates != null && response.candidates.Count > 0)
                    {
                        string content = response.candidates[0].content.parts[0].text;
                        Debug.Log($"Gemini Raw Response Part: {content}");
                        
                        // Handle potential markdown block from LLM
                        if (content.StartsWith("```json")) content = content.Substring(7);
                        if (content.EndsWith("```")) content = content.Substring(0, content.Length - 3);
                        content = content.Trim();

                        var result = JsonUtility.FromJson<BatchTranslationResult>(content);
                        onSuccess?.Invoke(result.results);
                    }
                    else
                    {
                        onError?.Invoke($"Gemini Error: No candidates returned. This might be due to safety filters.\nRaw: {request.downloadHandler.text}");
                    }
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Gemini JSON Error: {ex.Message}\nRaw content: {request.downloadHandler.text}");
                }
            }
            
            request.Dispose();
        }
    }
}
