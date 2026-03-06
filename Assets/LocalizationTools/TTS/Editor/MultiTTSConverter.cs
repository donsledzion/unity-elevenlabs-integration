using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
#if UNITY_LOCALIZATION
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEditor.Localization;
using ElevenLabs.Models;
#endif

namespace ElevenLabs.Editor
{
    public class MultiTTSConverter : EditorWindow
    {
        [MenuItem("Tools/Localization/Multi-TTS Converter")]
        public static void ShowWindow()
        {
            GetWindow<MultiTTSConverter>("Multi-TTS Converter");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Mass TTS Conversion Tool (ElevenLabs)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

#if !UNITY_LOCALIZATION
            EditorGUILayout.HelpBox("Unity Localization package is required for this tool.", MessageType.Error);
#else
            DrawTTSUI();
#endif
        }

#if UNITY_LOCALIZATION
        [SerializeField] private StringTable _sourceTable;
        [SerializeField] private AssetTable _targetTable;
        [SerializeField] private string _targetFolderPath = "Assets/LocalizationTools/Audio";
        [SerializeField] private bool _overwriteExisting = false;
        [SerializeField] private bool _showLocaleMapping = true;
        [SerializeField] private bool _showPaidVoices = false;

        private Dictionary<string, string> _localeVoiceMap = new Dictionary<string, string>();
        private List<ElevenLabsVoice> _availableVoices = new List<ElevenLabsVoice>();
        private string[] _filteredVoiceNames = new string[0];
        private string[] _filteredVoiceIds = new string[0];

        private void OnEnable()
        {
            if (_filteredVoiceIds == null || _filteredVoiceIds.Length == 0)
                UpdateFilteredVoices();
        }

