using UnityEngine;
using ElevenLabs.Utils;

namespace ElevenLabs.Claude
{
    [CreateAssetMenu(fileName = "ClaudeConfig", menuName = "ElevenLabs/Claude Config")]
    public class ClaudeConfig : ScriptableObject
    {
        [SerializeField, PasswordField] private string apiKey;
        [SerializeField] private string baseUrl = "https://api.anthropic.com/v1/messages";
        [SerializeField] private string model = "claude-3-5-sonnet-20240620";
        [SerializeField] [Range(0f, 1f)] private float temperature = 0.1f;
        [SerializeField] private string anthropicVersion = "2023-06-01";
        [SerializeField] private int maxTokens = 4096;

        public string ApiKey => apiKey;
        public string BaseUrl => baseUrl;
        public string Model => model;
        public float Temperature => temperature;
        public string AnthropicVersion => anthropicVersion;
        public int MaxTokens => maxTokens;

        public bool IsValid => !string.IsNullOrEmpty(apiKey);
    }
}
