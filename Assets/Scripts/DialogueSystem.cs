using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueSystem : MonoBehaviour
{
	[Serializable]
	public class DialogueLine
	{
		public string speaker;
		[TextArea(2, 5)] public string text;
		public Sprite portrait;

		public DialogueLine(string speaker, string text, Sprite portrait = null)
		{
			this.speaker = speaker;
			this.text = text;
			this.portrait = portrait;
		}
	}

	[Header("Optional UI overrides")]
	[SerializeField] private Canvas canvas;
	[SerializeField] private Text speakerText;
	[SerializeField] private Text dialogueText;
	[SerializeField] private Text advanceArrow;
	[SerializeField] private float charactersPerSecond = 45f;
	[Header("Font and portrait")]
	[SerializeField] private Font dialogueFont;
	[SerializeField] private Sprite placeholderPortrait;
	[SerializeField] private Image characterPortrait;

	private GameObject dialoguePanel;
	private Text portraitPlaceholderText;
	private DialogueLine[] activeLines;
	private int lineIndex;
	private string fullDialogueLine;
	private int revealedCharacterCount;
	private float revealProgress;
	private bool isShowing;

	public bool IsShowing => isShowing;

	private void Awake()
	{
		BuildUIIfNeeded();
		if (dialoguePanel != null)
			dialoguePanel.SetActive(false);
	}

	private void Update()
	{
		if (isShowing && revealedCharacterCount < fullDialogueLine.Length)
		{
			revealProgress += Mathf.Max(1f, charactersPerSecond) * Time.unscaledDeltaTime;
			revealedCharacterCount = Mathf.Min(Mathf.FloorToInt(revealProgress), fullDialogueLine.Length);
			dialogueText.text = fullDialogueLine.Substring(0, revealedCharacterCount);
		}

		Keyboard keyboard = Keyboard.current;
		if (keyboard == null)
			return;

		if (!isShowing)
			return;

		if (keyboard.spaceKey.wasPressedThisFrame || keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
			Advance();
	}

	/// <summary>Starts a dialogue sequence. Each line is shown in order.</summary>
	public void StartDialogue(DialogueLine[] lines)
	{
		if (lines == null || lines.Length == 0)
			return;

		BuildUIIfNeeded();
		activeLines = lines;
		lineIndex = 0;
		isShowing = true;
		dialoguePanel.SetActive(true);
		ShowCurrentLine();
	}

	/// <summary>Displays the next line, or closes the dialogue after the last line.</summary>
	public void Advance()
	{
		if (!isShowing)
			return;

		if (revealedCharacterCount < fullDialogueLine.Length)
		{
			revealedCharacterCount = fullDialogueLine.Length;
			dialogueText.text = fullDialogueLine;
			return;
		}

		lineIndex++;
		if (activeLines == null || lineIndex >= activeLines.Length)
		{
			isShowing = false;
			dialoguePanel.SetActive(false);
			activeLines = null;
			return;
		}

		ShowCurrentLine();
	}

	private void ShowCurrentLine()
	{
		DialogueLine line = activeLines[lineIndex];
		speakerText.text = line != null ? line.speaker : string.Empty;
		fullDialogueLine = line != null ? line.text ?? string.Empty : string.Empty;
		revealedCharacterCount = 0;
		revealProgress = 0f;
		dialogueText.text = string.Empty;
		speakerText.fontStyle = FontStyle.Bold;
		dialogueText.fontStyle = FontStyle.Italic;
		characterPortrait.sprite = line != null && line.portrait != null ? line.portrait : placeholderPortrait;
		characterPortrait.color = characterPortrait.sprite != null ? Color.white : new Color(0.18f, 0.22f, 0.3f, 1f);
		portraitPlaceholderText.gameObject.SetActive(characterPortrait.sprite == null);
	}

	private void BuildUIIfNeeded()
	{
		if (canvas == null)
		{
			GameObject canvasObject = new GameObject("Dialogue Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
			DontDestroyOnLoad(canvasObject);
			canvas = canvasObject.GetComponent<Canvas>();
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
			scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
			scaler.referenceResolution = new Vector2(1920, 1080);
		}

		if (FindAnyObjectByType<EventSystem>() == null)
		{
			GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
			eventSystem.transform.SetParent(transform);
			eventSystem.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
		}

		if (dialoguePanel == null)
		{
			dialoguePanel = new GameObject("Dialogue Panel", typeof(RectTransform), typeof(Image));
			dialoguePanel.transform.SetParent(canvas.transform, false);
			RectTransform panelRect = dialoguePanel.GetComponent<RectTransform>();
			panelRect.anchorMin = new Vector2(0.03f, 0.65f);
			panelRect.anchorMax = new Vector2(0.97f, 0.97f);
			panelRect.offsetMin = Vector2.zero;
			panelRect.offsetMax = Vector2.zero;
			dialoguePanel.GetComponent<Image>().color = new Color(0.04f, 0.06f, 0.12f, 0.94f);
		}

		if (characterPortrait == null)
		{
			GameObject portraitObject = new GameObject("Character Portrait", typeof(RectTransform), typeof(Image));
			portraitObject.transform.SetParent(dialoguePanel.transform, false);
			RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
			portraitRect.anchorMin = new Vector2(0.025f, 0.1f);
			portraitRect.anchorMax = new Vector2(0.19f, 0.9f);
			portraitRect.offsetMin = Vector2.zero;
			portraitRect.offsetMax = Vector2.zero;
			characterPortrait = portraitObject.GetComponent<Image>();
		}
		characterPortrait.preserveAspect = true;
		characterPortrait.raycastTarget = false;
		if (portraitPlaceholderText == null)
			portraitPlaceholderText = CreateText("Portrait Placeholder", characterPortrait.transform, Vector2.zero, Vector2.one, 18, FontStyle.Normal, dialogueFont);

		if (speakerText == null)
			speakerText = CreateText("Speaker", dialoguePanel.transform, new Vector2(0.22f, 0.69f), new Vector2(0.96f, 0.93f), 48, FontStyle.Bold, dialogueFont);
		if (dialogueText == null)
			dialogueText = CreateText("Dialogue", dialoguePanel.transform, new Vector2(0.22f, 0.25f), new Vector2(0.96f, 0.68f), 42, FontStyle.Italic, dialogueFont);
		if (dialogueFont != null)
		{
			speakerText.font = dialogueFont;
			dialogueText.font = dialogueFont;
		}
		speakerText.fontStyle = FontStyle.Bold;
		speakerText.fontSize = 48;
		dialogueText.fontStyle = FontStyle.Italic;
		dialogueText.fontSize = 42;

		if (advanceArrow == null)
			advanceArrow = CreateText("Advance Arrow", dialoguePanel.transform, new Vector2(0.88f, 0.03f), new Vector2(0.96f, 0.2f), 36, FontStyle.Normal, dialogueFont);
		advanceArrow.text = "\u25BC";
		advanceArrow.fontSize = 36;
		advanceArrow.alignment = TextAnchor.MiddleCenter;
		advanceArrow.raycastTarget = false;
	}

	private static Text CreateText(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, int fontSize, FontStyle style, Font font)
	{
		GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
		textObject.transform.SetParent(parent, false);
		RectTransform rect = textObject.GetComponent<RectTransform>();
		rect.anchorMin = anchorMin;
		rect.anchorMax = anchorMax;
		rect.offsetMin = new Vector2(12, 4);
		rect.offsetMax = new Vector2(-12, -4);
		Text text = textObject.GetComponent<Text>();
		text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
		text.fontSize = fontSize;
		text.fontStyle = style;
		text.color = Color.white;
		text.alignment = TextAnchor.MiddleLeft;
		text.horizontalOverflow = HorizontalWrapMode.Wrap;
		text.verticalOverflow = VerticalWrapMode.Overflow;
		return text;
	}
}
