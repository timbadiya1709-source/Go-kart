using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Button))]
[RequireComponent(typeof(AudioSource))]
public class UIButtonSoundAndColor : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Audio Clips")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Header("Text (TMP)")]
    public TextMeshProUGUI buttonText;      // drag your TMP text here
    public Color textNormalColor = Color.white;
    public Color textHoverColor = Color.yellow;

    private AudioSource audioSource;
    private Button button;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;

        button = GetComponent<Button>();

        if (buttonText != null)
            buttonText.color = textNormalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = textHoverColor;

        if (hoverClip != null)
            audioSource.PlayOneShot(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (buttonText != null)
            buttonText.color = textNormalColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (clickClip != null)
            audioSource.PlayOneShot(clickClip);
    }
}
