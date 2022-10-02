using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

namespace UI
{
	[RequireComponent(typeof(CanvasGroup))]
	[AddComponentMenu("UI/Window")]
	[ExecuteInEditMode]
	public class Window : MonoBehaviour
	{
		[SuppressMessage("ReSharper", "UnusedMember.Global")]
		protected enum StartMode
		{
			None,
			Show,
			Hide,
		}

		public float hideDuration = 1f;
		public float showDuration = 1f;
		public bool shouldTakeFocus = true;
		public bool allowMultiple = false;

		private static readonly HashSet<Window> _openWindows = new();
		private static readonly Dictionary<Type, Window> s_instances = new();
		private static bool _hadFocus;
		
		[SerializeField] protected StartMode _startMode;
		
		[SerializeField, HideInInspector] private bool _wasOpen;
		private bool _isOpen;
		private CanvasGroup _canvasGroup;
		private Coroutine _fadeCoroutine;

		public static event Action<Window, bool> OnWindowFocus;
		public event Action<bool> OnFocus;
		
		public static bool HasFocus => _openWindows.Count > 0;
		
		public bool IsOpen
		{
			get => _isOpen;
			set
			{
				if (_isOpen == value)
					return;
				
				_isOpen = value;
				_canvasGroup.interactable = value;
				_canvasGroup.blocksRaycasts = value;
				
				CrossFade(value ? 1f : 0f, value ? showDuration : hideDuration);
				
				if (shouldTakeFocus)
					Focus(value);
			}
		}

		public static T GetWindow<T>() where T : Window =>
			s_instances.TryGetValue(typeof(T), out Window window) ? (T)window : null;
		
		public static Window GetWindow(Type type) =>
			s_instances.TryGetValue(type, out Window window) ? window : null;
		
		public static bool TryGetWindow(Type type, out Window window) =>
			s_instances.TryGetValue(type, out window);

		public static bool TryGetWindow<T>(out T window) where T : Window
		{
			if (TryGetWindow(typeof(T), out Window _window))
			{
				window = _window as T;
				return true;
			}

			window = null;
			return false;
		}

		public void Toggle(bool instant = false) => SetOpen(!IsOpen, instant);
		
		public void CrossFade(float opacity, float duration = 2f)
		{
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			
			_fadeCoroutine = StartCoroutine(FadeCoroutine(opacity, duration));
		}

		public void Isolate(bool instant = false)
		{
			foreach ((Type _, Window window) in s_instances)
			{
				if (window.Equals(this))
					continue;
				
				window.SetOpen(false, instant);
			}
			
			SetOpen(true, instant);
		}

		public void SetOpen(bool value, bool instant = false)
		{
			if (!instant)
			{
				IsOpen = value;
				return;
			}
			
			_isOpen = value;
			
			_canvasGroup.alpha = value ? 1f : 0f;
			_canvasGroup.interactable = value;
			_canvasGroup.blocksRaycasts = value;
			
			if (shouldTakeFocus)
				Focus(value);
		}
		
		protected virtual void Start()
		{
			if (_startMode != StartMode.None && Application.isPlaying)
				SetOpen(_startMode == StartMode.Show, true);
		}

		protected virtual void OnEnable()
		{
			// Get canvas group.
			_canvasGroup = GetComponent<CanvasGroup>();
			
#if UNITY_EDITOR
			if (EditorSceneManager.IsPreviewSceneObject(this))
			{
				_canvasGroup.alpha = 1f;
				return;
			}
#endif
			// Update cursor state once.
			if (s_instances.Count == 0)
				UpdateCursor(false);
			
			// Add to instances.
			s_instances.TryAdd(GetType(), this);
			
			// Reopen.
			SetOpen(_wasOpen, true);
		}
		
		protected virtual void OnDisable()
		{
#if UNITY_EDITOR
			if (EditorSceneManager.IsPreviewSceneObject(this))
			{
				_canvasGroup.alpha = _isOpen ? 1f : 0f;
				return;
			}
#endif
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			
			_wasOpen = _isOpen;
			
			SetOpen(false, true);

			s_instances.Remove(GetType());
		}

		protected virtual void Focus(bool value)
		{
			if (value)
				_openWindows.Add(this);
			else
				_openWindows.Remove(this);

			bool hasFocus = HasFocus;
			if (hasFocus == _hadFocus) return;

			UpdateCursor(hasFocus);
			
			OnFocus?.Invoke(hasFocus);
			OnWindowFocus?.Invoke(this, hasFocus);
			
			_hadFocus = hasFocus;
		}

		private static void UpdateCursor(bool value)
		{
			if (!Application.isPlaying) value = true;
			
			Cursor.visible = value;
			
#if UNITY_EDITOR
			Cursor.lockState = value ? CursorLockMode.None : CursorLockMode.Locked;
#else
			Cursor.lockState = value ? CursorLockMode.Confined : CursorLockMode.Locked;
#endif
		}

		private IEnumerator FadeCoroutine(float target, float duration)
		{
			float start = _canvasGroup.alpha;
			float normalizedTime;
			
			while ((normalizedTime = (Time.unscaledTime - start) / duration) < 1f)
			{
				_canvasGroup.alpha = Mathf.Lerp(start, target, normalizedTime);
				yield return null;
			}
			
			_canvasGroup.alpha = target;
			_fadeCoroutine = null;
		}
	}
}