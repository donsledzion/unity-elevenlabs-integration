using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ElevenLabs.Zai;
using ElevenLabs.Gemini;

namespace ElevenLabs.Editor
{
    public class LocalizationSettingsProvider : SettingsProvider
    {
        private const string ElevenConfigPath = "Assets/LocalizationTools/Resources/ElevenLabsConfig.asset";
        private const string ZaiConfigPath = "Assets/LocalizationTools/Resources/ZaiConfig.asset";
        private const string GeminiConfigPath = "Assets/LocalizationTools/Resources/GeminiConfig.asset";
        
        private SerializedObject _serializedElevenConfig;
        private SerializedObject _serializedZaiConfig;
        private SerializedObject _serializedGeminiConfig;

        public LocalizationSettingsProvider(string path, SettingsScope scope = SettingsScope.Project)
            : base(path, scope) { }

        public override void OnActivate(string searchContext, UnityEngine.UIElements.VisualElement rootElement)
        {
            var elevenConfig = GetOrCreateConfig<ElevenLabsConfig>(ElevenConfigPath);
            if (elevenConfig != null) _serializedElevenConfig = new SerializedObject(elevenConfig);

            var zaiConfig = GetOrCreateConfig<ZaiConfig>(ZaiConfigPath);
            if (zaiConfig != null) _serializedZaiConfig = new SerializedObject(zaiConfig);

            var geminiConfig = GetOrCreateConfig<GeminiConfig>(GeminiConfigPath);
            if (geminiConfig != null) _serializedGeminiConfig = new SerializedObject(geminiConfig);
        }

        public override void OnGUI(string searchContext)
        {
            // --- TTS PROVIDERS ---
            EditorGUILayout.LabelField("TTS Providers", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();
            
            if (_serializedElevenConfig != null)
            {
                _serializedElevenConfig.Update();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("ElevenLabs Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedElevenConfig.FindProperty("apiKey"));
                EditorGUILayout.PropertyField(_serializedElevenConfig.FindProperty("defaultVoiceId"));
                EditorGUILayout.PropertyField(_serializedElevenConfig.FindProperty("defaultModelId"));
                EditorGUILayout.PropertyField(_serializedElevenConfig.FindProperty("outputFormat"));
                
                if (GUILayout.Button("Open ElevenLabs API Keys", GUILayout.Width(180)))
                    Application.OpenURL("https://elevenlabs.io/app/settings/api-keys");
                
                _serializedElevenConfig.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(20);

            // --- TRANSLATION PROVIDERS ---
            EditorGUILayout.LabelField("Translation Providers", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();

            if (_serializedGeminiConfig != null)
            {
                _serializedGeminiConfig.Update();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Google Gemini Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedGeminiConfig.FindProperty("apiKey"));
                EditorGUILayout.PropertyField(_serializedGeminiConfig.FindProperty("modelId"));
                EditorGUILayout.PropertyField(_serializedGeminiConfig.FindProperty("baseUrl"));
                EditorGUILayout.PropertyField(_serializedGeminiConfig.FindProperty("temperature"));

                if (GUILayout.Button("Get Gemini API Key", GUILayout.Width(180)))
                    Application.OpenURL("https://aistudio.google.com/app/apikey");

                _serializedGeminiConfig.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.Space(10);

            if (_serializedZaiConfig != null)
            {
                _serializedZaiConfig.Update();
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField("Z.ai (Zhipu) Settings", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedZaiConfig.FindProperty("apiKey"));
                EditorGUILayout.PropertyField(_serializedZaiConfig.FindProperty("baseUrl"));
                EditorGUILayout.PropertyField(_serializedZaiConfig.FindProperty("model"));

                if (GUILayout.Button("Z.ai Open Platform", GUILayout.Width(180)))
                    Application.OpenURL("https://open.bigmodel.cn/");

                _serializedZaiConfig.ApplyModifiedProperties();
                EditorGUILayout.EndVertical();
            }
        }

        private static T GetOrCreateConfig<T>(string path) where T : ScriptableObject
        {
            var config = AssetDatabase.LoadAssetAtPath<T>(path);
            if (config == null)
            {
                string directory = Path.GetDirectoryName(path);
                if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
                config = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
            }
            return config;
        }

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new LocalizationSettingsProvider("Project/AI Localization", SettingsScope.Project)
            {
                keywords = new HashSet<string>(new[] { "Localization", "Translation", "TTS", "Speech", "Gemini", "Zai", "ElevenLabs" })
            };
        }
    }
}
