using UnityEditor;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using Object = UnityEngine.Object;

/// <summary>
/// Editor window that consolidates four UI tools:
/// Button Reference, Font Preview, Color Manager, and Notes.
/// </summary>
public class UIBoardManager : EditorWindow 
{
    // Tabs available in the toolbar
    private enum Tabs { ButtonReference, FontPreview, ColorManager, Notes }

    // Currently selected tab
    private Tabs selectedTab;

    // Labels for each tab in the toolbar
    private readonly string[] tabLabels = { "Color Manager", "Button Reference", "Font Preview", "Notes" };

    // --- Button Reference Data ---
    /// <summary>
    /// Represents one category of button configurations.
    /// </summary>
    [Serializable]
    private class ButtonCategory
    {
        /// <summary>Name of this button category.</summary>
        public string name = "New Category";

        /// <summary>Whether the foldout is expanded or collapsed.</summary>
        public bool isExpanded = true;

        /// <summary>Which simulated state to preview (Normal, Hover, etc.).</summary>
        public SimulatedState simulatedState = SimulatedState.Normal;

        /// <summary>Whether to use color or sprite for preview.</summary>
        public VisualMode visualMode = VisualMode.Color;

        /// <summary>Width of the preview button.</summary>
        public float buttonWidth = 200f;

        /// <summary>Height of the preview button.</summary>
        public float buttonHeight = 50f;

        /// <summary>Text displayed on the preview button.</summary>
        public string buttonText = "Reference Button";

        /// <summary>Color used in Normal state.</summary>
        public Color normalColor = new Color(0.8f, 0.8f, 0.8f);

        /// <summary>Color used on Hover.</summary>
        public Color hoverColor = new Color(1f, 1f, 1f);

        /// <summary>Color used on Pressed.</summary>
        public Color pressedColor = new Color(0.6f, 0.6f, 0.6f);

        /// <summary>Color used on Selected.</summary>
        public Color selectedColor = new Color(0.4f, 0.7f, 1f);

        /// <summary>Color used when Disabled.</summary>
        public Color disabledColor = new Color(0.5f, 0.5f, 0.5f);

        /// <summary>Asset path for Normal-state sprite.</summary>
        public string normalSpritePath;

        /// <summary>Asset path for Hover-state sprite.</summary>
        public string hoverSpritePath;

        /// <summary>Asset path for Pressed-state sprite.</summary>
        public string pressedSpritePath;

        /// <summary>Asset path for Selected-state sprite.</summary>
        public string selectedSpritePath;

        /// <summary>Asset path for Disabled-state sprite.</summary>
        public string disabledSpritePath;

        /// <summary>Loaded Texture2D for Normal sprite.</summary>
        [NonSerialized] public Texture2D normalSprite;

        /// <summary>Loaded Texture2D for Hover sprite.</summary>
        [NonSerialized] public Texture2D hoverSprite;

        /// <summary>Loaded Texture2D for Pressed sprite.</summary>
        [NonSerialized] public Texture2D pressedSprite;

        /// <summary>Loaded Texture2D for Selected sprite.</summary>
        [NonSerialized] public Texture2D selectedSprite;

        /// <summary>Loaded Texture2D for Disabled sprite.</summary>
        [NonSerialized] public Texture2D disabledSprite;

        /// <summary>Scroll position for customization sub-section.</summary>
        [NonSerialized] public Vector2 customizationScroll;
    }

    /// <summary>Simulated button states available for preview.</summary>
    private enum SimulatedState { Normal, Hover, Pressed, Selected, Disabled }

    /// <summary>Visual mode: color fill or sprite image.</summary>
    private enum VisualMode     { Color, Sprite }

    /// <summary>Wrapper class for serializing list of ButtonCategory.</summary>
    [Serializable]
    private class CategoryWrapper { public List<ButtonCategory> categories; }

    private const string PrefKeyCategories = "ButtonReferenceTool.Categories";

    /// <summary>List of all button categories configured.</summary>
    private List<ButtonCategory> categories = new List<ButtonCategory>();

    /// <summary>Scroll position for entire Button Reference tab.</summary>
    private Vector2 overallScroll;

    /// <summary>Whether to show Help box in Button Reference tab.</summary>
    private bool showHelpButtonTab;

    // --- Font Preview Data ---
    /// <summary>Default preview text shown in Font Preview.</summary>
    private string previewText = "The quick brown fox jumps over the lazy dog";