        private void DrawTTSUI()
        {
            _sourceTable = (StringTable)EditorGUILayout.ObjectField("Source String Table", _sourceTable, typeof(StringTable), false);
            _targetTable = (AssetTable)EditorGUILayout.ObjectField("Target Asset Table", _targetTable, typeof(AssetTable), false);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            _targetFolderPath = EditorGUILayout.TextField("Target Folder", _targetFolderPath);
            if (GUILayout.Button("Browse", GUILayout.Width(60)))
            {
                string path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        _targetFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            _overwriteExisting = EditorGUILayout.Toggle("Overwrite Existing Assets", _overwriteExisting);

            EditorGUILayout.Space();
            _showLocaleMapping = EditorGUILayout.BeginFoldoutHeaderGroup(_showLocaleMapping, "Locale to Voice Mapping");
            if (_showLocaleMapping)
            {
                if (GUILayout.Button("Step 1: Fetch Available Voices")) FetchVoices();

                _showPaidVoices = EditorGUILayout.Toggle("Show Paid/Library Voices", _showPaidVoices);
                if (GUI.changed) UpdateFilteredVoices();

                EditorGUILayout.Space();

                foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    string code = locale.Identifier.Code;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(code, GUILayout.Width(60));
                    
                    if (!_localeVoiceMap.ContainsKey(code)) _localeVoiceMap[code] = "";

                    if (_filteredVoiceIds != null && _filteredVoiceIds.Length > 0 && _filteredVoiceIds[0] != "")
                    {
                        int currentIndex = Array.IndexOf(_filteredVoiceIds, _localeVoiceMap[code]);
                        if (currentIndex == -1) currentIndex = 0;

                        int newIndex = EditorGUILayout.Popup(currentIndex, _filteredVoiceNames);
                        _localeVoiceMap[code] = _filteredVoiceIds[newIndex];
                    }
                    else
                    {
                        _localeVoiceMap[code] = EditorGUILayout.TextField(_localeVoiceMap[code]);
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();

            EditorGUILayout.Space();
            if (GUILayout.Button("Action: Convert Table to Audio (ElevenLabs)", GUILayout.Height(40)))
            {
                PerformTTSConversion();
            }
        }

        private async void PerformTTSConversion()
        {
            if (_sourceTable == null || _targetTable == null)
            {
                Debug.LogError("Tables are not assigned!");
                return;
            }

            var config = AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>("Assets/LocalizationTools/Resources/ElevenLabsConfig.asset");
            if (config == null || string.IsNullOrEmpty(config.ApiKey))
            {
                Debug.LogError("ElevenLabs Configuration missing! Check Project Settings.");
                return;
            }

            var client = new ElevenLabsClient(config);
            var locales = LocalizationSettings.AvailableLocales.Locales;

            if (!System.IO.Directory.Exists(_targetFolderPath))
                System.IO.Directory.CreateDirectory(_targetFolderPath);

            Debug.Log($"Starting mass TTS conversion for {locales.Count} locales...");

            foreach (var locale in locales)
            {
                string langCode = locale.Identifier.Code;
                if (!_localeVoiceMap.ContainsKey(langCode))
                {
                    Debug.LogWarning($"Locale {langCode} not found in voice map. Skipping.");
                    continue;
                }

                string voiceId = _localeVoiceMap[langCode];
                if (string.IsNullOrEmpty(voiceId))
                {
                    Debug.LogWarning($"No voice ID assigned for locale {langCode}. Skipping.");
                    continue;
                }

                Debug.Log($"Processing locale: {langCode} with voice: {voiceId}");

                var stringTable = LocalizationSettings.StringDatabase.GetTable(_sourceTable.TableCollectionName, locale);
                if (stringTable == null)
                {
                    Debug.LogWarning($"String Table not found for locale {langCode}.");
                    continue;
                }

                var assetTable = LocalizationSettings.AssetDatabase.GetTable(_targetTable.TableCollectionName, locale) as AssetTable;
                if (assetTable == null)
                {
                    Debug.LogWarning($"Asset Table not found for locale {langCode}.");
                    continue;
                }

                var entries = stringTable.Values.ToList();
                Debug.Log($"Found {entries.Count} entries for {langCode}.");

                foreach (var entry in entries)
                {
                    if (string.IsNullOrEmpty(entry.Value)) continue;

                    var existingEntry = assetTable.GetEntry(entry.Key);
                    if (existingEntry != null && !_overwriteExisting)
                    {
                        Debug.Log($"Key '{entry.Key}' already has an asset. Skipping.");
                        continue;
                    }

                    string sanitizedLangCode = SanitizeLanguageCode(langCode);
                    await GenerateAndRegisterAudio(client, entry.Key, entry.Value, voiceId, sanitizedLangCode, langCode, assetTable);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("TTS Conversion process completed.");
        }

        private Task GenerateAndRegisterAudio(ElevenLabsClient client, string key, string text, string voiceId, string apiLangCode, string unityLangCode, AssetTable targetTable)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            EditorCoroutineRunner.StartCoroutine(client.TextToSpeech(text, voiceId, null, apiLangCode, new ElevenLabsVoiceSettings(), 
                (audioData) => 
                {
                    string fileName = $"{key}_{unityLangCode}.mp3";
                    string relativePath = System.IO.Path.Combine(_targetFolderPath, fileName).Replace("\\", "/");
                    string fullPath = System.IO.Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), relativePath);
                    
                    Debug.Log($"Writing audio file to: {fullPath}");
                    System.IO.File.WriteAllBytes(fullPath, audioData);
                    
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(relativePath);

                    ApplyAudioSettings(relativePath);

                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                    string guid = AssetDatabase.AssetPathToGUID(relativePath);
                    
                    Debug.Log($"Asset Data: Path={relativePath}, GUID={guid}, ClipValid={clip != null}");

                    if (!string.IsNullOrEmpty(guid))
                    {
                        var entry = targetTable.GetEntry(key) ?? targetTable.AddEntry(key, guid);
                        entry.Guid = guid;
                        EditorUtility.SetDirty(targetTable);
                        Debug.Log($"Successfully registered entry '{key}' in table '{targetTable.name}' with GUID: {guid}");
                    }
                    else
                    {
                        Debug.LogError($"Failed to get GUID for asset at {relativePath}. Check if file was created correctly.");
                    }
                    tcs.SetResult(true);
                }, 
                (error) => 
                {
                    Debug.LogError($"Error for {key}: {error}");
                    tcs.SetResult(false);
                }), this);

            return tcs.Task;
        }

        private string SanitizeLanguageCode(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return langCode;
            int dashIndex = langCode.IndexOf('-');
            if (dashIndex > 0) return langCode.Substring(0, dashIndex);
            int underscoreIndex = langCode.IndexOf('_');
            if (underscoreIndex > 0) return langCode.Substring(0, underscoreIndex);
            return langCode;
        }

        private void FetchVoices()
        {
            var config = AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>("Assets/LocalizationTools/Resources/ElevenLabsConfig.asset");
            if (config == null || string.IsNullOrEmpty(config.ApiKey)) return;

            var client = new ElevenLabsClient(config);
            EditorCoroutineRunner.StartCoroutine(client.GetVoices(
                (response) => 
                {
                    _availableVoices = response.voices;
                    UpdateFilteredVoices();
                    Repaint();
                },
                (error) => Debug.LogError(error)
            ), this);
        }

        private void UpdateFilteredVoices()
        {
            if (_availableVoices == null || _availableVoices.Count == 0)
            {
                _filteredVoiceNames = new[] { "No voices found. Please fetch." };
                _filteredVoiceIds = new[] { "" };
                return;
            }

            var filtered = _availableVoices
                .Where(v => {
                    string cat = (v.category ?? "").ToLower();
                    return _showPaidVoices || cat == "pre-made" || cat == "premade" || cat == "default";
                })
                .ToList();
            
            _filteredVoiceNames = filtered.Select(v => $"{v.name} ({v.category})").ToArray();
            _filteredVoiceIds = filtered.Select(v => v.voice_id).ToArray();
        }

        private void ApplyAudioSettings(string assetPath)
        {
            AudioImporter importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;
            importer.forceToMono = true;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            importer.defaultSampleSettings = settings;
            AssetDatabase.ImportAsset(assetPath);
        }
#endif
    }
}
