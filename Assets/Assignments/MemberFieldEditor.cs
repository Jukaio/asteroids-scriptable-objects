#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

/* NOTE: I willingly chose a more procedural approach rather than a pure OOP approach 
 * since I treat an EditorWindow/Debugging tool as their own little program and writing a full class
 * for each little characteristic of the EditorWindow (I.e. the Filter, and certain variations of fields)*/

// TODO: Availability for more custom types and such (Use Attribute)
// TODO: Implement Undo/Redo
// TODO: Break down class into smaller bits
// TODO: Make it work for properties that have setters and getters
// TODO: Make base class -> derived classes debug interface for unit and debug interface for scene
[System.Serializable]
public class MemberFieldEditor : EditorWindow
{
#region CustomizationFields
	private const string MENU_ITEM_PATH = "Window/Member Field Editor";
	private const string WINDOW_NAME = "Member Field Editor";
	private const string ASSET_SCENE_FILTER = "t:Scene";
	private const string REFRESH_BUTTON_TITLE = "Refresh";
	private const string CLOSE_BUTTON_TEXT = "Close";

	private const string FILTER_MENU_TITLE_TEXT = "Filter:";
	private const string FILTER_PLACEHOLDER_NAME = "...";
	private const string FILTER_EVERYTHING_NAME = "Everything";
	private const string FILTER_NO_FILTER_NAME = "No Filter";
	private const string FILTER_MIXED_NAME = "Mixed..";

	private const string SEARCH_TITLE_TEXT = "Search:";

	private const string CLEAR_TITLE_TEXT = "Clear";

	private const string EDIT_BUTTON_EDITABLE_OBJECT_TEXT = "EDIT";
	private const string EDIT_BUTTON_NULL_OBJECT_TEXT = "NULL";
	private const string EDIT_BUTTON_ACTIVE_INSPECTOR_IS_OF_OBJECT_TEXT = "Close";

	private const float INTERACTIVE_FIELD_WIDTH = 120.0f;
	private const float LABEL_FIELD_WIDTH = 180.0f;
	private const float INSPECTOR_AREA_HEIGHT_RATIO = 0.3f;
	private const float TOOLBAR_TO_MAIN_AREA_SPACE = 10.0f;
	private const float DOUBLED_INTERACTIVE_FIELD_LAYOUT_WIDTH = 240.0f;

	private static Vector2 MIN_SIZE_EDITOR_CLOSED => new Vector2(640, 300);
	private static Color DEFAULT_COLOR => Color.white;
	private static Color ACTIVE_EDITOR_IN_SELECTION_COLOUR_CHANGE => Color.green;
	private static Color ELEMENT_ACTIVE_FOLD_COLOR => Color.green;
	private static Color ELEMENT_INACTIVE_FOLD_COLOR => Color.magenta;

	private static GUILayoutOption[] LABEL_FIELD_LAYOUT_OPTIONS => 
		new GUILayoutOption[] { GUILayout.Width(LABEL_FIELD_WIDTH) };
	private static GUILayoutOption[] INTERACTIVE_FIELD_LAYOUT_OPTIONS => 
		new GUILayoutOption[] { GUILayout.Width(INTERACTIVE_FIELD_WIDTH) };
	private static GUILayoutOption[] DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS =>
		new GUILayoutOption[] { GUILayout.Width(DOUBLED_INTERACTIVE_FIELD_LAYOUT_WIDTH) };

