using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Reflection;
using PlasticGui;

[CustomEditor(typeof(WikiPage), true)]
[InitializeOnLoad]
public class ReadmeEditor : Editor
{
    static string s_ShowedReadmeSessionStateName = "ReadmeEditor.showedReadme";

    static string s_ReadmeSourceDirectory = "Assets/TutorialInfo";

    const float k_Space = 16f;

    WikiPage currentPage;
    Vector2 scrollPosition;
    bool currentlyEditing = false;
    string currentMDEditing = "";
    bool changedMDEditing = false;
    private MarkdownRenderer markdownRenderer;

    private SerializedProperty titleProperty;
    protected SerializedProperty markdownContent;
    private SerializedProperty iconProperty;

    static ReadmeEditor()
    {
        EditorApplication.delayCall += SelectReadmeAutomatically;
    }

    private static Color _DefaultBackgroundColor;
    public static Color DefaultBackgroundColor
    {
        get
        {
            if (_DefaultBackgroundColor.a == 0)
            {
                var method = typeof(EditorGUIUtility)
                    .GetMethod("GetDefaultBackgroundColor", BindingFlags.NonPublic | BindingFlags.Static);
                _DefaultBackgroundColor = (Color)method.Invoke(null, null);
            }
            return _DefaultBackgroundColor;
        }
    }

