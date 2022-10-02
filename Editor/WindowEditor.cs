using System;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UI
{
	[CustomEditor(typeof(Window), true)]
	[CanEditMultipleObjects]
	public class WindowEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			Window window = (Window)target;
			Type windowType = target.GetType();
			
			// Draw toggle.
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button(window.IsOpen ? "Close" : "Open"))
				window.Toggle(!Application.isPlaying);
			
			if (GUILayout.Button("Isolate"))
				window.Isolate(!Application.isPlaying);
			
			EditorGUILayout.EndHorizontal();
			
			// Draw matches.
			if (!window.allowMultiple && Window.TryGetWindow(windowType, out Window _window) && _window != window)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				EditorGUILayout.LabelField($"Multiple instances of type '{windowType.FullName}'!", EditorStyles.boldLabel);
				EditorGUI.BeginDisabledGroup(true);
				EditorGUILayout.ObjectField(_window, typeof(Window), true);
				EditorGUI.EndDisabledGroup();
				
				EditorGUILayout.EndVertical();
			}
			
			// Base inspector.
			EditorGUILayout.Space();
			
			base.OnInspectorGUI();
		}
	}
}