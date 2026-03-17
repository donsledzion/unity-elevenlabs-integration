using UnityEngine;
using ElevenLabs.Utils;

namespace ElevenLabs
{
    [CreateAssetMenu(fileName = "ElevenLabsConfig", menuName = "ElevenLabs/Config")]
    public class ElevenLabsConfig : ScriptableObject
    {
        [Header("API Authentication")]
        [SerializeField, PasswordField] private string apiKey;
        
        [Header("Default Settings")]
        [SerializeField] private string defaultVoiceId = "21m00Tcm4TlvDq8ikWAM"; // Rachel
        [SerializeField] private string defaultModelId = "eleven_multilingual_v2";
        
        [Header("Audio Settings")]
        [SerializeField] private string outputFormat = "mp3_44100_128";

        public string ApiKey { get => apiKey; set => apiKey = value; }
        public string DefaultVoiceId { get => defaultVoiceId; set => defaultVoiceId = value; }
        public string DefaultModelId { get => defaultModelId; set => defaultModelId = value; }
        public string OutputFormat { get => outputFormat; set => outputFormat = value; }
    }
}