    /// <summary>Filter string to narrow font list by name.</summary>
    private string filter = "";

    /// <summary>Scroll position for Font Preview list.</summary>
    private Vector2 fontScroll;

    /// <summary>All font assets found in the project.</summary>
    private List<Font> allFonts = new List<Font>();

    /// <summary>Whether to show Help box in Font Preview tab.</summary>
    private bool showHelpFont;

    /// <summary>Whether the font list is folded out.</summary>
    private bool showFontList = true;

    // --- Color Manager Data ---
    /// <summary>Represents one named color entry.</summary>
    [Serializable]
    private class ColorEntry { public string name; public Color color; }

    /// <summary>All saved named colors.</summary>
    private List<ColorEntry> savedColors = new List<ColorEntry>();

    /// <summary>Color picked for adding a new entry.</summary>
    private Color newColor = Color.white;

    /// <summary>Name for the new color entry.</summary>
    private string newColorName = "";

    /// <summary>Whether to show Help box in Color Manager tab.</summary>
    private bool showHelpColor;

    /// <summary>Whether the color list is folded out.</summary>
    private bool showColorList = true;

    /// <summary>Scroll position for Color Manager list.</summary>
    private Vector2 colorScroll;

    // --- Notes Data ---
    /// <summary>Represents one note with title & content.</summary>
    [Serializable]
    private class NoteEntry { public string title = "New Note"; public string content = ""; public bool isExpanded = true; }

    /// <summary>Wrapper class for serializing list of NoteEntry.</summary>
    [Serializable]
    private class NoteWrapper { public List<NoteEntry> notes; }

    /// <summary>All notes entered by the user.</summary>
    private List<NoteEntry> notes = new List<NoteEntry>();

    /// <summary>Scroll position for Notes tab.</summary>
    private Vector2 notesScroll;

    /// <summary>Whether to show Help box in Notes tab.</summary>
    private bool showHelpNotes;

    private const string PrefKeyNotes = "ButtonReferenceTool.Notes";

    /// <summary>Menu item to open the UI Board Manager window.</summary>
    [MenuItem("Tools/Julien Noe/UI Board Manager")]
    public static void Open() => GetWindow<UIBoardManager>("UI Board Manager");

    /// <summary>
    /// Called when the window is enabled; loads saved data and refreshes fonts.
    /// </summary>
    private void OnEnable()
    {
        LoadCategories();
        LoadColors();
        LoadNotes();
        previewText = EditorPrefs.GetString("FontPreview_PreviewText", previewText);
        RefreshFontList();
    }

    /// <summary>
    /// Called when the window is disabled; saves all data.
    /// </summary>
    private void OnDisable()
    {
        SaveCategories();
        SaveColors();
        SaveNotes();
        EditorPrefs.SetString("FontPreview_PreviewText", previewText);
    }

    /// <summary>
    /// Main GUI drawing entry point.
    /// </summary>
    private void OnGUI()
    {
        // Draw the top toolbar to switch between tabs
        selectedTab = (Tabs)GUILayout.Toolbar((int)selectedTab, tabLabels, GUILayout.Height(30));
        EditorGUILayout.Space(8);

        // Invoke the appropriate draw method
        switch (selectedTab)
        {
            case Tabs.ButtonReference: DrawButtonReferenceTab(); break;
            case Tabs.FontPreview:     DrawFontPreviewTab();     break;
            case Tabs.ColorManager:    DrawColorManagerTab();    break;
            case Tabs.Notes:           DrawNotesTab();           break;
        }
    }

    // -------- Button Reference Tab --------
    /// <summary>
    /// Draws the Button Reference tab, including Add/Remove and category editors.
    /// </summary>
    private void DrawButtonReferenceTab()
    {
        DrawHeader("Button Reference", ref showHelpButtonTab,
            "Create multiple button categories and preview their states with color or sprite customization.");

        // Add Category button (green)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Button", GUILayout.ExpandWidth(true)))
            categories.Add(new ButtonCategory());
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        // Scrollable area for category list
        overallScroll = EditorGUILayout.BeginScrollView(overallScroll,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));

