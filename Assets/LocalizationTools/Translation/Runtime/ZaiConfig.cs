using UnityEngine;
using ElevenLabs.Utils;

namespace ElevenLabs.Zai
{
    [CreateAssetMenu(fileName = "ZaiConfig", menuName = "ElevenLabs/Z.ai Config")]
    public class ZaiConfig : ScriptableObject
    {
        [SerializeField, PasswordField] private string apiKey;
        [SerializeField] private string baseUrl = "https://open.bigmodel.cn/api/paas/v4/chat/completions";
        [SerializeField] private string model = "glm-4.7-flash";
        
        public string ApiKey => apiKey;
        public string BaseUrl => baseUrl;
        public string Model => model;

        public bool IsValid => !string.IsNullOrEmpty(apiKey);
    }
}
