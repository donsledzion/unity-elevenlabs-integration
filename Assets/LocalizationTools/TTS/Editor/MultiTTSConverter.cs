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

        public enum TTSOverwriteMode { All, MissingOnly }
        public enum TTSMappingMode { ByKey, ByIndex }

        [SerializeField] private StringTable _sourceTable;
        [SerializeField] private AssetTable _targetTable;
        [SerializeField] private string _targetFolderPath = "Assets/LocalizationTools/Audio";
        [SerializeField] private TTSOverwriteMode _overwriteMode = TTSOverwriteMode.MissingOnly;
        [SerializeField] private TTSMappingMode _mappingMode = TTSMappingMode.ByKey;
        [SerializeField] private bool _makeAddressable = true;
        [SerializeField] private bool _simplifyAddressableKey = true;
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
                var path = EditorUtility.OpenFolderPanel("Select Target Folder", "Assets", "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.StartsWith(Application.dataPath))
                        _targetFolderPath = "Assets" + path.Substring(Application.dataPath.Length);
                }
            }
            EditorGUILayout.EndHorizontal();

            _mappingMode = (TTSMappingMode)EditorGUILayout.EnumPopup("Mapping Mode", _mappingMode);
            _overwriteMode = (TTSOverwriteMode)EditorGUILayout.EnumPopup("Overwrite Mode", _overwriteMode);

            EditorGUILayout.Space();
            _makeAddressable = EditorGUILayout.Toggle("Set Files as Addressable", _makeAddressable);
            EditorGUI.indentLevel++;
            using (new EditorGUI.DisabledGroupScope(!_makeAddressable))
            {
                _simplifyAddressableKey = EditorGUILayout.Toggle("Simplify Addressable Key", _simplifyAddressableKey);
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.Space();
            _showLocaleMapping = EditorGUILayout.BeginFoldoutHeaderGroup(_showLocaleMapping, "Locale to Voice Mapping");
            if (_showLocaleMapping)
            {
                if (GUILayout.Button("Step 1: Fetch Available Voices")) FetchVoices();

                _showPaidVoices = EditorGUILayout.Toggle("Show Paid/Library Voices", _showPaidVoices);
                if (GUI.changed) UpdateFilteredVoices();

                EditorGUILayout.Space();

                var locales = LocalizationSettings.AvailableLocales.Locales;
                foreach (var locale in locales)
                {
                    var code = locale.Identifier.Code;
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField(code, GUILayout.Width(60));
                    
                    if (!_localeVoiceMap.ContainsKey(code)) _localeVoiceMap[code] = "";

                    if (_filteredVoiceIds != null && _filteredVoiceIds.Length > 0 && _filteredVoiceIds[0] != "")
                    {
                        var currentIndex = Array.IndexOf(_filteredVoiceIds, _localeVoiceMap[code]);
                        if (currentIndex == -1) currentIndex = 0;

                        var newIndex = EditorGUILayout.Popup(currentIndex, _filteredVoiceNames);
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

            // Get shared data to access keys in order
            var sourceSharedData = _sourceTable.SharedData;
            var targetSharedData = _targetTable.SharedData;

            if (sourceSharedData == null || targetSharedData == null)
            {
                Debug.LogError("Shared Table Data missing for one of the tables!");
                return;
            }

            var sourceSharedEntries = sourceSharedData.Entries;
            var targetSharedEntries = targetSharedData.Entries;

            Debug.Log($"Starting mass TTS conversion ({_mappingMode} / {_overwriteMode}) for {locales.Count} locales...");

            foreach (var locale in locales)
            {
                var langCode = locale.Identifier.Code;
                if (!_localeVoiceMap.ContainsKey(langCode))
                {
                    Debug.LogWarning($"Locale {langCode} not found in voice map. Skipping.");
                    continue;
                }

                var voiceId = _localeVoiceMap[langCode];
                if (string.IsNullOrEmpty(voiceId))
                {
                    Debug.LogWarning($"No voice ID assigned for locale {langCode}. Skipping.");
                    continue;
                }

                var stringTable = LocalizationSettings.StringDatabase.GetTable(_sourceTable.TableCollectionName, locale);
                var assetTable = LocalizationSettings.AssetDatabase.GetTable(_targetTable.TableCollectionName, locale) as AssetTable;

                if (stringTable == null || assetTable == null)
                {
                    Debug.LogWarning($"Table(s) not found for locale {langCode}. Skipping.");
                    continue;
                }

                if (_mappingMode == TTSMappingMode.ByIndex)
                {
                    var count = Math.Min(sourceSharedEntries.Count, targetSharedEntries.Count);
                    Debug.Log($"Processing {count} entries via Index Match for {langCode}.");

                    for (var i = 0; i < count; i++)
                    {
                        var sourceKey = sourceSharedEntries[i].Key;
                        var targetKey = targetSharedEntries[i].Key;

                        var sourceEntry = stringTable.GetEntry(sourceKey);
                        if (sourceEntry == null || string.IsNullOrEmpty(sourceEntry.Value)) continue;

                        var targetEntry = assetTable.GetEntry(targetKey);
                        if (_overwriteMode == TTSOverwriteMode.MissingOnly && targetEntry != null && !string.IsNullOrEmpty(targetEntry.Guid))
                        {
                            continue;
                        }

                        var sanitizedLangCode = SanitizeLanguageCode(langCode);
                        await GenerateAndRegisterAudio(client, targetKey, sourceEntry.Value, voiceId, sanitizedLangCode, langCode, assetTable);
                    }
                }
                else // By Key
                {
                    Debug.Log($"Processing {sourceSharedEntries.Count} entries via Key Match for {langCode}.");
                    foreach (var sharedSource in sourceSharedEntries)
                    {
                        var key = sharedSource.Key;
                        var sourceEntry = stringTable.GetEntry(key);
                        if (sourceEntry == null || string.IsNullOrEmpty(sourceEntry.Value)) continue;

                        var targetEntry = assetTable.GetEntry(key);
                        if (_overwriteMode == TTSOverwriteMode.MissingOnly && targetEntry != null && !string.IsNullOrEmpty(targetEntry.Guid))
                        {
                            continue;
                        }

                        var sanitizedLangCode = SanitizeLanguageCode(langCode);
                        await GenerateAndRegisterAudio(client, key, sourceEntry.Value, voiceId, sanitizedLangCode, langCode, assetTable);
                    }
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
                    var fileName = $"{key}_{unityLangCode}.mp3";
                    var relativePath = System.IO.Path.Combine(_targetFolderPath, fileName).Replace("\\", "/");
                    var fullPath = System.IO.Path.Combine(Application.dataPath.Substring(0, Application.dataPath.Length - 6), relativePath);
                    
                    System.IO.File.WriteAllBytes(fullPath, audioData);
                    
                    AssetDatabase.Refresh();
                    AssetDatabase.ImportAsset(relativePath);

                    ApplyAudioSettings(relativePath);

                    var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                    var guid = AssetDatabase.AssetPathToGUID(relativePath);
                    
                    if (!string.IsNullOrEmpty(guid))
                    {
                        var entry = targetTable.GetEntry(key) ?? targetTable.AddEntry(key, guid);
                        entry.Guid = guid;
                        EditorUtility.SetDirty(targetTable);
                        Debug.Log($"Successfully registered entry '{key}' in table '{targetTable.name}' with GUID: {guid}");

                        RegisterAddressable(relativePath, guid);
                    }
                    else
                    {
                        Debug.LogError($"Failed to get GUID for asset at {relativePath}.");
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

        private void RegisterAddressable(string assetPath, string guid)
        {
#if UNITY_ADDRESSABLES
            if (!_makeAddressable) return;

            var settings = UnityEditor.AddressableAssets.AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null) return;

            var entry = settings.CreateOrMoveEntry(guid, settings.DefaultGroup);
            if (entry != null && _simplifyAddressableKey)
            {
                entry.address = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            }
#endif
        }

        private string SanitizeLanguageCode(string langCode)
        {
            if (string.IsNullOrEmpty(langCode)) return langCode;
            var dashIndex = langCode.IndexOf('-');
            if (dashIndex > 0) return langCode.Substring(0, dashIndex);
            var underscoreIndex = langCode.IndexOf('_');
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
                    var cat = (v.category ?? "").ToLower();
                    return _showPaidVoices || cat == "pre-made" || cat == "premade" || cat == "default";
                })
                .ToList();
            
            _filteredVoiceNames = filtered.Select(v => $"{v.name} ({v.category})").ToArray();
            _filteredVoiceIds = filtered.Select(v => v.voice_id).ToArray();
        }

        private void ApplyAudioSettings(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as AudioImporter;
            if (importer == null) return;
            importer.forceToMono = true;
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.CompressedInMemory;
            settings.compressionFormat = AudioCompressionFormat.ADPCM;
            importer.defaultSampleSettings = settings;
            AssetDatabase.ImportAsset(assetPath);
        }
#endif
    }
}
