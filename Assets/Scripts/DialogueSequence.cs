using System;
using UnityEngine;

[Serializable]
public class DialogueEntry
{
    [SerializeField] private string speaker;
    [TextArea(2, 5)] [SerializeField] private string text;
    [SerializeField] private Sprite portrait;

    public string Speaker => speaker;
    public string Text => text;
    public Sprite Portrait => portrait;

    public DialogueSystem.DialogueLine ToDialogueLine()
    {
        return new DialogueSystem.DialogueLine(speaker, text, portrait);
    }
}

[CreateAssetMenu(menuName = "Parallax/Dialogue Sequence")]
public class DialogueSequence : ScriptableObject
{
    [SerializeField] private DialogueEntry[] entries;
    public DialogueEntry[] Entries => entries ?? Array.Empty<DialogueEntry>();

    public DialogueSystem.DialogueLine[] ToDialogueLines()
    {
        DialogueSystem.DialogueLine[] result = new DialogueSystem.DialogueLine[Entries.Length];
        for (int i = 0; i < result.Length; i++)
            result[i] = Entries[i] != null ? Entries[i].ToDialogueLine() : new DialogueSystem.DialogueLine(string.Empty, string.Empty);
        return result;
    }
}
