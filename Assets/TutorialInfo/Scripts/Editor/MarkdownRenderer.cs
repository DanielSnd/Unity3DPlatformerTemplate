using UnityEngine;
using UnityEditor;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;

public class MarkdownRenderer
{
    private GUIStyle bodyStyle;
    private GUIStyle bodyStyleScratchedOff;
    private GUIStyle headingStyle1;
    private GUIStyle headingStyle2;
    private GUIStyle headingStyle3;
    private GUIStyle linkStyle;
    private GUIStyle toggleStyle;
    private GUIStyle codeStyle;
    private GUIStyle listStyle, textArea;

    private ReadmeEditor readmeEditor;
    public MarkdownRenderer()
    {
        InitializeStyles();
    }

    public void SetReadmeEditor(ReadmeEditor _editor)
    {
        readmeEditor = _editor;
    }

    private void InitializeStyles()
    {
        bodyStyle = new GUIStyle(EditorStyles.label);
        bodyStyle.wordWrap = true;
        bodyStyle.richText = true;
        bodyStyle.fontSize = 14;

        bodyStyleScratchedOff = new GUIStyle(EditorStyles.label);
        bodyStyleScratchedOff.wordWrap = true;
        bodyStyleScratchedOff.richText = true;
        bodyStyleScratchedOff.fontSize = 14;
        bodyStyleScratchedOff.normal.textColor = Color.gray;

        textArea = new GUIStyle(EditorStyles.textArea);

        toggleStyle = new GUIStyle(EditorStyles.toggle);

        headingStyle1 = new GUIStyle(bodyStyle);
        headingStyle1.fontSize = 26;
        headingStyle1.fontStyle = FontStyle.Bold;
        headingStyle1.normal.textColor = new Color(0.98f, 0.98f, 0.98f);

        headingStyle2 = new GUIStyle(bodyStyle);
        headingStyle2.fontSize = 20;
        headingStyle2.fontStyle = FontStyle.Bold;
        headingStyle2.normal.textColor = new Color(0.72f, 0.72f, 0.72f);

        headingStyle3 = new GUIStyle(bodyStyle);
        headingStyle3.fontSize = 16;
        headingStyle3.fontStyle = FontStyle.Bold;
        headingStyle3.normal.textColor = new Color(0.42f, 0.42f, 0.42f);

        linkStyle = new GUIStyle(bodyStyle);
        linkStyle.normal.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 1f);
        linkStyle.hover.textColor = new Color(0x00 / 255f, 0x78 / 255f, 0xDA / 255f, 0.8f);

        codeStyle = new GUIStyle(EditorStyles.textArea);
        codeStyle.wordWrap = true;
        codeStyle.fontSize = 12;
        codeStyle.fontStyle = FontStyle.Bold;

