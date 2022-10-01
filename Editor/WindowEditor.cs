using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace UI
{
	[CustomEditor(typeof(Window), true)]
	[CanEditMultipleObjects]
	public class WindowEditor : Editor
	{
		private string _name;

		private void OnEnable()
		{
			_name = target.name;
		}

		public override void OnInspectorGUI()
		{
			Window window = (Window)target;
			
			// Draw toggle.
			EditorGUILayout.BeginHorizontal();
			
			if (GUILayout.Button(window.IsOpen ? "Close" : "Open"))
				window.Toggle(!Application.isPlaying);
			
			if (GUILayout.Button("Isolate"))
				window.Isolate(!Application.isPlaying);
			
			EditorGUILayout.EndHorizontal();
			
			// Draw name.
			_name = Regex.Replace(EditorGUILayout.TextField("Name", _name), @"\s+", "");
			
			if (!_name.Equals(window.Name) && !string.IsNullOrEmpty(_name))
			{
				if (Window.TryGetWindow(_name, out Window namedWindow) && namedWindow != window)
				{
					EditorGUILayout.BeginVertical(EditorStyles.helpBox);

					EditorGUILayout.LabelField($"Name '{_name}' is used by another window!", EditorStyles.boldLabel);
					EditorGUI.BeginDisabledGroup(true);
					EditorGUILayout.ObjectField(namedWindow, typeof(Window), true);
					EditorGUI.EndDisabledGroup();
					
					EditorGUILayout.EndVertical();
				}
				else
				{
					window.name = _name;
				}
			}
			
			// Base inspector.
			EditorGUILayout.Space();
			
			base.OnInspectorGUI();
		}
	}
}