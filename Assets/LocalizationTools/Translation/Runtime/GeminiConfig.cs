using UnityEngine;
using ElevenLabs.Utils;

namespace ElevenLabs.Gemini
{
    [CreateAssetMenu(fileName = "GeminiConfig", menuName = "ElevenLabs/Gemini Config")]
    public class GeminiConfig : ScriptableObject
    {
        [PasswordField]
        public string apiKey;
        public string modelId = "gemini-2.5-flash";
        public float temperature = 0.2f;
        public string baseUrl = "https://generativelanguage.googleapis.com/v1beta";
    }
}
