using System.Collections;
using UnityEditor;
using UnityEngine;

namespace ElevenLabs.Editor
{
    public static class EditorCoroutineRunner
    {
        public static void StartCoroutine(IEnumerator routine, EditorWindow owner = null)
        {
            var stack = new System.Collections.Generic.Stack<IEnumerator>();
            stack.Push(routine);

            EditorApplication.CallbackFunction update = null;
            update = () =>
            {
                if (stack.Count == 0)
                {
                    EditorApplication.update -= update;
                    return;
                }

                var top = stack.Peek();

                if (top.Current is AsyncOperation op && !op.isDone)
                {
                    return;
                }

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
