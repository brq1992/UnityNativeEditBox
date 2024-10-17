#if !UNITY_EDITOR && UNITY_IOS
// #if UNITY_EDITOR || UNITY_IOS

using UnityEngine;
using UnityEngine.EventSystems;
using System;
using System.Collections;
using System.Runtime.InteropServices;
using TMPro;
using AOT;

public partial class NativeEditBox : IPointerClickHandler
{
	[DllImport("__Internal")]
	static extern IntPtr _CNativeEditBox_Init(int instanceId, bool multiline);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_Destroy(IntPtr instance);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetFocus(IntPtr instance, bool doFocus);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetPlacement(IntPtr instance, int left, int top, int right, int bottom);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetPlaceholder(IntPtr instance, string text, float r, float g, float b, float a);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetFontSize(IntPtr instance, int size);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetFontColor(IntPtr instance, float r, float g, float b, float a);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetTextAlignment(IntPtr instance, int alignment);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetInputType(IntPtr instance, int inputType);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetKeyboardType(IntPtr instance, int keyboardType);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetReturnButtonType(IntPtr instance, int returnButtonType);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetCharacterLimit(IntPtr instance, int characterLimit);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetText(IntPtr instance, string newText);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_ShowClearButton(IntPtr instance, bool value);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SelectRange(IntPtr instance, int from, int to);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_RegisterKeyboardChangedCallback(DelegateKeyboardChanged callback);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_RegisterTextCallbacks(DelegateWithText textChanged, DelegateWithText didEnd, DelegateWithText submitPressed);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_RegisterEmptyCallbacks(DelegateEmpty gotFocus, DelegateEmpty tapOutside);

	//Add by jeff, Register send delegate
	[DllImport("__Internal")]
	static extern void _CNativeEditBox_RegisterSendButtonCallbacks(DelegateEmpty tapSend);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetSendButtonPlacement(IntPtr instance, int left, int top, int right, int bottom);

	[DllImport("__Internal")]
	static extern void _CNativeEditBox_SetSendButtonIsShow(IntPtr instance, bool showSendButton);

	IntPtr editBox;

#region Public Methods

	public static bool IsKeyboardSupported()
	{
		return true;
	}

	public void SetText(string text)
	{
		inputField.text = text;

		if (editBox != IntPtr.Zero)
			_CNativeEditBox_SetText(editBox, text);
	}

	public void SelectRange(int from, int to)
	{
		if (editBox != IntPtr.Zero)
			_CNativeEditBox_SelectRange(editBox, from, to);
	}

	public void SetPlacement(int left, int top, int right, int bottom)
	{
		if (editBox != IntPtr.Zero)
			_CNativeEditBox_SetPlacement(editBox, left, top, right, bottom);
	}

	public void ActivateInputField()
	{
		if (editBox != IntPtr.Zero)
		{
			_CNativeEditBox_SetFocus(editBox, true);
			return;
		}

		StartCoroutine(CreateNow(true));
	}

	public void DestroyNative()
	{
		DestroyNow();
	}

	public string text
	{
		set => SetText(value);
		get => inputField.text;
	}

#endregion

	void AwakeNative()
	{
		inputField.interactable = false;

		if (!switchBetweenNativeAndUnity)
			StartCoroutine(CreateNow(false));
	}

	bool ShowText
	{
		set
		{
			inputField.placeholder.gameObject.SetActive(value);
			inputField.textComponent.enabled = value;
			inputField.enabled = value;
		}
	}

	void OnDestroy()
	{
		DestroyNow();
	}

	void OnDisable()
	{
		DestroyNow();
	}

	void UpdateNative()
	{
		
	}

