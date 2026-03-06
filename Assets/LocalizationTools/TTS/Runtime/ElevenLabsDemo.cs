using UnityEngine;
using ElevenLabs;

namespace ElevenLabs.Demo
{
    public class ElevenLabsDemo : MonoBehaviour
    {
        [SerializeField] private ElevenLabsTTS tts;
        [SerializeField] [TextArea] private string textToConvert = "Witaj w świecie ElevenLabs! To powitanie zostało wygenerowane automatycznie.";

        public void RunDemo()
        {
            if (tts == null)
            {
                tts = GetComponent<ElevenLabsTTS>();
            }

            if (tts != null)
            {
                Debug.Log("Starting TTS conversion...");
                tts.ConvertTextToSpeech(textToConvert, 
                    (path) => {
                        Debug.Log($"Demo successful! File at: {path}");
                    },
                    (error) => {
                        Debug.LogError($"Demo failed: {error}");
                    });
            }
            else
            {
                Debug.LogError("ElevenLabsTTS component not found!");
            }
        }
    }
}
