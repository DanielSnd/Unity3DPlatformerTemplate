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

        headingStyle2 = new GUIStyle(bodyStyle);
        headingStyle2.fontSize = 20;
        headingStyle2.fontStyle = FontStyle.Bold;

        headingStyle3 = new GUIStyle(bodyStyle);
        headingStyle3.fontSize = 16;
        headingStyle3.fontStyle = FontStyle.Bold;

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

        modifiedFile = false;
        fileLines = SplitIntoBlocks(markdown);
        for (int i = 0; i < fileLines.Count; i++) {
            RenderBlock(fileLines[i], i);
        }
        if (modifiedFile && readmeEditor) {
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
    private void RenderBlock(string block, int lineIndex)
    {
        if (string.IsNullOrWhiteSpace(block)) {
            wasPreviouslyRenderedBlockList = false;
            wasPreviouslyRenderedBlockHeader = false;
            EditorGUILayout.Space(currentSpaceBetweenBlocks);
            currentSpaceBetweenBlocks = 5;
            return;
        }
        string originalBlock = block;
        int indentationCount = 0;
        int extraIndentation = 0;
        block = block.Trim();

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
                GUILayout.Space(drawBefore);
            
            RenderList(block, indentationCount, lineIndex);

            currentSpaceBetweenBlocks = 0;
            wasPreviouslyRenderedBlockList = true;
            wasPreviouslyRenderedBlockHeader = false;
        }else
        {
            if (drawBefore > 0 && renderSpaceBeforeNextBlock)
                GUILayout.Space(drawBefore);

            if (wasPreviouslyRenderedBlockList)
                GUILayout.Space(currentSpaceBetweenBlocks + 5); // Add space between blocks
            
            wasPreviouslyRenderedBlockList = false;
            bool renderedHeading = false;
            if (block.StartsWith("# "))
            {
                RenderHeading(block.Substring(2), headingStyle1, indentationCount, ref renderedHeading);
            }
            else if (block.StartsWith("## "))
            {
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
            EditorGUILayout.LabelField(lineContent.ToString(), bodyStyle);
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
        public string linkType;
        public string target;
        public string displayText;
    }

    private List<TextSegment> SplitTextIntoSegments(string text)
    {
        var segments = new List<TextSegment>();
        var linkPattern = @"<link=""(.+?):(.+?)"">(.*?)</link>";
        var matches = Regex.Matches(text, linkPattern);
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

            // Add the link
            segments.Add(new TextSegment
            {
                isLink = true,
                linkType = match.Groups[1].Value,
                target = match.Groups[2].Value,
                displayText = match.Groups[3].Value
            });

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

    private void RenderCodeBlock(string block)
    {
        var lines = block.Split('\n');
        var code = string.Join("\n",
            lines.Skip(1).Take(lines.Length - 2)); // Remove ``` lines

        EditorGUILayout.TextArea(code, codeStyle);
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

        // Unity asset links with special syntax: [[WikiPage:path/to/asset]] or [[File:path/to/file]]
        text = Regex.Replace(text, @"\[\[WikiPage:(.+?)\]\]", match => {
            var path = "Assets/" + match.Groups[1].Value;
            if (!path.Contains("."))
                path = path + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<WikiPage>(path.TrimStart().TrimEnd());
            if (asset != null)
            {
                return $"<link=\"asset:{path}\">{asset.title}</link>";
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

        // Regular markdown links
        text = Regex.Replace(text, @"\[(.+?)\]\((.+?)\)", "<link=\"url:$2\">$1</link>");

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

}