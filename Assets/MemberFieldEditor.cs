#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MemberFieldEditor : EditorWindow
{
	private const string MENU_ITEM_PATH = "Window/Member Field Editor";
	private const string WINDOW_NAME = "Member Field Editor";
	private const string ASSET_SCENE_FILTER = "t:Scene";
	private const string REFRESH_BUTTON_TITLE = "Refresh List!";
	private const string CLOSE_BUTTON_TEXT = "Close";

	private const string EDIT_BUTTON_EDITABLE_OBJECT_TEXT = "EDIT";
	private const string EDIT_BUTTON_NULL_OBJECT_TEXT = "NULL";
	private const string EDIT_BUTTON_ACTIVE_INSPECTOR_IS_OF_OBJECT_TEXT = "Close";

	private const float INTERACTIVE_FIELD_WIDTH = 120.0f;
	private const float LABEL_FIELD_WIDTH = 180.0f;
	private const float INSPECTOR_AREA_HEIGHT_RATIO = 0.3f;

	private static Vector2 MIN_SIZE_EDITOR_CLOSED => new Vector2(640, 300);
	private static GUILayoutOption[] LABEL_FIELD_LAYOUT_OPTIONS => 
		new GUILayoutOption[] { GUILayout.Width(LABEL_FIELD_WIDTH) };
	private static GUILayoutOption[] INTERACTIVE_FIELD_LAYOUT_OPTIONS => 
		new GUILayoutOption[] { GUILayout.Width(INTERACTIVE_FIELD_WIDTH) };

	private static HashSet<System.Type> cachedLegalBaseTypeLookUp = null;
	private static HashSet<System.Type> LegalBaseTypeLookUp
	{
		get
		{
			if (cachedLegalBaseTypeLookUp == null) {
				cachedLegalBaseTypeLookUp = new HashSet<System.Type>();
				cachedLegalBaseTypeLookUp.Add(typeof(UnityEngine.Object));
			}
			return cachedLegalBaseTypeLookUp;
		}
	}
	private static bool IsBaseTypeImplementedInEditor(System.Type type)
	{
		return LegalBaseTypeLookUp.Contains(type);
	}

	[MenuItem(MENU_ITEM_PATH)]
    public static void OpenWindow()
    {
        var window = GetWindow<MemberFieldEditor>(WINDOW_NAME);
		window.minSize = MIN_SIZE_EDITOR_CLOSED;
	}

	private Editor activeEditor;
	private Scene[] scenes = null;
	private Vector2 scroll = new Vector2(0, 0);
	private Vector2 inspectorScroll = new Vector2(0, 0);
	private List<CachedDrawable> cachedDrawableElements = new List<CachedDrawable>();


	private void OnEnable()
	{
		scenes = FindAllScenesInProject();
		CacheValidElements(cachedDrawableElements, scenes);
	}

	public void OnGUI()
	{
		var isRefreshing = GUILayout.Button(REFRESH_BUTTON_TITLE);
		if (isRefreshing) {
			RefreshData();
		}

		EditorGUILayout.BeginVertical();
		
		DrawComponentSelection();

		if (activeEditor != null) {
			DrawActiveEditorHeader();

			DrawInspectorSectionOfSelectedField();
		}
		EditorGUILayout.EndVertical();
	}

	private void DrawActiveEditorHeader()
	{
		GUIStyle seperatorStyle = new GUIStyle(GUI.skin.box);
		seperatorStyle.normal.background = GUI.skin.button.normal.background;
		seperatorStyle.fontStyle = FontStyle.Bold;
		seperatorStyle.normal.textColor = Color.white;
		seperatorStyle.alignment = TextAnchor.MiddleCenter;
		GUILayout.Box(activeEditor.serializedObject.targetObject.name, seperatorStyle, GUILayout.Width(position.width));
	}

	private void RefreshData()
	{
		scenes = FindAllScenesInProject();
		CacheValidElements(cachedDrawableElements, scenes);
	}

	private void DrawInspectorSectionOfSelectedField()
	{
		var isRemoving = GUILayout.Button(CLOSE_BUTTON_TEXT);
		EditorGUILayout.BeginVertical(GUILayout.MaxHeight(position.height * INSPECTOR_AREA_HEIGHT_RATIO));
		inspectorScroll = DrawScrollArea(inspectorScroll, () =>
		{
			activeEditor.OnInspectorGUI();
			if (isRemoving) {
				activeEditor = null;
			}
		});
		EditorGUILayout.EndVertical();
	}

	private void DrawComponentSelection()
	{
		EditorGUILayout.BeginVertical();
		scroll = DrawScrollArea(scroll, () =>
		{
			DrawAllElements(cachedDrawableElements, ref activeEditor);
		});
		EditorGUILayout.EndVertical();
	}

	// Static Functions to keep object state mutability as limited as possible

	private static Vector2 DrawScrollArea(in Vector2 scroll, System.Action action)
	{
		var next = EditorGUILayout.BeginScrollView(scroll);

		action();

		EditorGUILayout.EndScrollView();
		return next;
	}


	private static Scene[] FindAllScenesInProject()
	{
		var allSceneGUIDs = AssetDatabase.FindAssets(ASSET_SCENE_FILTER);

		Scene[] allScenes = new Scene[allSceneGUIDs.Length];
		for(int i = 0; i < allSceneGUIDs.Length; i++) {
			var scene = allSceneGUIDs[i];
			allScenes[i] = SceneManager.GetSceneByPath(AssetDatabase.GUIDToAssetPath(scene));
		} 
		return allScenes;
	}

	private static void DrawAllElements(in List<CachedDrawable> drawableElements, ref Editor activeEditor)
	{
		foreach (var element in drawableElements) {
			activeEditor = DrawAndTryGettingActiveInspector(element, activeEditor);
			EditorGUILayout.Space();
		}
	}

	private static Editor DrawAndTryGettingActiveInspector(CachedDrawable element, Editor activeEditor)
	{
		GUIStyle style = GUI.skin.label;
		style.normal.textColor = Color.white;
		style.fontStyle = FontStyle.Bold;
		style.alignment = TextAnchor.MiddleCenter;
		style.wordWrap = true;

		var previous = GUI.color;
		GUI.color = element.isFolded ? Color.green : Color.magenta;
		if (GUILayout.Button(element.entryName)) {
			element.Toggle();
		}
		GUI.color = previous;

		if (element.isFolded) {
			foreach (var entry in element.entries) {
				EditorGUILayout.BeginHorizontal();

				style.normal.textColor = Color.white;
				style.fontStyle = FontStyle.Bold;
				style.alignment = TextAnchor.MiddleLeft;
				EditorGUILayout.LabelField($"{entry.formattedFiedType}", style, LABEL_FIELD_LAYOUT_OPTIONS);

				style.normal.textColor = Color.white;
				style.fontStyle = FontStyle.Normal;
				style.alignment = TextAnchor.MiddleLeft;
				EditorGUILayout.LabelField($"{entry.formattedFieldName}", style, LABEL_FIELD_LAYOUT_OPTIONS);

				activeEditor = DecideTypeToDrawAndDrawField(activeEditor, entry);

				EditorGUILayout.EndHorizontal();
			}
		}

		return activeEditor;
	}

	private static Editor DecideTypeToDrawAndDrawField(Editor activeEditor, CachedEntry entry)
	{
		var obj = entry.reflectionObjectWrapper.GetObject();
		var type = entry.reflectionObjectWrapper.fieldInfo.FieldType;
		if (typeof(Object).IsAssignableFrom(type)) {
			DrawObjectElement(ref activeEditor, entry, obj);
		}

		// ... Add float, int, etc. WHATEVER WE WANT!! 

		return activeEditor;
	}

	private static void DrawObjectElement(ref Editor activeEditor, CachedEntry entry, object obj)
	{
		var style = GUI.skin.label;

		entry.reflectionObjectWrapper.SetObject(
			EditorGUILayout.ObjectField(obj as Object,
			entry.reflectionObjectWrapper.fieldInfo.FieldType,
			INTERACTIVE_FIELD_LAYOUT_OPTIONS)
		);
		
		if (obj != null) {
			var previous = GUI.color;
			GUI.color = activeEditor?.serializedObject.targetObject == obj ? Color.green : GUI.color;
			if(activeEditor?.serializedObject.targetObject == obj) {
				GUI.color = Color.green;
				if (GUILayout.Button(EDIT_BUTTON_ACTIVE_INSPECTOR_IS_OF_OBJECT_TEXT, INTERACTIVE_FIELD_LAYOUT_OPTIONS)) {
					activeEditor = null;
				}
			}
			else {
				if (GUILayout.Button(EDIT_BUTTON_EDITABLE_OBJECT_TEXT, INTERACTIVE_FIELD_LAYOUT_OPTIONS)) {
					activeEditor = Editor.CreateEditor(obj as Object);
				}				
			}
			GUI.color = previous;
		}
		else {
			style.normal.textColor = Color.red;
			style.fontStyle = FontStyle.Bold;
			style.alignment = TextAnchor.MiddleCenter;
			GUILayout.Label(EDIT_BUTTON_NULL_OBJECT_TEXT, style, INTERACTIVE_FIELD_LAYOUT_OPTIONS);
		}
	}

	private static void CacheValidElements(List<CachedDrawable> cache, in Scene[] scenes)
	{
		cache.Clear();
		foreach (var scene in scenes) {
			if(scene.IsValid()) {
				ForEachRootGameObjectInScene(cache, scene);
			}
		}
	}

	private static void ForEachRootGameObjectInScene(List<CachedDrawable> cache, Scene scene)
	{
		foreach (var go in scene.GetRootGameObjects()) {
			ForEachChildInTransform(go.transform, (depth, transform) =>
			{
				foreach (var componment in transform.GetComponents<Component>()) {
					ForEachComponentInTransform(cache, transform, componment);
				}
			});
		}
	}

	private static void ForEachComponentInTransform(List<CachedDrawable> cache, Transform transform, Component componment)
	{
		var validComponents = FindValidComponents(componment);

		if (validComponents.Count > 0) {
			List<CachedEntry> cachedEntries = new List<CachedEntry>();

			GUIContent title = 
				new GUIContent($"{TransformHierarchyToTitle(transform, "\n")} - {componment.GetType().Name}");

			foreach (var validComponent in validComponents) {

				string fieldName = FormatTitle(validComponent);

				cachedEntries.Add(new CachedEntry
				{
					formattedFiedType = validComponent.fieldInfo?.FieldType.Name,
					formattedFieldName = fieldName,
					reflectionObjectWrapper = validComponent
				});

			}

			cache.Add(new CachedDrawable
			{
				entryName = title,
				entries = cachedEntries
			});
		}
	}

	private static string FormatTitle(ReflectionObjectWrapper e)
	{
		System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"^_|^._");
		string fieldName = regex.Replace(e.fieldInfo.Name, @"");
		fieldName = System.Text.RegularExpressions.Regex.Replace(fieldName, @"((?<=\p{Ll})\p{Lu})|((?!\A)\p{Lu}(?>\p{Ll}))", " $0");
		fieldName = System.Text.RegularExpressions.Regex.Replace(fieldName, @"^ {1,}", "");
		return System.Text.RegularExpressions.Regex.Replace(fieldName, @"^\w", m => m.Value.ToUpper());
	}

	private static string TransformHierarchyToTitle(Transform transform, string separator)
	{
		Transform current = transform;
		List<string> names = new List<string>();
		while(current != null) {
			names.Add(current.name);
			current = current.parent;
		}
		names.Reverse();
		return string.Join(separator, names);
	}

	private delegate void Action(int depth, Transform transform);
	private static void ForEachChildInTransform(Transform transform, Action action, int depth = 0)
	{
		action(depth, transform);
		for (int i = 0; i < transform.childCount; i++) {
			var child = transform.GetChild(i);
			ForEachChildInTransform(child, action, depth + 1);
		}
	}

	private static List<ReflectionObjectWrapper> FindValidComponents(Component component)
	{
		const System.Reflection.BindingFlags bindingFlags = 
			System.Reflection.BindingFlags.NonPublic 
			| System.Reflection.BindingFlags.Instance 
			| System.Reflection.BindingFlags.Public;

		List<ReflectionObjectWrapper> scriptableEvents 
			= new List<ReflectionObjectWrapper>();
	
		var type = component.GetType();
		var fields = type.GetFields(bindingFlags);
		foreach(var field in fields) {
			if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType)) {
				if(field.IsDefined(typeof(SerializeField), false) || field.IsPublic) {
					scriptableEvents.Add(new ReflectionObjectWrapper 
					{ 
						context = component,
						fieldInfo = field
					});
				}
			}
		}
		return scriptableEvents;
	}

	private struct CachedEntry
	{
		public string formattedFiedType;
		public string formattedFieldName;
		public ReflectionObjectWrapper reflectionObjectWrapper;
	}

	private class CachedDrawable
	{
		public bool isFolded;
		public GUIContent entryName;
		public List<CachedEntry> entries;

		public void Toggle()
		{
			isFolded = !isFolded;
		}
	}

	private struct ReflectionObjectWrapper
	{
		public Component context;
		public System.Reflection.FieldInfo fieldInfo;

		public object GetObject()
		{
			return fieldInfo.GetValue(context);
		}

		public void SetObject(object newEvent)
		{
			fieldInfo.SetValue(context, newEvent);
		}
	}

}
#endif