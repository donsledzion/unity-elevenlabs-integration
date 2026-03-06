using System.Collections;
using UnityEditor;

namespace ElevenLabs.Editor
{
    public static class EditorCoroutineRunner
    {
        public static void StartCoroutine(IEnumerator routine, EditorWindow owner = null)
        {
            System.Collections.Generic.Stack<IEnumerator> stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);

            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                if (stack.Count == 0)
                {
                    EditorApplication.update -= update;
                    return;
                }

                IEnumerator top = stack.Peek();
                if (top.MoveNext())
                {
                    if (top.Current is IEnumerator nested)
                    {
                        stack.Push(nested);
                    }
                }
                else
                {
                    stack.Pop();
                }

                if (owner != null) owner.Repaint();
            };
            EditorApplication.update += update;
        }
    }
}