	private static GUIStyle cachedElementHeaderStyle = null;
	private static GUIStyle ELEMENT_HEADER_STYLE
	{
		get
		{
			if(cachedElementHeaderStyle == null) {

				cachedElementHeaderStyle = new GUIStyle(EditorStyles.popup);
				cachedElementHeaderStyle.alignment = TextAnchor.MiddleCenter;
				cachedElementHeaderStyle.normal.textColor = Color.white;
				cachedElementHeaderStyle.fontStyle = FontStyle.Bold;
				cachedElementHeaderStyle.fontSize = 12;
			}
			return cachedElementHeaderStyle;
		}
	}
	private static GUIStyle cachedElementEntryTypeLabelStyle = null;
	private static GUIStyle ELEMENT_ENTRY_TYPE_LABEL_STYLE
	{
		get
		{
			if(cachedElementEntryTypeLabelStyle == null) {

				cachedElementEntryTypeLabelStyle = EditorStyles.boldLabel;
				cachedElementEntryTypeLabelStyle.normal.textColor = Color.white;
				cachedElementEntryTypeLabelStyle.fontStyle = FontStyle.Bold;
				cachedElementEntryTypeLabelStyle.alignment = TextAnchor.MiddleLeft;
				cachedElementEntryTypeLabelStyle.wordWrap = true;
			}
			return cachedElementEntryTypeLabelStyle;
		}
	}
	private static GUIStyle cachedElementEntryNameLabelStyle = null;
	private static GUIStyle ELEMENT_ENTRY_NAME_LABEL_STYLE
	{
		get
		{
			if (cachedElementEntryNameLabelStyle == null) {

				cachedElementEntryNameLabelStyle = EditorStyles.boldLabel;
				cachedElementEntryNameLabelStyle.normal.textColor = Color.white;
				cachedElementEntryNameLabelStyle.fontStyle = FontStyle.Normal;
				cachedElementEntryNameLabelStyle.alignment = TextAnchor.MiddleLeft;
				cachedElementEntryNameLabelStyle.wordWrap = true;
			}
			return cachedElementEntryTypeLabelStyle;
		}
	}
	private static GUIStyle cachedFilterMenuTitleStyle = null;
	private static GUIStyle TOOLBAR_LABEL_TITLE_STYLE
	{
		get
		{
			if(cachedFilterMenuTitleStyle == null) {

				cachedFilterMenuTitleStyle = new GUIStyle(GUI.skin.label);
				cachedFilterMenuTitleStyle.fontStyle = FontStyle.Bold;
				cachedFilterMenuTitleStyle.alignment = TextAnchor.UpperCenter;
			}
			return cachedFilterMenuTitleStyle;
		}
	}
	private static GUIStyle cachedToolBarButtonStyle = null;
	private static GUIStyle TOOLBAR_BUTTON_STYLE
	{
		get
		{
			if (cachedToolBarButtonStyle == null) {

				cachedToolBarButtonStyle = new GUIStyle(EditorStyles.toolbarButton);
				cachedToolBarButtonStyle.fontStyle = FontStyle.Bold;
			}
			return cachedToolBarButtonStyle;
		}
	}
	private static HashSet<System.Type> cachedLegalValueTypeLookUp = null;
	private static HashSet<System.Type> LegalValueTypeLookUp
	{
		get
		{
			if (cachedLegalValueTypeLookUp == null) {

				cachedLegalValueTypeLookUp = new HashSet<System.Type>();
				cachedLegalValueTypeLookUp.Add(typeof(UnityEngine.Object));
				cachedLegalValueTypeLookUp.Add(typeof(bool));
				cachedLegalValueTypeLookUp.Add(typeof(float));
				cachedLegalValueTypeLookUp.Add(typeof(double));
				cachedLegalValueTypeLookUp.Add(typeof(int));
				cachedLegalValueTypeLookUp.Add(typeof(long));
				cachedLegalValueTypeLookUp.Add(typeof(Vector2));
				cachedLegalValueTypeLookUp.Add(typeof(Vector3));
				cachedLegalValueTypeLookUp.Add(typeof(Vector4));
				cachedLegalValueTypeLookUp.Add(typeof(Vector2Int));
				cachedLegalValueTypeLookUp.Add(typeof(Vector3Int));
				cachedLegalValueTypeLookUp.Add(typeof(Bounds));
				cachedLegalValueTypeLookUp.Add(typeof(BoundsInt));
				cachedLegalValueTypeLookUp.Add(typeof(Rect));
				cachedLegalValueTypeLookUp.Add(typeof(RectInt));
				cachedLegalValueTypeLookUp.Add(typeof(AnimationCurve));
				cachedLegalValueTypeLookUp.Add(typeof(Color));
			}
			return cachedLegalValueTypeLookUp;
		}
	}
#endregion

#region ObjectStateBehaviour
	private void OnEnable()
	{
		scenes = FindAllScenesInProject();
		CacheValidElements(drawableElements, scenes);
	}

