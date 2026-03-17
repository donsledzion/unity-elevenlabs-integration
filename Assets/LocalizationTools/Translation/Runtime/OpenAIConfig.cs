using UnityEngine;

namespace ElevenLabs.OpenAI
{
    [CreateAssetMenu(fileName = "OpenAIConfig", menuName = "ElevenLabs/OpenAI Config")]
    public class OpenAIConfig : ScriptableObject
    {
        [SerializeField] private string apiKey;
        [SerializeField] private string baseUrl = "https://api.openai.com/v1/chat/completions";
        [SerializeField] private string model = "gpt-4o";
        [SerializeField] [Range(0f, 2f)] private float temperature = 0.1f;

        public string ApiKey => apiKey;
        public string BaseUrl => baseUrl;
        public string Model => model;
        public float Temperature => temperature;

        public bool IsValid => !string.IsNullOrEmpty(apiKey);
    }
}
