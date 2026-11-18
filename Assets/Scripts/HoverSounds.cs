using UnityEngine;
using UnityEngine.EventSystems;

public class HoverSounds : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hoverClip;

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioSource != null && hoverClip != null)
        {
            audioSource.PlayOneShot(hoverClip);
        }
    }
}
