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

		private static readonly HashSet<Window> _openWindows = new();
		private static readonly Dictionary<string, Window> s_instances = new();
		private static bool _hadFocus;
		
		[SerializeField] protected StartMode _startMode;
		
		[SerializeField, HideInInspector] private bool _wasOpen;
		private bool _isOpen;
		private CanvasGroup _canvasGroup;
		private Coroutine _fadeCoroutine;
		private string _name;

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

		public string Name
		{
			get => _name;
			set
			{
				if (_name.Equals(value))
					return;

				s_instances.Remove(_name);
				s_instances.TryAdd(value, this);
				
				_name = value;
			}
		}

		public static Window GetWindow(string name) =>
			s_instances.TryGetValue(name, out Window window) ? window : null;
		
		public static bool TryGetWindow(string name, out Window window) =>
			s_instances.TryGetValue(name, out window);

		public void Toggle(bool instant = false) => SetOpen(!IsOpen, instant);
		
		public void CrossFade(float opacity, float duration = 2f)
		{
			if (_fadeCoroutine != null)
				StopCoroutine(_fadeCoroutine);
			
			_fadeCoroutine = StartCoroutine(FadeCoroutine(opacity, duration));
		}

		public void Isolate(bool instant = false)
		{
			foreach ((string _, Window window) in s_instances)
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
			if (string.IsNullOrEmpty(_name))
				_name = gameObject.name;
			
			s_instances.Add(_name, this);
			
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

			s_instances.Remove(_name);
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
			
			while ((normalizedTime = (Time.time - start) / duration) < 1f)
			{
				_canvasGroup.alpha = Mathf.Lerp(start, target, normalizedTime);
				yield return null;
			}
			
			_canvasGroup.alpha = target;
			_fadeCoroutine = null;
		}
	}
}