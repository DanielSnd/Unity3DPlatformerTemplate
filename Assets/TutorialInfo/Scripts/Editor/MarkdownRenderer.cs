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
    private List<Action> cachedGuiCalls = new List<Action>();
    private string lastParsedMarkdown;
    private bool needsReparsing = true;

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

    public void RenderMarkdown(string markdown)
    {
        if (string.IsNullOrEmpty(markdown)) return;

        if (markdown != lastParsedMarkdown || needsReparsing)
        {
            modifiedFile = false;
            cachedGuiCalls.Clear();
            fileLines = SplitIntoBlocks(markdown);
            
            for (int i = 0; i < fileLines.Count; i++) 
            {
                CacheBlock(fileLines[i], i);
            }
            
            lastParsedMarkdown = markdown;
            needsReparsing = false;
        }

        foreach (var guiCall in cachedGuiCalls)
        {
            guiCall();
        }

        if (modifiedFile && readmeEditor) 
        {
            readmeEditor.UpdateMarkdownFile(string.Join("\n", fileLines));
        }
    }

    List<string> fileLines = new List<string>();
    
    private List<string> SplitIntoBlocks(string markdown)
    {
        return markdown.Split(new[] { "\n", "\r\n" }, System.StringSplitOptions.None).ToList();
    }

    bool wasPreviouslyRenderedBlockList = false;
    bool wasPreviouslyRenderedBlockHeader = false;
    bool renderSpaceBeforeNextBlock = false;
    int currentSpaceBetweenBlocks = 5;
    bool modifiedFile = false;
    private bool isInCodeBlock = false;
    private StringBuilder codeBlockContent;

    private void CacheBlock(string block, int lineIndex)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            wasPreviouslyRenderedBlockList = false;
            wasPreviouslyRenderedBlockHeader = false;
            cachedGuiCalls.Add(() => EditorGUILayout.Space(currentSpaceBetweenBlocks));
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
            if (block.Length > 0 && originalBlock[i] == block[0])
            {
                indentationCount = i + extraIndentation;
                break;
            }
        }

        bool isList = block.StartsWith("- ") || block.StartsWith("* ");
        int drawBefore = wasPreviouslyRenderedBlockHeader && isList ? 0 : currentSpaceBetweenBlocks;
        currentSpaceBetweenBlocks = 5;

        if (isList)
        {
            if (drawBefore > 0 && renderSpaceBeforeNextBlock)
            {
                int spaceToDraw = drawBefore;
                cachedGuiCalls.Add(() => GUILayout.Space(spaceToDraw));
            }

            CacheListBlock(block, indentationCount, lineIndex);
            currentSpaceBetweenBlocks = 0;
            wasPreviouslyRenderedBlockList = true;
            wasPreviouslyRenderedBlockHeader = false;
        }
        else
        {
            if (drawBefore > 0 && renderSpaceBeforeNextBlock)
            {
                int spaceToDraw = drawBefore;
                cachedGuiCalls.Add(() => GUILayout.Space(spaceToDraw));
            }

            if (wasPreviouslyRenderedBlockList)
            {
                cachedGuiCalls.Add(() => GUILayout.Space(currentSpaceBetweenBlocks + 5));
            }

            wasPreviouslyRenderedBlockList = false;
            bool renderedHeading = false;

            if (block.StartsWith("# "))
            {
                string text = block.Substring(2);
                cachedGuiCalls.Add(() => RenderHeading(text, headingStyle1, indentationCount, ref renderedHeading));
            }
            else if (block.StartsWith("## "))
            {
                string text = block.Substring(3);
                cachedGuiCalls.Add(() => RenderHeading(text, headingStyle2, indentationCount, ref renderedHeading));
            }
            else if (block.StartsWith("### "))
            {
                string text = block.Substring(4);
                cachedGuiCalls.Add(() => RenderHeading(text, headingStyle3, indentationCount, ref renderedHeading));
            }
            else if (block.StartsWith("```"))
            {
                string codeBlock = block;
                cachedGuiCalls.Add(() => RenderCodeBlock(codeBlock));
            }
            else
            {
                string paragraph = block;
                int indent = indentationCount;
                cachedGuiCalls.Add(() => RenderParagraph(paragraph, indent));
            }

            wasPreviouslyRenderedBlockHeader = renderedHeading;
        }

        renderSpaceBeforeNextBlock = true;
    }

    private void RenderHeading(string text, GUIStyle style, int indentationCount, ref bool renderedHeading)
    {
        text = ProcessInlineFormatting(text);
        EditorGUILayout.LabelField(text, style);
        renderedHeading = true;
    }

    private void RenderParagraph(string text, int indentationCount)
    {
        if (string.IsNullOrEmpty(text.Trim()))
        {
            EditorGUILayout.Space(5);
            return;
        }
        text = ProcessInlineFormatting(text);
        // Split the text into segments (link and non-link parts)
        var segments = SplitTextIntoSegments(text);
        if (indentationCount > 0)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.Space(indentationCount);
        }
        EditorGUILayout.BeginVertical();
        foreach (var segment in segments)
        {
            if (segment.isImage)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(segment.target);
                if (texture != null)
                {
                    float maxWidth = EditorGUIUtility.currentViewWidth - 40;
                    float aspectRatio = (float)texture.width / texture.height;
                    float width = Mathf.Min(maxWidth, texture.width);
                    float height = width / aspectRatio;

                    cachedGuiCalls.Add(() => {
                        GUILayout.Label(texture, GUILayout.Width(width), GUILayout.Height(height));
                    });
                }
                continue;
            }

            if (segment.isLink)
            {
                cachedGuiCalls.Add(() => {
                    if (LinkLabel(new GUIContent(segment.displayText)))
                    {
                        HandleLinkClick(segment.linkType, segment.target);
                    }
                });
            }
            else
            {
                string textSegment = segment.text;
                cachedGuiCalls.Add(() => {
                    EditorGUILayout.LabelField(textSegment, bodyStyle);
                });
            }
        }

        EditorGUILayout.EndVertical();

        if (indentationCount > 0)
        {
            EditorGUILayout.EndHorizontal();
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
        var linkPattern = @"<link=""(.+?):(.+?)"">(.*?)</link>";
        var imagePattern = @"<image=""(.+?)""/>";
        var matches = Regex.Matches(text, $"{linkPattern}|{imagePattern}");
        int lastIndex = 0;

        foreach (Match match in matches)
        {
            // Add text before the link
            if (match.Index > lastIndex)
            {
                segments.Add(new TextSegment
                {
                    text = text.Substring(lastIndex, match.Index - lastIndex),
                    isLink = false
                });
            }

            if (match.Groups[1].Success) // Link
            {
                segments.Add(new TextSegment
                {
                    isLink = true,
                    linkType = match.Groups[1].Value,
                    target = match.Groups[2].Value,
                    displayText = match.Groups[3].Value
                });
            }
            else if (match.Groups[4].Success) // Image
            {
                segments.Add(new TextSegment
                {
                    isImage = true,
                    target = match.Groups[4].Value
                });
            }

            lastIndex = match.Index + match.Length;
        }

        // Add remaining text after last link
        if (lastIndex < text.Length)
        {
            segments.Add(new TextSegment
            {
                text = text.Substring(lastIndex),
                isLink = false
            });
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
                        EditorGUILayout.LabelField(lineContent.ToString(), bodyStyle);
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
                    EditorGUILayout.LabelField(strikethrough, bodyStyleScratchedOff);

                } else
                {
                    EditorGUILayout.LabelField(lineContent.ToString(), bodyStyle);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
    }

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

        // Cache GUI calls directly with lambda expressions
        cachedGuiCalls.Add(() => {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.ExpandHeight(true), guiLayoutHeight);
            EditorGUI.DrawRect(GUILayoutUtility.GetRect(0, height), new Color(0.2f, 0.2f, 0.2f));
            GUILayout.Space(2);
            EditorGUILayout.SelectableLabel(lineContent.ToString(), bodyStyle, guiLayoutHeight);
            EditorGUILayout.EndVertical();
            GUILayout.Space(2);
        });
    }

    string DrawToggle(string useLine, bool status, ref bool _drewToggle, int uselineSubstring = 3, int lineIndex = -1)
    {
        bool newToggleValue = EditorGUILayout.Toggle(status, toggleStyle, GUILayout.Width(15));
        useLine = useLine.Substring(uselineSubstring);
        _drewToggle = true;
        if (newToggleValue != status && lineIndex > 0 && lineIndex < fileLines.Count)
        {
            if (newToggleValue)
            {
                fileLines[lineIndex] = fileLines[lineIndex].Replace("- []", "- [x]");
                fileLines[lineIndex] = fileLines[lineIndex].Replace("- [ ]", "- [x]");
            }
            else
            {
                fileLines[lineIndex] = fileLines[lineIndex].Replace("- [x]", "- [ ]");
            }
            modifiedFile = true;
        }
        return useLine;
    }
    private void CacheListBlock(string block, int indentationCount, int lineIndex)
    {
        var items = block.Split('\n')
            .Where(line => line.StartsWith("- ") || line.StartsWith("* "))
            .Select(line => line.Substring(2));

        foreach (var item in items)
        {
            string capturedItem = item;
            int capturedIndent = indentationCount;
            int capturedLineIndex = lineIndex;

            cachedGuiCalls.Add(() => {
                EditorGUILayout.BeginHorizontal();
                if (capturedIndent > 0)
                    EditorGUILayout.Space(capturedIndent * 5.0f, false);

                readmeEditor.showingSpecialBackgroundColor = false;

                bool drewToggle = false;
                bool toggleStatus = false;
                string useLine = capturedItem;
                
                if (useLine.StartsWith("[ ]"))
                    useLine = DrawToggle(useLine, false, ref drewToggle, 3, capturedLineIndex);
                else if (useLine.StartsWith("[]"))
                    useLine = DrawToggle(useLine, false, ref drewToggle, 2, capturedLineIndex);
                else if (useLine.StartsWith("[x]"))
                { 
                    useLine = DrawToggle(useLine, true, ref drewToggle, 3, capturedLineIndex);
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
                            EditorGUILayout.LabelField(lineContent.ToString(), bodyStyle);
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
                        EditorGUILayout.LabelField(strikethrough, bodyStyleScratchedOff);
                    } 
                    else
                    {
                        EditorGUILayout.LabelField(lineContent.ToString(), bodyStyle);
                    }
                }
                EditorGUILayout.EndHorizontal();
            });
        }
    }

}
