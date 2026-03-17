using UnityEngine;

namespace ElevenLabs.Utils
{
    /// <summary>
    /// Attribute to mark a string field as a password/API key in the Inspector,
    /// masking its value with dots.
    /// </summary>
    public class PasswordFieldAttribute : PropertyAttribute
    {
    }
}