        listStyle = new GUIStyle(bodyStyle);
        listStyle.padding.left = 20;
    }

    private class CachedRenderOperation
    {
        public enum OperationType
        {
            Space,
            Label,
            Link,
            Image,
            CodeBlock,
            BeginHorizontal,
            EndHorizontal,
            BeginVertical,
            EndVertical,
            DrawRect,
            SelectableLabel
        }

        public OperationType Type { get; set; }
        public string Text { get; set; }
        public GUIStyle Style { get; set; }
        public float SpaceAmount { get; set; }
        public string LinkType { get; set; }
        public string LinkTarget { get; set; }
        public Texture2D Image { get; set; }
        public float ImageWidth { get; set; }
        public float ImageHeight { get; set; }
        public Color BackgroundColor { get; set; }
        public GUILayoutOption[] LayoutOptions { get; set; }
    }

    private List<CachedRenderOperation> cachedOperations;
    private string lastParsedMarkdown;
    private bool needsReparse = true;

    List<string> fileLines = new List<string>();

    private List<string> SplitIntoBlocks(string markdown)
    {
        return markdown.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.None).ToList();
    }

    public void RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return;

        // Only reparse if content has changed
        if (markdown != lastParsedMarkdown || needsReparse)
        {
            ParseMarkdown(markdown);
            lastParsedMarkdown = markdown;
            needsReparse = false;
        }

        // Execute cached render operations
        ExecuteCachedOperations();
    }

    private void ParseMarkdown(string markdown)
    {
        cachedOperations = new List<CachedRenderOperation>();
        modifiedFile = false;
        fileLines = SplitIntoBlocks(markdown);
        
        for (int i = 0; i < fileLines.Count; i++)
        {
            ParseBlock(fileLines[i], i);
        }

        if (modifiedFile && readmeEditor)
        {
            readmeEditor.UpdateMarkdownFile(string.Join("\n", fileLines));
        }
    }

    private void ExecuteCachedOperations()
    {
        Rect? lastRect = null;
        foreach (var operation in cachedOperations)
        {
            switch (operation.Type)
            {
                case CachedRenderOperation.OperationType.Space:
                    EditorGUILayout.Space(operation.SpaceAmount);
                    break;

                case CachedRenderOperation.OperationType.Label:
                    EditorGUILayout.LabelField(operation.Text, operation.Style);
                    break;

                case CachedRenderOperation.OperationType.Link:
                    if (LinkLabel(new GUIContent(operation.Text)))
                    {
                        HandleLinkClick(operation.LinkType, operation.LinkTarget);
                    }
                    break;

                case CachedRenderOperation.OperationType.Image:
                    EditorGUI.DrawPreviewTexture(
                        GUILayoutUtility.GetRect(operation.ImageWidth, operation.ImageHeight),
                        operation.Image
                    );
                    break;

                case CachedRenderOperation.OperationType.BeginHorizontal:
                    EditorGUILayout.BeginHorizontal();
                    break;

                case CachedRenderOperation.OperationType.EndHorizontal:
                    EditorGUILayout.EndHorizontal();
                    break;

                case CachedRenderOperation.OperationType.BeginVertical:
                    if (operation.Style != null && operation.LayoutOptions != null)
                    {
                        lastRect = EditorGUILayout.BeginVertical(operation.Style, operation.LayoutOptions);
                    }
                    else if (operation.Style != null)
                    {
                        lastRect = EditorGUILayout.BeginVertical(operation.Style);
                    }
                    else
                    {
                        EditorGUILayout.BeginVertical();
                    }
                    break;

                case CachedRenderOperation.OperationType.EndVertical:
                    EditorGUILayout.EndVertical();
                    break;

                case CachedRenderOperation.OperationType.DrawRect:
                    if (lastRect.HasValue)
                    {
                        EditorGUI.DrawRect(lastRect.Value, operation.BackgroundColor);
                    }
                    break;

                case CachedRenderOperation.OperationType.SelectableLabel:
                    if (operation.LayoutOptions != null)
                    {
                        EditorGUILayout.SelectableLabel(operation.Text, operation.Style, operation.LayoutOptions);
                    }
                    else
                    {
                        EditorGUILayout.SelectableLabel(operation.Text, operation.Style);
                    }
                    break;
            }
        }
    }

    bool isInCodeBlock = false;
    private StringBuilder codeBlockContent;

    private void ParseBlock(string block, int lineIndex)
    {
        if (string.IsNullOrWhiteSpace(block) && !isInCodeBlock)
        {
            wasPreviouslyRenderedBlockList = false;
            wasPreviouslyRenderedBlockHeader = false;
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.Space,
                SpaceAmount = currentSpaceBetweenBlocks
            });
            currentSpaceBetweenBlocks = 5;
            return;
        }

        string originalBlock = block;
        int indentationCount = 0;
        int extraIndentation = 0;
        block = block.Trim();

        // Handle code block start/end
        if (block.StartsWith("```")) {
            if (!isInCodeBlock) {
                isInCodeBlock = true;
                codeBlockContent = new StringBuilder();
                // Skip the language identifier if present
                return;
            } else {
                isInCodeBlock = false;
                RenderCodeBlock(codeBlockContent.ToString());
                codeBlockContent = null;
                return;
            }
        }

        if (isInCodeBlock) {
            codeBlockContent.AppendLine(originalBlock);
            return;
        }

        for (int i = 0; i < originalBlock.Length; i++) {
            if (originalBlock[i] == '\t') extraIndentation += 4;
            if (block.Length > 0 && originalBlock[i] == block[0]) {
                indentationCount = i + extraIndentation;
                break;
            }
        }

        bool isList = block.StartsWith("- ") || block.StartsWith("* ");

        int drawBefore = wasPreviouslyRenderedBlockHeader && isList ? 0 : currentSpaceBetweenBlocks;
        
        currentSpaceBetweenBlocks = 5;

        if (isList)
        {
            if(drawBefore > 0 && renderSpaceBeforeNextBlock)
                cachedOperations.Add(new CachedRenderOperation {
                    Type = CachedRenderOperation.OperationType.Space,
                    SpaceAmount = drawBefore
                });
            
            RenderList(block, indentationCount, lineIndex);

            currentSpaceBetweenBlocks = 0;
            wasPreviouslyRenderedBlockList = true;
            wasPreviouslyRenderedBlockHeader = false;
        }else
        {
            if (drawBefore > 0 && renderSpaceBeforeNextBlock)
                cachedOperations.Add(new CachedRenderOperation {
                    Type = CachedRenderOperation.OperationType.Space,
                    SpaceAmount = drawBefore
                });

            if (wasPreviouslyRenderedBlockList)
                cachedOperations.Add(new CachedRenderOperation {
                    Type = CachedRenderOperation.OperationType.Space,
                    SpaceAmount = currentSpaceBetweenBlocks + 5
                });
            
            wasPreviouslyRenderedBlockList = false;
            bool renderedHeading = false;
            if (block.StartsWith("# "))
            {
                cachedOperations.Add(new CachedRenderOperation {
                    Type = CachedRenderOperation.OperationType.Space,
                    SpaceAmount = 6
                });
                RenderHeading(block.Substring(2), headingStyle1, indentationCount, ref renderedHeading);
            }
            else if (block.StartsWith("## "))
            {
                cachedOperations.Add(new CachedRenderOperation {
                    Type = CachedRenderOperation.OperationType.Space,
                    SpaceAmount = 6
                });
                RenderHeading(block.Substring(3), headingStyle2, indentationCount, ref renderedHeading);
            }
            else if (block.StartsWith("### "))
            {
                RenderHeading(block.Substring(4), headingStyle3, indentationCount, ref renderedHeading);
            }
            else if (block.StartsWith("```"))
            {
                RenderCodeBlock(block);
            }
            else
            {
                RenderParagraph(block, indentationCount);
            }

            wasPreviouslyRenderedBlockHeader = renderedHeading;
        }

        renderSpaceBeforeNextBlock = true;
    }

    private void RenderHeading(string text, GUIStyle style, int indentationCount, ref bool renderedHeading)
    {
        text = ProcessInlineFormatting(text);
        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.Label,
            Text = text,
            Style = style
        });
        renderedHeading = true;
    }

    private void RenderParagraph(string text, int indentationCount)
    {
        if (string.IsNullOrEmpty(text.Trim()))
        {
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.Space,
                SpaceAmount = 5
            });
            return;
        }
        text = ProcessInlineFormatting(text);
        // Split the text into segments (link and non-link parts)
        var segments = SplitTextIntoSegments(text);
        if (indentationCount > 0)
        {
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.BeginHorizontal
            });
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.Space,
                SpaceAmount = indentationCount
            });
        }
        EditorGUILayout.BeginVertical();
        var lineContent = new StringBuilder();

        foreach (var segment in segments)
        {
            if (segment.isImage)
            {
                // Flush any accumulated text first
                if (lineContent.Length > 0)
                {
                    cachedOperations.Add(new CachedRenderOperation {
                        Type = CachedRenderOperation.OperationType.Label,
                        Text = lineContent.ToString(),
                        Style = bodyStyle
                    });
                    lineContent.Clear();
                }

                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(segment.target);
                if (texture != null)
                {
                    float maxWidth = EditorGUIUtility.currentViewWidth - 40;
                    float aspectRatio = (float)texture.width / texture.height;
                    float width = Mathf.Min(maxWidth, texture.width);
                    float height = width / aspectRatio;
                    
                    cachedOperations.Add(new CachedRenderOperation {
                        Type = CachedRenderOperation.OperationType.Image,
                        Image = texture,
                        ImageWidth = width,
                        ImageHeight = height
                    });
                }
                continue;
            }

            if (segment.isLink)
            {
                // Flush any accumulated regular text
                if (lineContent.Length > 0)
                {
                    cachedOperations.Add(new CachedRenderOperation {
                        Type = CachedRenderOperation.OperationType.Label,
                        Text = lineContent.ToString(),
                        Style = bodyStyle
                    });
                    lineContent.Clear();
                }

                // Render the link
                if (LinkLabel(new GUIContent(segment.displayText)))
                {
                    HandleLinkClick(segment.linkType, segment.target);
                }
            }
            else
            {
                lineContent.Append(segment.text);
            }
        }

        // Flush any remaining text
        if (lineContent.Length > 0)
        {
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.Label,
                Text = lineContent.ToString(),
                Style = bodyStyle
            });
        }

        EditorGUILayout.EndVertical();

        if (indentationCount > 0)
        {
            cachedOperations.Add(new CachedRenderOperation {
                Type = CachedRenderOperation.OperationType.EndHorizontal
            });
        }
    }

    private class TextSegment
    {
        public string text;
        public bool isLink;
        public bool isImage;
        public string linkType;
        public string target;
        public string displayText;
    }

    private List<TextSegment> SplitTextIntoSegments(string text)
    {
        var segments = new List<TextSegment>();
        // First split by image tags since they don't have closing tags
        var parts = Regex.Split(text, @"(<image=.*?>)");
        
        foreach (var part in parts)
        {
            if (part.StartsWith("<image="))
            {
                // Extract path from <image=path>
                var path = part.Substring(7, part.Length - 8); // Remove <image= and >
                segments.Add(new TextSegment
                {
                    isImage = true,
                    isLink = false,
                    target = path
                });
            }
            else if (!string.IsNullOrEmpty(part))
            {
                // Process remaining text for links
                var linkMatches = Regex.Matches(part, @"<link=""(.+?):(.+?)"">(.*?)</link>");
                int lastIndex = 0;

                foreach (Match match in linkMatches)
                {
                    // Add text before the link
                    if (match.Index > lastIndex)
                    {
                        segments.Add(new TextSegment
                        {
                            text = part.Substring(lastIndex, match.Index - lastIndex),
                            isLink = false,
                            isImage = false
                        });
                    }

                    segments.Add(new TextSegment
                    {
                        isLink = true,
                        isImage = false,
                        linkType = match.Groups[1].Value,
                        target = match.Groups[2].Value,
                        displayText = match.Groups[3].Value
                    });

                    lastIndex = match.Index + match.Length;
                }

                // Add remaining text after last link
                if (lastIndex < part.Length)
                {
                    segments.Add(new TextSegment
                    {
                        text = part.Substring(lastIndex),
                        isLink = false,
                        isImage = false
                    });
                }
            }
        }

        return segments;
    }

    private void HandleLinkClick(string linkType, string target)
    {
        switch (linkType)
        {
            case "asset":
                var wikiPage = AssetDatabase.LoadAssetAtPath<WikiPage>(target);
                if (wikiPage != null)
                {
                    Selection.activeObject = wikiPage;
                }
                break;

            case "file":
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(target);
                if (asset != null)
                {
                    AssetDatabase.OpenAsset(asset);
                }
                break;

            case "url":
                Application.OpenURL(target);
                break;
        }
    }

    private bool LinkLabel(GUIContent label)
    {
        var position = GUILayoutUtility.GetRect(label, linkStyle);

        Handles.BeginGUI();
        Handles.color = linkStyle.normal.textColor;
        Handles.DrawLine(new Vector3(position.xMin, position.yMax), new Vector3(position.xMin + label.text.Length * 7f, position.yMax));
        Handles.color = Color.white;
        Handles.EndGUI();

        EditorGUIUtility.AddCursorRect(position, MouseCursor.Link);

        return GUI.Button(position, label, linkStyle);
    }

    private void RenderList(string block, int indentationCount, int lineIndex)
    {
        var items = block.Split('\n')
            .Where(line => line.StartsWith("- ") || line.StartsWith("* "))
            .Select(line => line.Substring(2));

        foreach (var item in items)
        {
            EditorGUILayout.BeginHorizontal();
            if (indentationCount > 0)
                EditorGUILayout.Space(indentationCount * 5.0f, false);

            readmeEditor.showingSpecialBackgroundColor = false;

            bool drewToggle = false;
            bool toggleStatus = false;
            string useLine = item;
            if (useLine.StartsWith("[ ]"))
                useLine = DrawToggle(useLine, false, ref drewToggle, 3, lineIndex);
            else if (useLine.StartsWith("[]"))
                useLine = DrawToggle(useLine, false, ref drewToggle, 2, lineIndex);
            else if (useLine.StartsWith("[x]"))
            { 
                useLine = DrawToggle(useLine, true, ref drewToggle, 3, lineIndex);
                toggleStatus = true;
            }

            readmeEditor.showingSpecialBackgroundColor = true;

            if (!drewToggle)
                EditorGUILayout.LabelField("•", GUILayout.Width(15));

            var segments = SplitTextIntoSegments(ProcessInlineFormatting(useLine));
            var lineContent = new StringBuilder();
            
            foreach (var segment in segments)
            {
                if (segment.isLink)
                {
                    // Flush any accumulated regular text
                    if (lineContent.Length > 0)
                    {
                        cachedOperations.Add(new CachedRenderOperation {
                            Type = CachedRenderOperation.OperationType.Label,
                            Text = lineContent.ToString(),
                            Style = bodyStyle
                        });
                        lineContent.Clear();
                    }

                    // Render the link
                    if (LinkLabel(new GUIContent(segment.displayText)))
                    {
                        HandleLinkClick(segment.linkType, segment.target);
                    }
                }
                else
                {
                    lineContent.Append(segment.text);
                }
            }

            // Flush any remaining text
            if (lineContent.Length > 0)
            {
                if (drewToggle && toggleStatus) {
                    string strikethrough = "";
                    bool foundFirstNonSpace = false;
                    foreach (char c in lineContent.ToString())
                    {
                        if (c != ' ' || foundFirstNonSpace)
                        {
                            strikethrough = strikethrough + c + ('\u0336');
                            foundFirstNonSpace = true;
                        }
                        else{
                            strikethrough = strikethrough + c;
                        }
                    }
                    cachedOperations.Add(new CachedRenderOperation {
                        Type = CachedRenderOperation.OperationType.Label,
                        Text = strikethrough,
                        Style = bodyStyleScratchedOff
                    });

                } else
                {
                    cachedOperations.Add(new CachedRenderOperation {
                        Type = CachedRenderOperation.OperationType.Label,
                        Text = lineContent.ToString(),
                        Style = bodyStyle
                    });
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        string DrawToggle(string useLine, bool status, ref bool _drewToggle, int uselineSubstring = 3, int lineIndex = -1)
        {
            bool newToggleValue = EditorGUILayout.Toggle(status, toggleStyle, GUILayout.Width(15));
            useLine = useLine.Substring(uselineSubstring);
            _drewToggle = true;
            if (newToggleValue != status && lineIndex > 0 && lineIndex < fileLines.Count)
            {
                if (newToggleValue) {
                    fileLines[lineIndex] = fileLines[lineIndex].Replace("- []", "- [x]");
                    fileLines[lineIndex] = fileLines[lineIndex].Replace("- [ ]", "- [x]");
                } else
                {
                    fileLines[lineIndex] = fileLines[lineIndex].Replace("- [x]", "- [ ]");
                }
                modifiedFile = true;
            }
            return useLine;
        }
    }
    bool wasPreviouslyRenderedBlockList = false;
    bool wasPreviouslyRenderedBlockHeader = false;
    bool renderSpaceBeforeNextBlock = false;
    int currentSpaceBetweenBlocks = 5;
    bool modifiedFile = false;
    private string ProcessInlineFormatting(string text)
    {
        // Bold
        text = Regex.Replace(text, @"\*\*(.+?)\*\*", "<b>$1</b>");
        text = Regex.Replace(text, @"__(.+?)__", "<b>$1</b>");

        // Italic
        text = Regex.Replace(text, @"\*(.+?)\*", "<i>$1</i>");
        text = Regex.Replace(text, @"_(.+?)_", "<i>$1</i>");

        // Inline code
        text = Regex.Replace(text, @"`(.+?)`", "<color=#bdc4cb><b>$1</b></color>");

        // Handle images and links
        text = Regex.Replace(text, @"(!?)\[(.+?)\]\((.+?)\)", match => {
            var isImage = match.Groups[1].Value == "!";
            var altText = match.Groups[2].Value;
            var path = match.Groups[3].Value;

            // Skip URLs
            if (path.StartsWith("http://") || path.StartsWith("https://"))
            {
                if (isImage)
                {
                    // TODO: Could add web image loading here if needed
                    return $"<color=grey>[External Image: {altText}]</color>";
                }
                return $"<link=\"url:{path}\">{altText}</link>";
            }

            // Try direct asset path first
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset == null)
            {
                // Handle paths that start with a forward slash by treating them as relative to Assets/
                if (path.StartsWith("/"))
                {
                    string assetPath = "Assets" + path;
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        path = assetPath;
                    }
                }
                // Try adding Assets/ prefix if not present
                else if (!path.StartsWith("Assets/"))
                {
                    string assetPath = "Assets/" + path;
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                    if (asset != null)
                    {
                        path = assetPath;
                    }
                }

                // If still not found, try relative to current file
                if (asset == null && readmeEditor?.target != null)
                {
                    try
                    {
                        string currentFilePath = AssetDatabase.GetAssetPath(readmeEditor.target);
                        string currentDir = Path.GetDirectoryName(currentFilePath);
                        string fullPath = Path.GetFullPath(Path.Combine(currentDir, path.TrimStart('/')));
                        
                        // Normalize paths for comparison
                        string normalizedFullPath = Path.GetFullPath(fullPath).Replace('\\', '/');
                        string normalizedDataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
                        
                        // Debug.Log($"Normalized paths:\n{normalizedFullPath}\n{normalizedDataPath}");
                        
                        // Only proceed if the path is within the Assets folder
                        if (normalizedFullPath.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
                        {
                            string relativePath = "Assets" + normalizedFullPath.Substring(normalizedDataPath.Length).Replace('\\', '/');
                            asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(relativePath);
                            // Debug.Log($"relativePath: {relativePath}");
                            if (asset != null)
                            {
                                path = relativePath;
                            }
                        }
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning($"Invalid path characters in: {path}");
                    }
                }
            }

            if (asset != null)
            {
                if (isImage && asset is Texture2D)
                {
                    return $"<image={path}>";  // Special tag we'll handle in rendering
                }
                return $"<link=\"file:{path}\">{altText}</link>";
            }
            
            // Asset not found
            if (isImage)
            {
                return $"<color=red>[Missing Image: {path}]</color>";
            }
            return $"<link=\"url:{path}\">{altText}</link>";
        });

        text = Regex.Replace(text, @"\[\[WikiPage:(.+?)\]\]", match => {
            var path = "Assets/" + match.Groups[1].Value;
            if (!path.Contains("."))
                path = path + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<WikiPage>(path.TrimStart().TrimEnd());
            if (asset != null)
            {
                string displayTitle = !string.IsNullOrEmpty(asset.title) ? asset.title : Path.GetFileNameWithoutExtension(path);
                return $"<link=\"asset:{path}\">{displayTitle}</link>";
            }
            return $"<color=red>Missing: {path}</color>";
        });

        text = Regex.Replace(text, @"\[\[File:(.+?)\]\]", match => {
            var path = "Assets/" + match.Groups[1].Value;
            var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (asset != null)
            {
                return $"<link=\"file:{path}\">{Path.GetFileName(path)}</link>";
            }
            return $"<color=red>Missing: {path}</color>";
        });

        return text;
    }

    public bool HandleLinks(string text, Vector2 position)
    {
        var linkMatches = Regex.Matches(text, @"\[(.+?)\]\((.+?)\)");
        foreach (Match match in linkMatches)
        {
            var linkText = match.Groups[1].Value;
            var url = match.Groups[2].Value;

            // Calculate link position and check if clicked
            var linkRect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown &&
                linkRect.Contains(Event.current.mousePosition))
            {
                if (url.StartsWith("http"))
                {
                    Application.OpenURL(url);
                    return true;
                }
                // Handle internal page links here
            }
        }
        return false;
    }

    private void RenderCodeBlock(string code)
    {
        // C# keywords to highlight
        string[] keywords = new string[] {
            "public", "private", "protected", "internal", "class", "struct", "interface",
            "void", "string", "int", "float", "bool", "var", "new", "return", "if", "else",
            "for", "foreach", "while", "do", "switch", "case", "break", "continue", "static",
            "readonly", "const", "using", "namespace", "ref", "out", "in", "null", "true", "false", "base", "override", "virtual", "sealed", "abstract", "event", "delegate", "enum", "struct", "interface", "class", "struct", "interface", "this",
            "using", "namespace", "ref", "out", "in", "null", "true", "false"
        };


        var lineContent = new StringBuilder();
        var lines = code.Split('\n');
        int linesCount = lines.Length;
        lineContent.AppendLine(" ");
        
        foreach (var line in lines)
        {
            string processedLine = line;

            // Handle comments first
            int commentIndex = line.IndexOf("//");
            string comment = "";
            if (commentIndex >= 0)
            {
                comment = line.Substring(commentIndex);
                processedLine = line.Substring(0, commentIndex);
            }

            // Highlight keywords
            foreach (var keyword in keywords)
            {
                processedLine = Regex.Replace(
                    processedLine,
                    $@"\b{keyword}\b",
                    $"<color=#569CD6>{keyword}</color>"
                );
            }

            // Handle string literals
            processedLine = Regex.Replace(
                processedLine,
                "\".*?\"",
                m => $"<color=#CE9178>{m.Value}</color>"
            );

            // Add back comments with different color
            if (!string.IsNullOrEmpty(comment))
            {
                processedLine += $"<color=#57A64A>{comment}</color>";
            }
            
            lineContent.AppendLine(processedLine);
        }

        var height = linesCount * EditorGUIUtility.singleLineHeight;
        var guiLayoutHeight = GUILayout.Height(height);
        
        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.BeginVertical,
            Style = EditorStyles.helpBox,
            LayoutOptions = new[] { GUILayout.ExpandHeight(true), guiLayoutHeight }
        });

        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.DrawRect,
            BackgroundColor = new Color(0.2f, 0.2f, 0.2f)
        });

        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.Space,
            SpaceAmount = 2
        });

        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.SelectableLabel,
            Text = lineContent.ToString(),
            Style = bodyStyle,
            LayoutOptions = new[] { guiLayoutHeight }
        });

        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.EndVertical
        });

        cachedOperations.Add(new CachedRenderOperation {
            Type = CachedRenderOperation.OperationType.Space,
            SpaceAmount = 2
        });
    }

    public void InvalidateCache()
    {
        needsReparse = true;
    }
}