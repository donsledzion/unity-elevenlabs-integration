using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using ElevenLabs.Zai;
using ElevenLabs.Models;
using ElevenLabs.Gemini;
using ElevenLabs.OpenAI;
using ElevenLabs.Claude;
#endif

namespace ElevenLabs.Editor
{
    public class Multitranslator : EditorWindow
    {
        [MenuItem("Tools/Localization/Multitranslator")]
        public static void ShowWindow()
        {
            GetWindow<Multitranslator>("Multitranslator");
        }

        public enum TranslationProvider { Zai, Gemini, OpenAI, Claude }
        private TranslationProvider _provider = TranslationProvider.Gemini;

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mass Translation Tool", EditorStyles.boldLabel);
            EditorGUILayout.Space();

#if !UNITY_LOCALIZATION
            EditorGUILayout.HelpBox("Unity Localization package is required for this tool.", MessageType.Error);
#else
            DrawTranslatorUI();
#endif
        }

#if UNITY_LOCALIZATION
        [SerializeField] private StringTable _sourceTable;
        [SerializeField] private string _translationContext = "";
        private const int MaxContextLength = 1000;

        private void DrawTranslatorUI()
        {
            _sourceTable = (StringTable)EditorGUILayout.ObjectField("Source String Table", _sourceTable, typeof(StringTable), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Translation Settings", EditorStyles.boldLabel);
            _provider = (TranslationProvider)EditorGUILayout.EnumPopup("Provider", _provider);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Background Context (Optional, max {MaxContextLength} chars)");
            
            GUIStyle textAreaStyle = new GUIStyle(EditorStyles.textArea);
            textAreaStyle.wordWrap = true;
            _translationContext = EditorGUILayout.TextArea(_translationContext, textAreaStyle, GUILayout.Height(60));

            int currentLength = _translationContext?.Length ?? 0;
            GUIStyle counterStyle = new GUIStyle(EditorStyles.miniLabel);
            if (currentLength > MaxContextLength) counterStyle.normal.textColor = Color.red;
            
            EditorGUILayout.LabelField($"{currentLength} / {MaxContextLength}", counterStyle);

            bool isContextValid = currentLength <= MaxContextLength;
            if (currentLength > MaxContextLength)
            {
                EditorGUILayout.HelpBox($"Context is too long! Please reduce by {currentLength - MaxContextLength} characters.", MessageType.Warning);
            }

            EditorGUILayout.Space();
            GUI.enabled = isContextValid;
            if (GUILayout.Button($"Action: Perform Translations ({_provider})", GUILayout.Height(40)))
            {
                PerformTranslations();
            }
            GUI.enabled = true;
        }

        private async void PerformTranslations()
        {
            if (_sourceTable == null)
            {
                Debug.LogError("Source String Table is not assigned!");
                return;
            }

            var locales = LocalizationSettings.AvailableLocales.Locales;
            if (locales == null || locales.Count == 0)
            {
                var op = LocalizationSettings.InitializationOperation;
                while (!op.IsDone) System.Threading.Thread.Sleep(10);
                locales = LocalizationSettings.AvailableLocales.Locales;
            }

            Debug.Log($"Starting mass translation for table: {_sourceTable.TableCollectionName} using {_provider}");
            
            var collection = LocalizationEditorSettings.GetStringTableCollection(_sourceTable.TableCollectionName);
            if (collection == null)
            {
                Debug.LogError($"String Table Collection '{_sourceTable.TableCollectionName}' not found.");
                return;
            }

            foreach (var locale in locales)
            {
                if (locale.Identifier == _sourceTable.LocaleIdentifier) continue;

                var targetTable = collection.GetTable(locale.Identifier) as StringTable;
                if (targetTable == null) continue;

                List<TranslationEntry> batch = new List<TranslationEntry>();
                foreach (var entry in _sourceTable.Values)
                {
                    if (string.IsNullOrEmpty(entry.Value)) continue;

                    var targetEntry = targetTable.GetEntry(entry.Key);
                    if (targetEntry == null || string.IsNullOrEmpty(targetEntry.Value))
                    {
                        batch.Add(new TranslationEntry { key = entry.Key, value = entry.Value });
                    }
                }

                if (batch.Count == 0)
                {
                    Debug.Log($"All keys for {locale.Identifier.Code} are already translated.");
                    continue;
                }

                if (_provider == TranslationProvider.Gemini)
                {
                    var geminiConfig = AssetDatabase.LoadAssetAtPath<GeminiConfig>("Assets/LocalizationTools/Resources/GeminiConfig.asset");
                    if (geminiConfig == null || string.IsNullOrEmpty(geminiConfig.apiKey))
                    {
                        Debug.LogError("Gemini Configuration is missing or invalid! Check Project Settings.");
                        continue;
                    }

                    var geminiClient = new GeminiClient(geminiConfig);
                    var tcs = new TaskCompletionSource<bool>();
                    EditorCoroutineRunner.StartCoroutine(geminiClient.TranslateBatch(_sourceTable.LocaleIdentifier.Code, locale.Identifier.Code, batch, 
                        (results) => {
                            foreach (var res in results)
                            {
                                var e = targetTable.GetEntry(res.key) ?? targetTable.AddEntry(res.key, "");
                                e.Value = res.value;
                            }
                            EditorUtility.SetDirty(targetTable);
                            EditorUtility.SetDirty(collection);
                            Debug.Log($"Gemini: Translated {results.Count} keys for {locale.Identifier.Code}.");
                            tcs.SetResult(true);
                        },
                        (error) => {
                            Debug.LogError($"Gemini Error ({locale.Identifier.Code}): {error}");
                            tcs.SetResult(false);
                        }, _translationContext
                    ), this);
                    await tcs.Task;
                }
                else if (_provider == TranslationProvider.OpenAI)
                {
                    var openAIConfig = AssetDatabase.LoadAssetAtPath<OpenAIConfig>("Assets/LocalizationTools/Resources/OpenAIConfig.asset");
                    if (openAIConfig == null || !openAIConfig.IsValid)
                    {
                        Debug.LogError("OpenAI Configuration is missing or invalid!");
                        continue;
                    }

                    var openAIClient = new OpenAIClient(openAIConfig);
                    var tcs = new TaskCompletionSource<bool>();
                    EditorCoroutineRunner.StartCoroutine(openAIClient.TranslateBatch(_sourceTable.LocaleIdentifier.Code, locale.Identifier.Code, batch, 
                        (results) => {
                            foreach (var res in results)
                            {
                                var e = targetTable.GetEntry(res.key) ?? targetTable.AddEntry(res.key, "");
                                e.Value = res.value;
                            }
                            EditorUtility.SetDirty(targetTable);
                            EditorUtility.SetDirty(collection);
                            Debug.Log($"OpenAI: Translated {results.Count} keys for {locale.Identifier.Code}.");
                            tcs.SetResult(true);
                        },
                        (error) => {
                            Debug.LogError($"OpenAI Error ({locale.Identifier.Code}): {error}");
                            tcs.SetResult(false);
                        }, _translationContext
                    ), this);
                    await tcs.Task;
                }
                else if (_provider == TranslationProvider.Claude)
                {
                    var claudeConfig = AssetDatabase.LoadAssetAtPath<ClaudeConfig>("Assets/LocalizationTools/Resources/ClaudeConfig.asset");
                    if (claudeConfig == null || !claudeConfig.IsValid)
                    {
                        Debug.LogError("Claude Configuration is missing or invalid!");
                        continue;
                    }

                    var claudeClient = new ClaudeClient(claudeConfig);
                    var tcs = new TaskCompletionSource<bool>();
                    EditorCoroutineRunner.StartCoroutine(claudeClient.TranslateBatch(_sourceTable.LocaleIdentifier.Code, locale.Identifier.Code, batch, 
                        (results) => {
                            foreach (var res in results)
                            {
                                var e = targetTable.GetEntry(res.key) ?? targetTable.AddEntry(res.key, "");
                                e.Value = res.value;
                            }
                            EditorUtility.SetDirty(targetTable);
                            EditorUtility.SetDirty(collection);
                            Debug.Log($"Claude: Translated {results.Count} keys for {locale.Identifier.Code}.");
                            tcs.SetResult(true);
                        },
                        (error) => {
                            Debug.LogError($"Claude Error ({locale.Identifier.Code}): {error}");
                            tcs.SetResult(false);
                        }, _translationContext
                    ), this);
                    await tcs.Task;
                }
                else if (_provider == TranslationProvider.Zai)
                {
                    var zaiConfig = AssetDatabase.LoadAssetAtPath<ZaiConfig>("Assets/LocalizationTools/Resources/ZaiConfig.asset");
                    if (zaiConfig == null || string.IsNullOrEmpty(zaiConfig.ApiKey))
                    {
                        Debug.LogError("Z.ai Configuration is missing or invalid! Check Project Settings.");
                        continue;
                    }

                    var zaiClient = new ZaiClient(zaiConfig);
                    var tcs = new TaskCompletionSource<bool>();
                    EditorCoroutineRunner.StartCoroutine(zaiClient.TranslateBatch(_sourceTable.LocaleIdentifier.Code, locale.Identifier.Code, batch, 
                        (results) => {
                            foreach (var res in results)
                            {
                                var e = targetTable.GetEntry(res.key) ?? targetTable.AddEntry(res.key, "");
                                e.Value = res.value;
                            }
                            EditorUtility.SetDirty(targetTable);
                            EditorUtility.SetDirty(collection);
                            Debug.Log($"Z.ai: Translated {results.Count} keys for {locale.Identifier.Code}.");
                            tcs.SetResult(true);
                        },
                        (error) => {
                            Debug.LogError($"Z.ai Error ({locale.Identifier.Code}): {error}");
                            tcs.SetResult(false);
                        }, _translationContext
                    ), this);
                    await tcs.Task;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Translation process completed.");
        }
#endif
    }
}