	public void OnGUI()
	{
		EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
		DrawSearchArea();
		DrawFilterMenu();
		DrawClearButton();
		DrawRefreshButton();
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.Space(TOOLBAR_TO_MAIN_AREA_SPACE);

		EditorGUILayout.BeginVertical();
		DrawComponentSelection();
		TryDrawActiveEditor();
		EditorGUILayout.EndVertical();
	}

	private void TryDrawActiveEditor()
	{
		if (activeEditor != null) {

			DrawActiveEditorHeader();
			DrawInspectorSectionOfSelectedField();
		}
	}

	private void DrawClearButton()
	{
		GUIContent clearLabelContent = new GUIContent(CLEAR_TITLE_TEXT);
		EditorStyles.toolbarButton.CalcMinMaxWidth(clearLabelContent, out float min, out float max);
		if (GUILayout.Button(clearLabelContent, EditorStyles.toolbarButton, 
				GUILayout.MinWidth(min), GUILayout.MaxWidth(max))) {

			filterTerm = string.Empty;
			filteredTypes.Clear();
		}
	}

	private void DrawSearchArea()
	{
		var labelContent = new GUIContent(SEARCH_TITLE_TEXT);
		TOOLBAR_LABEL_TITLE_STYLE.CalcMinMaxWidth(labelContent, out float min, out float max);
		EditorGUILayout.LabelField(labelContent, TOOLBAR_LABEL_TITLE_STYLE, GUILayout.MinWidth(min), GUILayout.MaxWidth(max));
		filterTerm = EditorGUILayout.TextField
		(
			filterTerm,
			EditorStyles.toolbarSearchField,
			DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS
		);
	}

	private void DrawRefreshButton()
	{
		var isRefreshing = GUILayout.Button(REFRESH_BUTTON_TITLE, TOOLBAR_BUTTON_STYLE);
		if (isRefreshing) {

			activeEditor = null;
			RefreshData();
		}
	}

	private void DrawFilterMenu()
	{
		var labelContent = new GUIContent(FILTER_MENU_TITLE_TEXT);
		TOOLBAR_LABEL_TITLE_STYLE.CalcMinMaxWidth(labelContent, out float min, out float max);
		EditorGUILayout.LabelField(labelContent, TOOLBAR_LABEL_TITLE_STYLE, GUILayout.MinWidth(min), GUILayout.MaxWidth(max));

		string label = FILTER_PLACEHOLDER_NAME;
		if (IsFilterEverything()) {
			label = FILTER_EVERYTHING_NAME;

		}
		else if (filteredTypes.Count == 1) {

			label = filteredTypes.First().Name;
		}
		else if (IsFilterNothing()) {

			label = FILTER_NO_FILTER_NAME;
		}
		else {

			label = FILTER_MIXED_NAME;
		}

		var buttonStyle = new GUIStyle(EditorStyles.toolbarButton);
		if (GUILayout.Button(label, buttonStyle, GUILayout.Width(LABEL_FIELD_WIDTH / 2))) {

			CreateFilterMenu().ShowAsContext();
		}
	}

	private GenericMenu CreateFilterMenu()
	{
		GenericMenu filterMenu = new GenericMenu();
		filterMenu.AddItem(new GUIContent(FILTER_NO_FILTER_NAME), IsFilterNothing(), FilterSetNothing);
		filterMenu.AddItem(new GUIContent(FILTER_EVERYTHING_NAME), IsFilterEverything(), FilterSetEverything);
		foreach (var item in LegalValueTypeLookUp) {
			filterMenu.AddItem(new GUIContent(item.Name), filteredTypes.Contains(item), FilterToggleState, item);
		}
		return filterMenu;
	}

	private void FilterToggleState(object that)
	{
		if (filteredTypes.Contains(that)) {

			filteredTypes.Remove((System.Type)that);
		}
		else {

			filteredTypes.Add((System.Type)that);
		}
	}

