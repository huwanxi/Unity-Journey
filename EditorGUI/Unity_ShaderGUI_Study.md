# Unity ShaderGUI 学习指南

`ShaderGUI` 是 Unity 中用于自定义材质（Material）Inspector 面板的强大工具。通过继承 `ShaderGUI` 类，你可以完全控制材质属性的显示方式、添加自定义逻辑、开关宏定义（Keywords）等。

## 1. 基础流程

要为 Shader 创建自定义 GUI，需要两个步骤：
1.  创建一个继承自 `ShaderGUI` 的 C# 类。
2.  在 Shader 文件末尾指定该类名：`CustomEditor "MyCustomShaderGUI"`。

### Shader 代码示例
```glsl
Shader "Custom/MyShader" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader {
        // ... shader pass ...
    }
    // 指定自定义 GUI 类名
    CustomEditor "MyShaderGUI"
}
```

---

## 2. ShaderGUI 类详解

### 2.1 核心方法：OnGUI

所有绘制逻辑都在 `OnGUI` 方法中执行。

```csharp
using UnityEngine;
using UnityEditor;

public class MyShaderGUI : ShaderGUI {
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        // 1. 绘制默认 Inspector (可选)
        // base.OnGUI(materialEditor, properties);

        // 2. 自定义绘制
        // 获取当前编辑的材质
        Material targetMat = materialEditor.target as Material;

        // 查找属性
        MaterialProperty mainTex = FindProperty("_MainTex", properties);
        MaterialProperty color = FindProperty("_Color", properties);

        // 使用 MaterialEditor 绘制属性
        materialEditor.ShaderProperty(mainTex, "Main Texture");
        materialEditor.ColorProperty(color, "Main Color");
        
        // 3. 处理其他逻辑，如 RenderQueue
        // materialEditor.RenderQueueField();
    }
}
```

### 2.2 常用 API

#### FindProperty
在 `properties` 数组中查找指定名称的属性。
```csharp
MaterialProperty prop = FindProperty("_Name", properties);
```

#### materialEditor.ShaderProperty
绘制最通用的属性控件。它会自动根据属性类型（Texture, Color, Float 等）选择合适的 UI。
```csharp
materialEditor.ShaderProperty(prop, new GUIContent("Label"));
```

#### materialEditor.TextureProperty / TexturePropertySingleLine
专门用于绘制纹理的更紧凑的 API。
```csharp
// 绘制纹理槽和额外的属性（如颜色）在同一行
materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), mainTexProp, colorProp);
```

#### materialEditor.RangeProperty
绘制滑动条。
```csharp
materialEditor.RangeProperty(rangeProp, "Slider Label");
```

---

## 3. 材质关键字 (Keywords) 管理

在 ShaderGUI 中，经常需要根据 UI 选项开启或关闭 Shader 变体关键字。

```csharp
// 检查属性值
if (prop.floatValue > 0.5f) {
    targetMat.EnableKeyword("MY_FEATURE_ON");
} else {
    targetMat.DisableKeyword("MY_FEATURE_ON");
}
```

**最佳实践**：通常在 UI 修改后立即更新关键字。
```csharp
EditorGUI.BeginChangeCheck();
materialEditor.ShaderProperty(toggleProp, "Enable Feature");
if (EditorGUI.EndChangeCheck()) {
    // 只有在值改变时才重新设置关键字，避免每帧调用
    SetKeyword(targetMat, "FEATURE_ON", toggleProp.floatValue > 0.5f);
}
```

---

## 4. 布局与样式

由于 `ShaderGUI` 也是在 Editor 环境下运行，你可以混合使用 `EditorGUILayout` 和 `EditorGUI` 的 API。

```csharp
// 添加标题
EditorGUILayout.LabelField("Main Settings", EditorStyles.boldLabel);

// 添加垂直布局
EditorGUILayout.BeginVertical(EditorStyles.helpBox);
materialEditor.TexturePropertySingleLine(new GUIContent("Map"), texProp);
EditorGUILayout.EndVertical();
```

---

## 5. 进阶：处理多选编辑

`MaterialProperty` 自动处理了多选编辑（Multi-editing）。当你选中多个材质时，`FindProperty` 返回的属性包含了所有选中材质的信息。

*   `prop.hasMixedValue`: 如果选中材质的该属性值不一致，此字段为 true。Unity 的绘制函数会自动显示为 "—"（混合状态）。

## 6. 完整示例结构

```csharp
using UnityEditor;
using UnityEngine;

public class CompleteShaderGUI : ShaderGUI {
    MaterialProperty _mainTex;
    MaterialProperty _color;
    MaterialProperty _emission;

    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        // 查找属性
        _mainTex = FindProperty("_MainTex", properties);
        _color = FindProperty("_Color", properties);
        _emission = FindProperty("_Emission", properties);

        Material material = materialEditor.target as Material;

        // 开始绘制
        EditorGUILayout.LabelField("Surface Options", EditorStyles.boldLabel);
        
        // 纹理和颜色一行显示
        materialEditor.TexturePropertySingleLine(new GUIContent("Albedo"), _mainTex, _color);

        // 间距
        EditorGUILayout.Space();

        // 自定义开关控制 Emission
        EditorGUI.BeginChangeCheck();
        bool enableEmission = Array.IndexOf(material.shaderKeywords, "_EMISSION") != -1;
        enableEmission = EditorGUILayout.Toggle("Enable Emission", enableEmission);
        if (EditorGUI.EndChangeCheck()) {
            if (enableEmission) material.EnableKeyword("_EMISSION");
            else material.DisableKeyword("_EMISSION");
        }

        if (enableEmission) {
            materialEditor.ShaderProperty(_emission, "Emission Color");
        }

        // 底部绘制 Render Queue 和 Instancing
        EditorGUILayout.Space();
        materialEditor.RenderQueueField();
        materialEditor.EnableInstancingField();
    }
}
```
