# Unity EditorGUI 学习指南

本文档详细介绍了 Unity 中 `EditorGUI` 和 `EditorGUILayout` 的核心概念、常用 API 及其使用方法。

## 1. 基础概念

在 Unity 编辑器扩展中，主要有两个用于绘制 GUI 的类：
*   **EditorGUI**: 用于手动控制位置（Rect）的 GUI 绘制。通常用于 `PropertyDrawer` 或需要精确控制布局的场景。
*   **EditorGUILayout**: 自动布局版本的 GUI 绘制。Unity 会自动计算控件的位置和大小。通常用于 `Editor` 窗口（Editor Window）或组件检查器（Inspector）的简单扩展。

---

## 2. 常用 API 详解

### 2.1 基础控件

#### LabelField
显示只读文本标签。
```csharp
// EditorGUI
EditorGUI.LabelField(rect, "Label Text", "Optional Tooltip");
// EditorGUILayout
EditorGUILayout.LabelField("Label Text", "Optional Tooltip");
```

#### TextField
显示文本输入框。
```csharp
// EditorGUI
string text = EditorGUI.TextField(rect, "Label", currentText);
// EditorGUILayout
string text = EditorGUILayout.TextField("Label", currentText);
```

#### IntField / FloatField
显示整数或浮点数输入框。
```csharp
// EditorGUI
int val = EditorGUI.IntField(rect, "Label", currentVal);
// EditorGUILayout
float val = EditorGUILayout.FloatField("Label", currentVal);
```

#### Toggle
显示复选框。
```csharp
// EditorGUI
bool val = EditorGUI.Toggle(rect, "Label", currentVal);
// EditorGUILayout
bool val = EditorGUILayout.Toggle("Label", currentVal);
```

### 2.2 复杂控件

#### PropertyField
**最常用**。自动根据 `SerializedProperty` 的类型绘制合适的控件。支持撤销/重做（Undo/Redo）和预制体覆盖（Prefab overrides）。
```csharp
// EditorGUI
EditorGUI.PropertyField(rect, serializedProperty, new GUIContent("Label"));
// EditorGUILayout
EditorGUILayout.PropertyField(serializedProperty, new GUIContent("Label"));
```

#### Slider / IntSlider
显示滑动条。
```csharp
// EditorGUI
float val = EditorGUI.Slider(rect, "Label", currentVal, min, max);
// EditorGUILayout
int val = EditorGUILayout.IntSlider("Label", currentVal, min, max);
```

#### EnumPopup
显示枚举选择下拉框。
```csharp
// EditorGUI
Enum val = EditorGUI.EnumPopup(rect, "Label", currentEnum);
// EditorGUILayout
Enum val = EditorGUILayout.EnumPopup("Label", currentEnum);
```

#### ColorField
显示颜色选择器。
```csharp
// EditorGUI
Color col = EditorGUI.ColorField(rect, "Label", currentColor);
// EditorGUILayout
Color col = EditorGUILayout.ColorField("Label", currentColor);
```

#### ObjectField
显示对象引用槽。
```csharp
// EditorGUI
Object obj = EditorGUI.ObjectField(rect, "Label", currentObj, typeof(GameObject), allowSceneObjects);
// EditorGUILayout
Object obj = EditorGUILayout.ObjectField("Label", currentObj, typeof(GameObject), allowSceneObjects);
```

### 2.3 布局控制 (EditorGUILayout 专用)

#### BeginHorizontal / EndHorizontal
开始/结束水平布局组。
```csharp
EditorGUILayout.BeginHorizontal();
// 这里的控件将水平排列
EditorGUILayout.LabelField("Left");
EditorGUILayout.LabelField("Right");
EditorGUILayout.EndHorizontal();
```

#### BeginVertical / EndVertical
开始/结束垂直布局组。
```csharp
EditorGUILayout.BeginVertical();
// 这里的控件将垂直排列
EditorGUILayout.EndVertical();
```

#### Space
添加空白间距。
```csharp
EditorGUILayout.Space(10); // 10像素间距
```

#### HelpBox
显示提示框（信息、警告、错误）。
```csharp
EditorGUILayout.HelpBox("This is a warning message.", MessageType.Warning);
```

---

## 3. EditorGUI 实用工具

### 3.1 IndentLevel
控制缩进级别，常用于层次化显示。
```csharp
EditorGUI.indentLevel++;
// 绘制缩进后的内容
EditorGUI.indentLevel--;
```

### 3.2 BeginChangeCheck / EndChangeCheck
检测代码块内的 GUI 值是否发生了变化。
```csharp
EditorGUI.BeginChangeCheck();
float newVal = EditorGUILayout.FloatField("Value", val);
if (EditorGUI.EndChangeCheck()) {
    // 值发生了改变，执行相应逻辑，例如记录 Undo
    Undo.RecordObject(targetObject, "Change Value");
    val = newVal;
}
```

### 3.3 BeginDisabledGroup / EndDisabledGroup
禁用一组控件（变灰且不可交互）。
```csharp
EditorGUI.BeginDisabledGroup(isDisabled);
EditorGUILayout.TextField("Disabled Field", text);
EditorGUI.EndDisabledGroup();
```

---

## 4. 进阶技巧：Rect 计算

在使用 `EditorGUI` 时，经常需要手动计算 `Rect`。Unity 提供了 `EditorGUILayout.GetControlRect()` 来获取自动布局计算出的 `Rect`，然后用于 `EditorGUI` 绘制。

```csharp
Rect rect = EditorGUILayout.GetControlRect(false, fieldHeight);
EditorGUI.PropertyField(rect, property);
```

## 5. 示例代码：自定义 Inspector

```csharp
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MyComponent))]
public class MyComponentEditor : Editor {
    SerializedProperty myFloatProp;

    void OnEnable() {
        myFloatProp = serializedObject.FindProperty("myFloat");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.LabelField("My Custom Inspector", EditorStyles.boldLabel);
        
        EditorGUILayout.Space();

        EditorGUI.BeginChangeCheck();
        EditorGUILayout.PropertyField(myFloatProp);
        if (EditorGUI.EndChangeCheck()) {
            Debug.Log("Value Changed!");
        }

        if (GUILayout.Button("Click Me")) {
            Debug.Log("Button Clicked");
        }

        serializedObject.ApplyModifiedProperties();
    }
}
```