    private void OnEnable()
    {
        titleProperty = serializedObject.FindProperty("title");
        iconProperty = serializedObject.FindProperty("icon");
        markdownContent = serializedObject.FindProperty("markdownContent");
    }
    static void RemoveTutorial()
    {
        if (EditorUtility.DisplayDialog("Remove Readme Assets",

            $"All contents under {s_ReadmeSourceDirectory} will be removed, are you sure you want to proceed?",
            "Proceed",
            "Cancel"))
        {
            if (Directory.Exists(s_ReadmeSourceDirectory))
            {
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory);
                FileUtil.DeleteFileOrDirectory(s_ReadmeSourceDirectory + ".meta");
            }
            else
            {
                Debug.Log($"Could not find the Readme folder at {s_ReadmeSourceDirectory}");
            }

            var readmeAsset = SelectReadme();
            if (readmeAsset != null)
            {
                var path = AssetDatabase.GetAssetPath(readmeAsset);
                FileUtil.DeleteFileOrDirectory(path + ".meta");
                FileUtil.DeleteFileOrDirectory(path);
            }

            AssetDatabase.Refresh();
        }
    }

    static void SelectReadmeAutomatically()
    {
        if (!SessionState.GetBool(s_ShowedReadmeSessionStateName, false))
        {
            var readme = SelectReadme();
            SessionState.SetBool(s_ShowedReadmeSessionStateName, true);

            if (readme && !readme.loadedLayout)
            {
                LoadLayout();
                readme.loadedLayout = true;
            }
        }
    }

    static void LoadLayout()
    {
        var assembly = typeof(EditorApplication).Assembly;
        var windowLayoutType = assembly.GetType("UnityEditor.WindowLayout", true);
        var method = windowLayoutType.GetMethod("LoadWindowLayout", BindingFlags.Public | BindingFlags.Static);
        method.Invoke(null, new object[] { Path.Combine(Application.dataPath, "TutorialInfo/Layout.wlt"), false });
    }

    static Readme SelectReadme()
    {
        var ids = AssetDatabase.FindAssets("Readme t:Readme");
        if (ids.Length == 1)
        {
            var readmeObject = AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(ids[0]));

            Selection.objects = new UnityEngine.Object[] { readmeObject };

            return (Readme)readmeObject;
        }
        else
        {
            Debug.Log("Couldn't find a readme");
            return null;
        }
    }

    protected override void OnHeaderGUI()
    {
        showingSpecialBackgroundColor = false;
        currentPage = (WikiPage)target;
        Init();

        var iconWidth = Mathf.Min(EditorGUIUtility.currentViewWidth / 3f - 20f, 128f);

        GUILayout.BeginVertical("Big title");

        bool shouldShowHeader = !currentlyEditing && (currentPage.icon != null || (currentPage.title !=  null && !string.IsNullOrEmpty(currentPage.title)));
        GUILayout.BeginHorizontal("ButtonsTop");
        GUILayout.FlexibleSpace();
        // Navigation breadcrumb if this is a sub-page
        if (!(target is Readme) && !currentlyEditing)
        {
            if (GUILayout.Button("← Home", EditorStyles.miniButton, GUILayout.Width(60)))
            {
                SelectReadme();
            }
            GUILayout.Space(k_Space);
        }
        if (currentlyEditing && GUILayout.Button("Open .md", EditorStyles.miniButton, GUILayout.Width(80))) {
            string assetPath = AssetDatabase.GetAssetPath(target);
            string mdPath = Path.ChangeExtension(assetPath, ".md");
            if (File.Exists(mdPath)) {
                AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath));
            }
        }
        if (GUILayout.Button(currentlyEditing ? "Done" : "Edit", EditorStyles.miniButton, GUILayout.Width(60)))
        {
            currentlyEditing = !currentlyEditing;
            if (currentlyEditing)
            {
                string assetPath = AssetDatabase.GetAssetPath(target);
                string mdPath = Path.ChangeExtension(assetPath, ".md");
             
                if (File.Exists(mdPath))
                {
                    currentMDEditing = File.ReadAllText(mdPath);
                    changedMDEditing = false;
                }
                else
                {
                    File.WriteAllText(mdPath, "# " + ((WikiPage)target).title + "\n\nAdd your content here...");
                    AssetDatabase.Refresh();
                    AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath));
                }
            } else
            {
                if (changedMDEditing)
                {
                    string assetPath = AssetDatabase.GetAssetPath(target);
                    string mdPath = Path.ChangeExtension(assetPath, ".md");
                    File.WriteAllText(mdPath, currentMDEditing);
                    markdownContent.stringValue = null;
                    serializedObject.ApplyModifiedProperties();
                }
            }
        }
        GUILayout.EndHorizontal();

        if (currentlyEditing)
        {
            EditorGUILayout.BeginVertical();
            EditorGUILayout.PropertyField(titleProperty, new GUIContent("Title"));
            EditorGUILayout.PropertyField(iconProperty, new GUIContent("Icon"));
            EditorGUILayout.EndVertical();
            serializedObject.ApplyModifiedProperties();
            //currentPage.title = GUILayout.TextField(currentPage.title, TitleStyle);
        }
        if (shouldShowHeader)
        {
            GUILayout.BeginHorizontal("In BigTitle");
            {
                if (!currentlyEditing)
                {
                    if (currentPage.icon != null)
                    {
                        GUILayout.Space(k_Space);
                        GUILayout.Label(currentPage.icon, GUILayout.Width(iconWidth), GUILayout.Height(iconWidth));
                    }
                    GUILayout.Space(k_Space);
                }
                GUILayout.BeginVertical();
                { 
                    if (!currentlyEditing)
                    {
                        GUILayout.FlexibleSpace();
                        GUILayout.Label(currentPage.title, TitleStyle);
                        GUILayout.FlexibleSpace();
                    }
                }
                GUILayout.EndVertical();
                if (!currentlyEditing)
                    GUILayout.FlexibleSpace();
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndVertical();

        showingSpecialBackgroundColor = false;
    }

    Color originalBackgroundColor;
    bool _showingSpecialBackgroundColor = false;
    public bool showingSpecialBackgroundColor { get => _showingSpecialBackgroundColor; set
        {
            if (_showingSpecialBackgroundColor != value)
            {
                if (value) {
                    originalBackgroundColor = GUI.backgroundColor;
                    GUI.backgroundColor = DefaultBackgroundColor;
                } else
                {
                    GUI.backgroundColor = originalBackgroundColor;
                }
                _showingSpecialBackgroundColor = value;
            }
        }
    }

    public void UpdateMarkdownFile(string newFileContents)
    {
        dirtyFileContent = newFileContents;
    }

    string dirtyFileContent = "";
    public override void OnInspectorGUI()
    {
        var wikiPage = (WikiPage)target;
        showingSpecialBackgroundColor = true;
        
        if (markdownRenderer == null) markdownRenderer = new MarkdownRenderer();
        markdownRenderer.SetReadmeEditor(this);
        Init();

        if (currentlyEditing)
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUILayout.BeginVertical();
            this.markdownContent.stringValue = EditorGUILayout.TextArea(currentMDEditing, GUILayout.ExpandHeight(true), GUILayout.MinHeight(450f));

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndScrollView();
            if (this.markdownContent.stringValue != currentMDEditing)
            {
                currentMDEditing = this.markdownContent.stringValue;
                changedMDEditing = true;
                serializedObject.ApplyModifiedProperties();
            }
            
            return;
        }

        if (wikiPage.sections != null && wikiPage.sections.Length > 0)
            foreach (var section in wikiPage.sections)
            {
                if (!string.IsNullOrEmpty(section.heading))
                {
                    GUILayout.Label(section.heading, HeadingStyle);
                }

                if (!string.IsNullOrEmpty(section.text))
                {
                    GUILayout.Label(section.text, BodyStyle);
                }

                if (!string.IsNullOrEmpty(section.linkText))
                {
                    if (LinkLabel(new GUIContent(section.linkText)))
                    {
                        Application.OpenURL(section.url);
                    }
                }

                GUILayout.Space(k_Space);
            }

        EditorGUILayout.BeginVertical();
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // Try to load markdown content from external file
        string markdownContent = wikiPage.markdownContent;
        string assetPath = AssetDatabase.GetAssetPath(wikiPage);
        string mdPath = Path.ChangeExtension(assetPath, ".md");

        if (File.Exists(mdPath))
        {
            try
            {
                markdownContent = File.ReadAllText(mdPath);
            }
            catch (Exception e)
            {
                EditorGUILayout.HelpBox($"Error loading markdown file: {e.Message}", MessageType.Error);
            }
        }

        // Render markdown content
        if (!string.IsNullOrEmpty(markdownContent))
        {
            markdownRenderer.RenderMarkdown(markdownContent);
        }

        // Links section
        if (wikiPage.links != null && wikiPage.links.Length > 0)
        {
            EditorGUILayout.LabelField("Related Pages", HeadingStyle);
            foreach (var link in wikiPage.links)
            {
                if (link.targetPage != null)
                {
                    if (LinkLabel(new GUIContent(link.linkText)))
                    {
                        Selection.activeObject = link.targetPage;
                    }
                }
                else if (!string.IsNullOrEmpty(link.externalUrl))
                {
                    if (LinkLabel(new GUIContent(link.linkText + " ↗")))
                    {
                        Application.OpenURL(link.externalUrl);
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();


        showingSpecialBackgroundColor = false;
        // Handle repaint requests from markdown renderer
        if (GUI.changed)
        {
            Repaint();
            if (!string.IsNullOrEmpty(dirtyFileContent))
            {
                string getAssetPath = AssetDatabase.GetAssetPath(wikiPage);
                string getMdPath = Path.ChangeExtension(assetPath, ".md");
                File.WriteAllText(getMdPath, dirtyFileContent);
                dirtyFileContent = "";
            }
        }
    }

    bool m_Initialized;

    GUIStyle LinkStyle
    {
        get { return m_LinkStyle; }
    }

    [SerializeField]
    GUIStyle m_LinkStyle;

    GUIStyle TitleStyle
    {
        get { return m_TitleStyle; }
    }

    [SerializeField]
    GUIStyle m_TitleStyle;

    GUIStyle HeadingStyle
    {
        get { return m_HeadingStyle; }
    }

    [SerializeField]
    GUIStyle m_HeadingStyle;

    GUIStyle BodyStyle
    {
        get { return m_BodyStyle; }
    }

    [SerializeField]
    GUIStyle m_BodyStyle;

    GUIStyle ButtonStyle
    {
        get { return m_ButtonStyle; }
    }

    [SerializeField]
    GUIStyle m_ButtonStyle;

    void Init()
    {
        if (m_Initialized)
            return;

        markdownRenderer = new MarkdownRenderer();

        m_BodyStyle = new GUIStyle(EditorStyles.label);
        m_BodyStyle.wordWrap = true;
        m_BodyStyle.fontSize = 14;
        m_BodyStyle.richText = true;

        m_TitleStyle = new GUIStyle(m_BodyStyle);
        m_TitleStyle.fontSize = 26;
        m_TitleStyle.fontStyle = FontStyle.Bold;

        m_HeadingStyle = new GUIStyle(m_BodyStyle);
        m_HeadingStyle.fontStyle = FontStyle.Bold;
        m_HeadingStyle.fontSize = 18;

        m_LinkStyle = new GUIStyle(m_BodyStyle);
        m_LinkStyle.wordWrap = false;

        // Match selection color which works nicely for both light and dark skins
        m_LinkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
        m_LinkStyle.stretchWidth = false;

        m_ButtonStyle = new GUIStyle(EditorStyles.miniButton);
        m_ButtonStyle.fontStyle = FontStyle.Bold;

        m_Initialized = true;
    }

    bool LinkLabel(GUIContent label, params GUILayoutOption[] options)
    {
        var position = GUILayoutUtility.GetRect(label, LinkStyle, options);

        Handles.BeginGUI();
        Handles.color = LinkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMax, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, LinkStyle);
    }

    // Add this method to monitor external file changes
    void OnInspectorUpdate()
    {
        string assetPath = AssetDatabase.GetAssetPath(target);
        string mdPath = Path.ChangeExtension(assetPath, ".md");

        if (File.Exists(mdPath))
        {
            // Check if file has been modified
            if (EditorUtility.IsDirty(AssetDatabase.LoadAssetAtPath<TextAsset>(mdPath)))
            {
                Repaint();
            }
        }
    }
}
