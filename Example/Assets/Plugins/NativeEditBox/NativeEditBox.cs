﻿using System.Collections;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public partial class NativeEditBox : MonoBehaviour
{
	const string GlobalListenerName = "NativeEditBoxGlobalListener_1000";

	static GameObject globalListener = null;

	enum ReturnButtonType
	{
		Default,
		Go,
		Next,
		Search,
		Send,
		Done,
	}

	public delegate void OnEventHandler();
	public delegate void OnTextChangedHandler(string text);
	public delegate void OnSubmitHandler(string text);

	public event OnTextChangedHandler OnTextChanged;
	public event OnSubmitHandler OnSubmit;
	public event OnEventHandler OnGotFocus;
	public event OnEventHandler OnDidEnd;
	public event OnEventHandler OnTapOutside;

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
		inputField = GetComponent<TMP_InputField>();
		transform = GetComponent<Transform>();

		if (globalListener == null)
			CreateGlobalListener();

		name += "NEB" + GetInstanceID().ToString();
	}

	void OnEnable()
	{
		AwakeNative();

		lastPosition = transform.position;
	}

	void CreateGlobalListener()
	{
		globalListener = new GameObject();
		globalListener.name = GlobalListenerName;
		GameObject.DontDestroyOnLoad(globalListener);

		globalListener.AddComponent<NativeEditBoxGlobalListener>();
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
		if (inputField == null || inputField.textComponent == null)
			return;

		if (coUpdatePlacement != null)
			return;

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
	}

	Rect GetScreenRectFromRectTransform(RectTransform rectTransform)
	{
		Rect r = rectTransform.rect;
		Vector2 zero = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x, r.y));
		Vector2 one = rectTransform.localToWorldMatrix.MultiplyPoint(new Vector3(r.x + r.width, r.y + r.height));

		return new Rect(zero.x, Screen.height - one.y, one.x, Screen.height - zero.y);
	}

	static NativeEditBox FindNativeEditBoxBy(int instanceId)
	{
		var instances = FindObjectsByType<NativeEditBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);
		foreach (var i in instances)
			if (i.GetInstanceID() == instanceId)
				return i;
		return null;
	}

	#region Public Methods

	public static Rect KeyboardArea => NativeEditBoxGlobalListener.KeyboardArea;

	#endregion
}