	private void FilterSetEverything()
	{
		if (!filteredTypes.Contains(typeof(Object))) {

			filteredTypes.Add(typeof(Object));
		}
		foreach (var item in LegalValueTypeLookUp) {

			if (!filteredTypes.Contains(item)) {

				filteredTypes.Add(item);
			}
		}
	}

	private void FilterSetNothing()
	{
		if (filteredTypes.Contains(typeof(Object))) {

			filteredTypes.Remove(typeof(Object));
		}
		foreach (var item in LegalValueTypeLookUp) {

			if (filteredTypes.Contains(item)) {

				filteredTypes.Remove(item);
			}
		}
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
		CacheValidElements(drawableElements, scenes);
	}

	private void DrawInspectorSectionOfSelectedField()
	{
		var isRemoving = GUILayout.Button(CLOSE_BUTTON_TEXT);

		using var scrollScope = 
			new EditorGUILayout.ScrollViewScope
			(
				inspectorScroll, 
				GUILayout.Height(position.height * INSPECTOR_AREA_HEIGHT_RATIO)
			);

		inspectorScroll = scrollScope.scrollPosition;

		activeEditor.OnInspectorGUI();
		if (isRemoving) {
			activeEditor = null;
		}
	}

	private void DrawComponentSelection()
	{
		using var scrollScope =
			new EditorGUILayout.ScrollViewScope(mainSectionScroll);

		mainSectionScroll = scrollScope.scrollPosition;
		DrawAllElements(this, drawableElements, ref activeEditor);
	}

	private bool IsFilterEverything()
	{
		return filteredTypes.Count >= LegalValueTypeLookUp.Count;
	}

	private bool IsFilterNothing()
	{
		return filteredTypes.Count == 0;
	}

	private bool IsActiveInFilter(System.Type type)
	{
		if (typeof(UnityEngine.Object).IsAssignableFrom(type)) {

			return filteredTypes.Contains(typeof(UnityEngine.Object));
		}
		return filteredTypes.Contains(type);
	}

	private bool IsNameFilteredIn(string that)
	{
		if (filterTerm.Length == 0) {

			return true;
		}
		return that.ToLower().Contains(filterTerm.ToLower());
	}
#endregion

#region StaticFunctionality
	// --- Static Functions to keep object state mutability as limited as possible --- //

	[MenuItem(MENU_ITEM_PATH)]
	public static void OpenWindow()
	{
		var window = GetWindow<MemberFieldEditor>(WINDOW_NAME);
		window.minSize = MIN_SIZE_EDITOR_CLOSED;
	}

	private static bool IsBaseTypeImplementedInEditor(System.Type type)
	{
		return LegalValueTypeLookUp.Contains(type);
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

	private static void DrawAllElements(MemberFieldEditor editor, in List<CachedDrawable> drawableElements, ref Editor activeEditor)
	{
		foreach (var element in drawableElements) {

			activeEditor = DrawAndTryGettingActiveInspector(editor, element, activeEditor);
		}
	}

	private static Editor DrawAndTryGettingActiveInspector(MemberFieldEditor editor, CachedDrawable element, Editor activeEditor)
	{
		var entries = GetFilteredEntryCollectionOfElement(editor, element);

		if (!(entries.Count() > 0)) {

			return activeEditor;
		}


		GUI.color = element.isFolded ? ELEMENT_ACTIVE_FOLD_COLOR : ELEMENT_INACTIVE_FOLD_COLOR;
		if (GUILayout.Button(element.entryName, ELEMENT_HEADER_STYLE)) {

			element.Toggle();
		}

		GUI.color = DEFAULT_COLOR;
		if (element.isFolded) {

			foreach (var entry in entries) {

				EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);

				EditorGUILayout.LabelField($"{entry.formattedFiedType}", 
					ELEMENT_ENTRY_TYPE_LABEL_STYLE, LABEL_FIELD_LAYOUT_OPTIONS);

				EditorGUILayout.LabelField($"{entry.formattedFieldName}", 
					ELEMENT_ENTRY_NAME_LABEL_STYLE, LABEL_FIELD_LAYOUT_OPTIONS);

				activeEditor = DecideTypeToDrawAndDrawField(activeEditor, entry);

				EditorGUILayout.EndHorizontal();
			}
		}

		EditorGUILayout.Space();

		return activeEditor;
	}

