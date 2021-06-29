using UnityEngine;
using UnityEditor;
using TMPro;
using UnityEngine.Playables;

[System.Serializable]
public class DialogueClip : PlayableAsset
{
    public DialogueBehaviour Behaviour = new DialogueBehaviour();

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        return ScriptPlayable<DialogueBehaviour>.Create(graph, Behaviour);
    }

    [CustomEditor(typeof(DialogueClip))]
    public class DialogueClipEditor : Editor
    {
        public DialogueClip Target = null;

        private void Awake()
        {
            Target = (DialogueClip)target;
        }

        public override void OnInspectorGUI()
        {
            Target.Behaviour.Text = EditorGUILayout.TextField("Text:", Target.Behaviour.Text);
            Target.Behaviour.hasPause = EditorGUILayout.Toggle("Pause At End:", Target.Behaviour.hasPause);
        }
    }
}