        int removeIndex = -1;
        for (int i = 0; i < categories.Count; i++)
        {
            var cat = categories[i];

            // Header row: foldout + Remove button
            EditorGUILayout.BeginHorizontal();
            cat.isExpanded = EditorGUILayout.Foldout(cat.isExpanded, cat.name, true);
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                removeIndex = i;
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();

            // Only draw details if expanded
            if (!cat.isExpanded) continue;

            EditorGUI.indentLevel++;
            DrawCategoryEditor(cat);
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();

        // Perform removal outside loop
        if (removeIndex >= 0)
        {
            categories.RemoveAt(removeIndex);
            SaveCategories();
        }
    }

    // -------- Font Preview Tab --------
    /// <summary>
    /// Draws the Font Preview tab for scanning and previewing project fonts.
    /// </summary>
    private void DrawFontPreviewTab()
    {
        DrawHeader("Font Preview", ref showHelpFont,
            "Scan all font assets, preview custom text, filter by name, and select fonts.");

        EditorGUILayout.LabelField("Preview Settings", EditorStyles.boldLabel);
        previewText = EditorGUILayout.TextField("Preview Text", previewText);
        if (string.IsNullOrWhiteSpace(previewText))
            previewText = "The quick brown fox jumps over the lazy dog";

        EditorGUILayout.Space();
        filter = EditorGUILayout.TextField("Filter by Font Name", filter);

        // Refresh button (green)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Refresh List", GUILayout.Width(100)))
            RefreshFontList();
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();

        // Foldout for font list
        showFontList = EditorGUILayout.Foldout(showFontList, $"Fonts Found: {allFonts.Count}", true);
        if (showFontList)
        {
            fontScroll = EditorGUILayout.BeginScrollView(fontScroll);
            foreach (var font in allFonts)
            {
                if (!string.IsNullOrEmpty(filter) && !font.name.ToLower().Contains(filter.ToLower()))
                    continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.LabelField("Font: " + font.name, EditorStyles.boldLabel);
                var style = new GUIStyle(GUI.skin.label) { font = font, fontSize = 14, wordWrap = true };
                EditorGUILayout.LabelField(previewText, style, GUILayout.Height(40));

                if (GUILayout.Button("Select Font Asset"))
                {
                    Selection.activeObject = font;
                    EditorGUIUtility.PingObject(font);
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndScrollView();
        }
    }

    // -------- Color Manager Tab --------
    /// <summary>
    /// Draws the Color Manager tab to add, display, and delete named colors.
    /// </summary>
    private void DrawColorManagerTab()
    {
        DrawHeader("Color Manager", ref showHelpColor,
            "Manage named colors and save them to Unity's global color picker.");

        EditorGUILayout.LabelField("Add New Color", EditorStyles.boldLabel);
        newColorName = EditorGUILayout.TextField("Color Name", newColorName);
        newColor     = EditorGUILayout.ColorField("New Color", newColor);

        // Add button (green)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Color", GUILayout.Width(100)))
        {
            savedColors.Add(new ColorEntry { name = newColorName, color = newColor });
            SaveColors();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space();

        // Foldout list of saved colors
        showColorList = EditorGUILayout.Foldout(showColorList, $"Saved Colors: {savedColors.Count}", true);
        if (showColorList)
        {
            int deleteIndex = -1;
            colorScroll = EditorGUILayout.BeginScrollView(colorScroll);

            for (int i = 0; i < savedColors.Count; i++)
            {
                var entry = savedColors[i];
                EditorGUILayout.BeginHorizontal("box");

                entry.name  = EditorGUILayout.TextField(entry.name);
                entry.color = EditorGUILayout.ColorField(entry.color);
                EditorGUILayout.LabelField("#" + ColorUtility.ToHtmlStringRGB(entry.color), GUILayout.Width(100));

                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                    EditorGUIUtility.systemCopyBuffer = "#" + ColorUtility.ToHtmlStringRGB(entry.color);

                // Delete button (red)
                GUI.backgroundColor = Color.red;
                if (GUILayout.Button("Delete", GUILayout.Width(60)))
                    deleteIndex = i;
                GUI.backgroundColor = Color.white;

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            // Perform deletion outside loop
            if (deleteIndex >= 0)
            {
                savedColors.RemoveAt(deleteIndex);
                SaveColors();
            }
        }
    }

    // -------- Notes Tab --------
    /// <summary>
    /// Draws the Notes tab for creating, editing, and deleting notes.
    /// </summary>
    private void DrawNotesTab()
    {
        DrawHeader("Notes", ref showHelpNotes,
            "Add titled notes. Use 'Add Note' to create new entries and remove as needed.");

        EditorGUILayout.Space(10);

        // Add Note button (green)
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Add Note", GUILayout.ExpandWidth(true)))
            notes.Add(new NoteEntry());
        GUI.backgroundColor = Color.white;
        EditorGUILayout.Space(10);

        notesScroll = EditorGUILayout.BeginScrollView(notesScroll,
            GUIStyle.none, GUI.skin.verticalScrollbar, GUILayout.ExpandWidth(true));

        int removeIndex = -1;
        for (int i = 0; i < notes.Count; i++)
        {
            var note = notes[i];

            EditorGUILayout.BeginHorizontal();
            note.isExpanded = EditorGUILayout.Foldout(note.isExpanded, note.title, true);

            // Remove button (red)
            GUI.backgroundColor = Color.red;
            if (GUILayout.Button("Remove", GUILayout.Width(60)))
                removeIndex = i;
            GUI.backgroundColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (!note.isExpanded) continue;

            EditorGUI.indentLevel++;
            note.title   = EditorGUILayout.TextField("Title", note.title);
            EditorGUILayout.LabelField("Content", EditorStyles.boldLabel);
            note.content = EditorGUILayout.TextArea(note.content, GUILayout.Height(100), GUILayout.ExpandWidth(true));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(10);
        }

        EditorGUILayout.EndScrollView();

        // Perform removal outside loop
        if (removeIndex >= 0)
        {
            notes.RemoveAt(removeIndex);
            SaveNotes();
        }
    }

    // --- Helper methods ---

    /// <summary>
    /// Draws a header with title and Help dropdown for each tab.
    /// </summary>
    private void DrawHeader(string title, ref bool showHelp, string helpText)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(title, EditorStyles.boldLabel);
        GUILayout.FlexibleSpace();

        if (GUILayout.Button("Help", EditorStyles.toolbarDropDown, GUILayout.Width(60)))
            showHelp = !showHelp;

        EditorGUILayout.EndHorizontal();

        if (showHelp)
        {
            EditorGUILayout.HelpBox(helpText, MessageType.Info);
            EditorGUILayout.Space(10);
        }
    }

    /// <summary>
    /// Draws the editors for one ButtonCategory.
    /// </summary>
    private void DrawCategoryEditor(ButtonCategory cat)
    {
        DrawCategoryPreview(cat);
        DrawColorCustomization(cat);
        DrawSpriteCustomization(cat);
    }

    private void DrawCategoryPreview(ButtonCategory cat) { /* Unchanged preview logic */ }
    private void DrawColorCustomization(ButtonCategory cat) { /* Unchanged color UI */ }
    private void DrawSpriteCustomization(ButtonCategory cat) { /* Unchanged sprite UI */ }

    /// <summary>
    /// Draws a sprite field and returns the selected path.
    /// </summary>
    private Object DrawSpriteField(string label, Texture2D tex, out string path)
    {
        // Implementation omitted for brevity
        path = null;
        return null;
    }

    /// <summary>
    /// Scans the project for all Font assets and updates the list.
    /// </summary>
    private void RefreshFontList() { /* Unchanged font scanning logic */ }

    /// <summary>
    /// Saves the button categories to EditorPrefs.
    /// </summary>
    private void SaveCategories() { /* Unchanged serialization logic */ }

    /// <summary>
    /// Loads the button categories from EditorPrefs.
    /// </summary>
    private void LoadCategories() { /* Unchanged deserialization logic */ }

    /// <summary>
    /// Saves the named colors to EditorPrefs or a file.
    /// </summary>
    private void SaveColors() { /* Unchanged save logic */ }

    /// <summary>
    /// Loads the named colors from EditorPrefs or a file.
    /// </summary>
    private void LoadColors() { /* Unchanged load logic */ }

    /// <summary>
    /// Saves the notes list to EditorPrefs.
    /// </summary>
    private void SaveNotes()
    {
        EditorPrefs.SetString(PrefKeyNotes, JsonUtility.ToJson(new NoteWrapper { notes = notes }));
    }

    /// <summary>
    /// Loads the notes list from EditorPrefs.
    /// </summary>
    private void LoadNotes()
    {
        if (EditorPrefs.HasKey(PrefKeyNotes))
            notes = JsonUtility.FromJson<NoteWrapper>(EditorPrefs.GetString(PrefKeyNotes)).notes ?? new List<NoteEntry>();
        else
            notes = new List<NoteEntry>();
    }
}
