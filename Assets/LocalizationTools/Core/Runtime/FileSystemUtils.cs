using System.IO;
using UnityEngine;

namespace ElevenLabs.Utils
{
    public static class FileSystemUtils
    {
        public static string SaveAudioFile(byte[] data, string fileName, string folderName = "ElevenLabsAudio")
        {
            // Save inside the project's Assets folder so it's easier to find and use
            string directoryPath = Path.Combine(Application.dataPath, folderName);
            
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            string fullPath = Path.Combine(directoryPath, fileName);
            File.WriteAllBytes(fullPath, data);

#if UNITY_EDITOR
            // Refresh AssetDatabase to show the new file in the project window
            UnityEditor.AssetDatabase.Refresh();
#endif
            
            return fullPath;
        }

        public static string GetSafeFileName(string text)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            string safeText = string.Join("_", text.Split(invalidChars, System.StringSplitOptions.RemoveEmptyEntries)).Trim();
            
            if (safeText.Length > 20)
            {
                safeText = safeText.Substring(0, 20);
            }
            
            return $"{safeText}_{System.DateTime.Now:yyyyMMdd_HHmmss}.mp3";
        }
    }
}