	void SetNativeData()
    {
		if (SendButtonTransmform != null)
		{
			Vector3[] corners = new Vector3[4];
			SendButtonTransmform.GetWorldCorners(corners);
			var cam = Camera.main;//Use the camera which rendering UI at runtime.
			var windowC0 = cam.WorldToScreenPoint(corners[0]);
			var windowC1 = cam.WorldToScreenPoint(corners[1]);
			var windowC3 = cam.WorldToScreenPoint(corners[3]);
			float width = Vector3.Distance(windowC0, windowC3);
			float height = Vector3.Distance(windowC0, windowC1);
			float screenHeight = Screen.height;
			Rect rect = new Rect(windowC1.x, screenHeight - windowC1.y, width, height);
			SetSendButtonPlacement((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height);
		}
	}

	IEnumerator CreateNow(bool doFocus)
	{
		yield return new WaitForEndOfFrame();

		if (editBox != IntPtr.Zero)
			yield break;

		SetupInputField();

		ShowText = false;

		if (doFocus)
			_CNativeEditBox_SetFocus(editBox, true);
	}

	void DestroyNow()
	{
		if (editBox == IntPtr.Zero)
			return;

		_CNativeEditBox_Destroy(editBox);
		editBox = IntPtr.Zero;

		ShowText = true;
	}

#region IPointerClickHandler implementation

	public void OnPointerClick(PointerEventData eventData)
	{
		if (editBox == IntPtr.Zero)
			StartCoroutine(CreateNow(true));
	}

#endregion

	void SetupInputField()
	{
		TMP_Text text = inputField.textComponent;
		TMP_Text placeholder = inputField.placeholder as TMP_Text;

		//ugly, fix enum
		TextAnchor alignment = text.verticalAlignment switch
		{
			VerticalAlignmentOptions.Top => text.horizontalAlignment switch
			{
				HorizontalAlignmentOptions.Left => TextAnchor.TextAnchorUpperLeft,
				HorizontalAlignmentOptions.Center => TextAnchor.TextAnchorUpperCenter,
				HorizontalAlignmentOptions.Right => TextAnchor.TextAnchorUpperRight,
				_ => TextAnchor.TextAnchorUpperLeft
			},
			VerticalAlignmentOptions.Middle => text.horizontalAlignment switch
			{
				HorizontalAlignmentOptions.Left => TextAnchor.TextAnchorMiddleLeft,
				HorizontalAlignmentOptions.Center => TextAnchor.TextAnchorMiddleCenter,
				HorizontalAlignmentOptions.Right => TextAnchor.TextAnchorMiddleRight,
				_ => TextAnchor.TextAnchorMiddleLeft
			},
			VerticalAlignmentOptions.Bottom => text.horizontalAlignment switch
			{
				HorizontalAlignmentOptions.Left => TextAnchor.TextAnchorLowerLeft,
				HorizontalAlignmentOptions.Center => TextAnchor.TextAnchorLowerCenter,
				HorizontalAlignmentOptions.Right => TextAnchor.TextAnchorLowerRight,
				_ => TextAnchor.TextAnchorLowerLeft
			},
			_ => TextAnchor.TextAnchorUpperLeft
		};

		editBox = _CNativeEditBox_Init(GetInstanceID(), inputField.lineType != TMP_InputField.LineType.SingleLine);
		_CNativeEditBox_RegisterKeyboardChangedCallback(delegateKeyboardChanged);
		_CNativeEditBox_RegisterTextCallbacks(DelegateTextChanged, DelegateDidEnd, DelegateSubmitPressed);
		_CNativeEditBox_RegisterEmptyCallbacks(DelegateGotFocus, DelegateTapOutside);
		_CNativeEditBox_RegisterSendButtonCallbacks(DelegateTapSendButton);
		

		UpdatePlacementNow();

		_CNativeEditBox_SetFontSize(editBox, Mathf.RoundToInt(text.fontSize * text.pixelsPerUnit));
		_CNativeEditBox_SetFontColor(editBox, text.color.r, text.color.g, text.color.b, text.color.a);
		_CNativeEditBox_SetPlaceholder(editBox, placeholder.text, placeholder.color.r, placeholder.color.g, placeholder.color.b, placeholder.color.a);
		_CNativeEditBox_SetTextAlignment(editBox, (int)alignment);
		_CNativeEditBox_SetInputType(editBox, (int)inputField.inputType);
		_CNativeEditBox_SetKeyboardType(editBox, (int)inputField.keyboardType);
		_CNativeEditBox_SetReturnButtonType(editBox, (int)returnButtonType);
		_CNativeEditBox_SetCharacterLimit(editBox, inputField.characterLimit);
		_CNativeEditBox_SetText(editBox, inputField.text);
		_CNativeEditBox_ShowClearButton(editBox, showClearButton);
		_CNativeEditBox_SetSendButtonIsShow(editBox, ShowNativeSendButton);
	}

	void SetSendButtonPlacement(int x , int y, int width, int height)
    {
		if (editBox != IntPtr.Zero)
			_CNativeEditBox_SetSendButtonPlacement(editBox, x, y, width, height);
	}

#region CALLBACKS

	delegate void DelegateWithText(int instanceId, string text);
	delegate void DelegateEmpty(int instanceId);

	[MonoPInvokeCallback(typeof(DelegateWithText))]
	static void DelegateTextChanged(int instanceId, string text)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			editBox.inputField.text = text;

			editBox.OnTextChanged?.Invoke(text);
		}
	}

	[MonoPInvokeCallback(typeof(DelegateWithText))]
	static void DelegateDidEnd(int instanceId, string text)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			editBox.inputField.text = text;

			if (editBox.switchBetweenNativeAndUnity)
				editBox.DestroyNow();

			editBox.OnDidEnd?.Invoke();
		}
	}

	[MonoPInvokeCallback(typeof(DelegateWithText))]
	static void DelegateSubmitPressed(int instanceId, string text)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			editBox.inputField.text = text;

			editBox.OnSubmit?.Invoke(text);
		}
	}

	[MonoPInvokeCallback(typeof(DelegateEmpty))]
	static void DelegateGotFocus(int instanceId)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			editBox.OnGotFocus?.Invoke();

			if (editBox.inputField.onFocusSelectAll)
				editBox.SelectRange(0, editBox.inputField.text.Length);
		}
	}

	[MonoPInvokeCallback(typeof(DelegateEmpty))]
	static void DelegateTapOutside(int instanceId)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			if (editBox.switchBetweenNativeAndUnity)
				editBox.DestroyNow();

			editBox.OnTapOutside?.Invoke();
		}
	}

	[MonoPInvokeCallback(typeof(DelegateEmpty))]
	static void DelegateTapSendButton(int instanceId)
	{
		var editBox = FindNativeEditBoxBy(instanceId);
		if (editBox != null)
		{
			editBox.OnClickSend?.Invoke();
		}
	}

#endregion

#region GLOBAL CALLBACK

	delegate void DelegateKeyboardChanged(float x, float y, float width, float height);

	[MonoPInvokeCallback(typeof(DelegateKeyboardChanged))]
	static void delegateKeyboardChanged(float x, float y, float width, float height)
	{
		keyboard = new Rect(x, y, width, height);
	}

#endregion
}

#endif
