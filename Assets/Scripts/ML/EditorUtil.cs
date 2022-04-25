#if (UNITY_EDITOR)
using UnityEditor;
using UnityEngine;

namespace DroneProject
{
    public static class EditorUtil
    {
        /// <summary>
        /// Hides the BehaviorParametersEditor inspector.
        /// </summary>
        // Workaround for https://github.com/Unity-Technologies/ml-agents/issues/5443
        public static void HideBehaviorParametersEditor()
        {
            var tracker = ActiveEditorTracker.sharedTracker;
            var editors = tracker.activeEditors;
            bool warn = false;

            // Can't check type because BehaviorParametersEditor is internal.
            const string type = " (Unity.MLAgents.Editor.BehaviorParametersEditor)";
            
            for (int i = 0; i < editors.Length; i++)
            {
                if (tracker.GetVisible(i) != 0 && editors[i] != null && editors[i].ToString() == type)
                {
                    tracker.SetVisible(i, 0);
                    warn = true;
                }
            }

            if (warn)
            {
                Debug.LogWarning("Hiding behavior parameters inspector in order " + 
                                 "to prevent repeated sensor initialization");
            }
        }
    }
}
#endif