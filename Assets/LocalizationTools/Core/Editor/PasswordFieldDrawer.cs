using UnityEditor;
using UnityEngine;
using ElevenLabs.Utils;

namespace ElevenLabs.Editor
{
    [CustomPropertyDrawer(typeof(PasswordFieldAttribute))]
    public class PasswordFieldDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            if (property.propertyType == SerializedPropertyType.String)
            {
                property.stringValue = EditorGUI.PasswordField(position, label, property.stringValue);
            }
            else
            {
                EditorGUI.LabelField(position, label.text, "Use PasswordField with string.");
            }
        }
    }
}