	private static IEnumerable<CachedEntry> GetFilteredEntryCollectionOfElement(MemberFieldEditor editor, CachedDrawable element)
	{
		return element.entries.Where((entry) =>
		{
			var isValidInTypeFilter = editor.IsFilterEverything()
				|| editor.IsFilterNothing()
				|| editor.IsActiveInFilter(entry.reflectionObjectWrapper.fieldInfo.FieldType);
			var isValidInTermFilter = editor.IsNameFilteredIn(entry.reflectionObjectWrapper.fieldInfo.Name);
			return isValidInTypeFilter && isValidInTermFilter;
		});
	}

	private static Editor DecideTypeToDrawAndDrawField(Editor activeEditor, CachedEntry entry)
	{
		var obj = entry.reflectionObjectWrapper.GetObject();
		var type = entry.reflectionObjectWrapper.fieldInfo.FieldType;

		/* To kill this absolute monster of an if/else control block...
		 * No need to - Later on it is possible to inject "priotity" object
		 * So the user can define custom drawable types - in 99% of the cases not needed */
		if (typeof(Object).IsAssignableFrom(type)) {

			DrawObjectElement(ref activeEditor, entry, obj);
		}
		else if (typeof(float).Equals(type)) {

			DrawFloatElement(entry);
		}
		else if (typeof(double).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.DoubleField((double)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(int).Equals(type)) { 

			DrawIntElement(entry);
		}
		else if (typeof(long).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.LongField((long)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(bool).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				var next = EditorGUILayout.Toggle((bool)boxedValue, INTERACTIVE_FIELD_LAYOUT_OPTIONS);
				EditorGUILayout.LabelField(string.Empty, INTERACTIVE_FIELD_LAYOUT_OPTIONS);
				return next;
			});
		}
		else if (typeof(Vector2).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.Vector2Field(string.Empty, (Vector2)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Vector3).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.Vector3Field(string.Empty, (Vector3)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Vector2Int).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.Vector2IntField(string.Empty, (Vector2Int)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Vector3Int).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.Vector3IntField(string.Empty, (Vector3Int)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Vector4).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.Vector4Field(string.Empty, (Vector4)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Bounds).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.BoundsField(string.Empty, (Bounds)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(BoundsInt).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.BoundsIntField(string.Empty, (BoundsInt)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(AnimationCurve).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.CurveField(string.Empty, (AnimationCurve)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Rect).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.RectField(string.Empty, (Rect)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(RectInt).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.RectIntField(string.Empty, (RectInt)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(string).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.TextField(string.Empty, (string)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		}
		else if (typeof(Color).Equals(type)) {

			DrawGenericElement(entry, boxedValue =>
			{
				return EditorGUILayout.ColorField(string.Empty, (Color)boxedValue, DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS);
			});
		} 

		return activeEditor;
	}

	private delegate object Assigner(object obj);
	private static void DrawGenericElement(CachedEntry entry, Assigner assign)
	{
		var value = entry.reflectionObjectWrapper.GetObject();
		entry.reflectionObjectWrapper.SetObject(assign(value));
	}

	private static void DrawFloatElement(CachedEntry entry) {

		float value = (float)entry.reflectionObjectWrapper.GetObject();
		var attributes = entry.reflectionObjectWrapper.fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true);

		if (attributes != null 
			&& attributes.Length > 0) {

			var find = attributes.Where(that => that.GetType() == typeof(RangeAttribute));
			var range = (RangeAttribute)find.First();

			entry.reflectionObjectWrapper.SetObject
			(
				EditorGUILayout.Slider
				(
					value, 
					range.min, 
					range.max, 
					DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS
				)
			);
		}
		else {

			entry.reflectionObjectWrapper.SetObject(EditorGUILayout.FloatField(value, INTERACTIVE_FIELD_LAYOUT_OPTIONS));
			EditorGUILayout.LabelField(string.Empty, INTERACTIVE_FIELD_LAYOUT_OPTIONS);
		}
	}

	private static void DrawIntElement(CachedEntry entry)
	{
		int value = (int)entry.reflectionObjectWrapper.GetObject();
		var attributes = entry.reflectionObjectWrapper.fieldInfo.GetCustomAttributes(typeof(RangeAttribute), true);
		if (attributes != null && attributes.Length > 0) {

			var find = attributes.Where(that => that.GetType() == typeof(RangeAttribute));
			var range = (RangeAttribute)find.First();

			entry.reflectionObjectWrapper.SetObject
			(
				EditorGUILayout.IntSlider
				(
					(int)value, 
					(int)range.min, 
					(int)range.max,
					DOUBLED_INTERACTIVE_FIELD_LAYOUT_OPTIONS
				)
			);
		}
		else {

			entry.reflectionObjectWrapper.SetObject(EditorGUILayout.IntField(value, INTERACTIVE_FIELD_LAYOUT_OPTIONS));
			EditorGUILayout.LabelField(string.Empty, INTERACTIVE_FIELD_LAYOUT_OPTIONS);
		}
	}

	private static void DrawObjectElement(ref Editor activeEditor, CachedEntry entry, object obj)
	{
		var style = new GUIStyle(GUI.skin.label);

		entry.reflectionObjectWrapper.SetObject(
			EditorGUILayout.ObjectField(obj as Object,
			entry.reflectionObjectWrapper.fieldInfo.FieldType, true,
			INTERACTIVE_FIELD_LAYOUT_OPTIONS)
		);
		
		if (obj != null) {

			GUI.color = activeEditor?.serializedObject.targetObject == obj 
				? ACTIVE_EDITOR_IN_SELECTION_COLOUR_CHANGE 
				: DEFAULT_COLOR;

			if (activeEditor?.serializedObject.targetObject == obj) {

				GUI.color = ACTIVE_EDITOR_IN_SELECTION_COLOUR_CHANGE;
				if (GUILayout.Button(EDIT_BUTTON_ACTIVE_INSPECTOR_IS_OF_OBJECT_TEXT, INTERACTIVE_FIELD_LAYOUT_OPTIONS)) {

					activeEditor = null;
				}
			}
			else {

				if (GUILayout.Button(EDIT_BUTTON_EDITABLE_OBJECT_TEXT, INTERACTIVE_FIELD_LAYOUT_OPTIONS)) {

					activeEditor = Editor.CreateEditor(obj as Object);
				}
			}
			GUI.color = DEFAULT_COLOR;
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
				new GUIContent($"({TransformHierarchyToTitle(transform, " - ")}) - {componment.GetType().Name}");

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
		action(depth, transform); // Replace this with your own functionality without using lambda

		for (int index = 0; index < transform.childCount; index++) {

			var child = transform.GetChild(index);
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
			if (typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType) 
				|| IsBaseTypeImplementedInEditor(field.FieldType)) {

				if (!field.IsDefined(typeof(HideInInspector), false) 
					&& (field.IsDefined(typeof(SerializeField), false) 
					|| field.IsPublic)) {

					scriptableEvents.Add(new ReflectionObjectWrapper 
					{ 
						context = component,
						fieldInfo = field,
					});
				}
			}
		}
		return scriptableEvents;
	}
#endregion

#region HelperDataStructures
	private class CachedEntry
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

	private class ReflectionObjectWrapper
	{
		public Component context;
		public System.Reflection.FieldInfo fieldInfo;

		public object GetObject()
		{
			return fieldInfo.GetValue(context);
		}

		public void SetObject(object newObject)
		{
			fieldInfo.SetValue(context, newObject);
		}
	}
#endregion

#region ObjectStateData
	private Editor activeEditor = null;
	private Scene[] scenes = null;
	private Vector2 mainSectionScroll = new Vector2(0, 0);
	private Vector2 inspectorScroll = new Vector2(0, 0);
	private List<CachedDrawable> drawableElements = new List<CachedDrawable>();
	private string filterTerm = string.Empty;
	private HashSet<System.Type> filteredTypes = new HashSet<System.Type>();
#endregion
}
#endif