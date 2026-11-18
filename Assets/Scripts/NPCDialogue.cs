using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class NPCDialogue : MonoBehaviour
{
    [SerializeField] DialogueSystem dialogueSystem;
    [SerializeField] DialogueSequence dialogueSequence;
    [SerializeField] string playerTag = "Player";

    bool playerInRange;
    Collider2D interactionArea;

    private void Awake()
    {
        interactionArea = GetComponent<Collider2D>();
        if (!interactionArea.isTrigger)
            Debug.LogWarning("NPCDialogue requires its Collider2D to have Is Trigger enabled.", this);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (!playerInRange || keyboard == null || !keyboard.eKey.wasPressedThisFrame)
            return;

        if (dialogueSystem == null)
        {
            Debug.LogError("Assign a DialogueSystem to this NPC.", this);
            return;
        }

        if (dialogueSequence == null)
        {
            Debug.LogError("Assign a DialogueSequence or TestDialogue asset to this NPC.", this);
            return;
        }

        if (!dialogueSystem.IsShowing)
            dialogueSystem.StartDialogue(dialogueSequence.ToDialogueLines());
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.transform.root.CompareTag(playerTag))
            playerInRange = true;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.root.CompareTag(playerTag))
            playerInRange = false;
    }
}
