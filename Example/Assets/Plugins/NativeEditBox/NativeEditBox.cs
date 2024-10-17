using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public partial class NativeEditBox : MonoBehaviour
{
	enum ReturnButtonType
	{
		Default,
		Go,
		Next,
		Search,
		Send,
		Done,
	}

	enum TextAnchor
	{
		TextAnchorUpperLeft,
		TextAnchorUpperCenter,
		TextAnchorUpperRight,
		TextAnchorMiddleLeft,
		TextAnchorMiddleCenter,
		TextAnchorMiddleRight,
		TextAnchorLowerLeft,
		TextAnchorLowerCenter,
		TextAnchorLowerRight
	};

	public delegate void OnEventHandler();
	public delegate void OnTextChangedHandler(string text);
	public delegate void OnSubmitHandler(string text);

	public event OnTextChangedHandler OnTextChanged;
	public event OnSubmitHandler OnSubmit;
	public event OnEventHandler OnGotFocus;
	public event OnEventHandler OnDidEnd;
	public event OnEventHandler OnTapOutside;

    //add by jeff, used for 'send' button and only support IOS platform.
    [Tooltip("iOS ONLY")]
    public event OnEventHandler OnClickSend;
    [Tooltip("iOS ONLY")]
    public RectTransform SendButtonTransmform;
    [Tooltip("iOS ONLY")]
    public bool ShowNativeSendButton;

#pragma warning disable 0414

	[SerializeField]
	ReturnButtonType returnButtonType = ReturnButtonType.Default;

	[Tooltip("iOS ONLY")]
	[SerializeField]
	bool showClearButton = true;

	[SerializeField]
	bool switchBetweenNativeAndUnity = false;

#pragma warning restore 0414

	TMP_InputField inputField = null;
	new Transform transform = null;

	Coroutine coUpdatePlacement = null;

	Vector3 lastPosition = default;

	void Awake()
	{
		transform = GetComponent<Transform>();
		inputField = GetComponent<TMP_InputField>();
		inputField.shouldHideMobileInput = true;
		inputField.shouldHideSoftKeyboard = true;
	}

	void OnEnable()
	{
		AwakeNative();

		lastPosition = transform.position;
	}

	void Update()
	{
		Vector3 pos = transform.position;
		if (pos != lastPosition)
		{
			lastPosition = pos;

			OnRectTransformDimensionsChange();
		}

		UpdateNative();
	}

	void OnRectTransformDimensionsChange()
	{
		if (inputField == null)
		{
			return;
		}

		if (inputField.textComponent == null)
		{
			Debug.LogError("Input textComponent is null");
			return;
		}
		if (coUpdatePlacement != null)
		{
			Debug.LogError(" coUpdate Placement is not null! ");
			return;
		}
		if (gameObject.activeInHierarchy)
			coUpdatePlacement = StartCoroutine(CoUpdatePlacement());
	}

	IEnumerator CoUpdatePlacement()
	{
		yield return new WaitForEndOfFrame();

		if (this == null)
			yield break;

		UpdatePlacementNow();

		coUpdatePlacement = null;
	}

	void UpdatePlacementNow()
	{
		Rect rectScreen = GetScreenRectFromRectTransform(inputField.textComponent.rectTransform);

		SetPlacement((int)rectScreen.x, (int)rectScreen.y, (int)rectScreen.width, (int)rectScreen.height);

		SetNativeData();
	}



#if !UNITY_EDITOR && UNITY_IOS
	Rect GetScreenRectFromRectTransform(RectTransform rectTransform)
	{
		if (!NoStretched(rectTransform))
		{
			Vector3[] corners = new Vector3[4];
			rectTransform.GetWorldCorners(corners);
			var cam = Camera.main;//Use the camera which rendering UI at runtime.
			var windowC0 = cam.WorldToScreenPoint(corners[0]);
			var windowC1 = cam.WorldToScreenPoint(corners[1]);
			var windowC2 = cam.WorldToScreenPoint(corners[2]);
			var windowC3 = cam.WorldToScreenPoint(corners[3]);
			float width = Vector3.Distance(windowC0 , windowC3 );
			float height = Vector3.Distance(windowC0 , windowC1);

			float screenWidth = Screen.width;
			float screenHeight = Screen.height;
			Rect rect = new Rect(windowC1.x, screenHeight  - windowC1.y  , width, height);
			return rect;
		}
		else
		{
			Rect r = rectTransform.rect;
			Vector2 zero = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x, r.y));
			Vector2 one = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x + r.width, r.y + r.height));
			return new Rect(zero.x, Screen.height - one.y, one.x, Screen.height - zero.y);
		}

	}

	bool NoStretched(RectTransform rectTransform)
	{
		return rectTransform.anchorMin.x == rectTransform.anchorMax.x && rectTransform.anchorMin.y == rectTransform.anchorMax.y;
	}

#else

	Rect GetScreenRectFromRectTransform(RectTransform rectTransform)
	{
		Rect r = rectTransform.rect;
		Vector2 zero = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x, r.y));
		Vector2 one = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x + r.width, r.y + r.height));
		return new Rect(zero.x, Screen.height - one.y, one.x, Screen.height - zero.y);
	}
#endif

	static NativeEditBox FindNativeEditBoxBy(int instanceId)
	{
		var instances = FindObjectsByType<NativeEditBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (var i in instances)
			if (i.GetInstanceID() == instanceId)
				return i;
		return null;
	}

#region Keyboard Position and Size

	static Rect keyboard = default(Rect);

	public static Rect KeyboardArea => keyboard;

#endregion
}
