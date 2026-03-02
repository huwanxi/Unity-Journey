# **LOD系统（Level of Detail）详细讲解**

## 一、LOD核心概念

### 1. **什么是LOD？**
**Level of Detail（细节层次）** 是一种优化技术，根据对象与摄像机的距离，使用不同精度的模型来渲染，以**平衡画质与性能**。

### 2. **基本思想**
```text
近距离 → 高细节模型（高多边形）
中距离 → 中细节模型  
远距离 → 低细节模型（低多边形）
极远距离 → 可能不渲染或Billboard
```

### 3. **核心价值**
- **性能提升**：减少远处物体的渲染压力
- **内存优化**：按需加载不同精度的资源
- **视觉优化**：用户几乎察觉不到差异

---

## 二、Unity中的LOD系统

### 1. **Unity内置LOD组件**
```csharp
// Unity的LOD Group组件
GameObject obj = new GameObject("LOD_Object");
LODGroup lodGroup = obj.AddComponent<LODGroup>();

// 设置LOD级别
LOD[] lods = new LOD[3];
lods[0] = new LOD(0.6f, new Renderer[]{ highDetail.GetComponent<Renderer>() });  // 0-60%
lods[1] = new LOD(0.3f, new Renderer[]{ mediumDetail.GetComponent<Renderer>() }); // 60-90%
lods[2] = new LOD(0.05f, new Renderer[]{ lowDetail.GetComponent<Renderer>() });  // 90-95%

lodGroup.SetLODs(lods);
lodGroup.RecalculateBounds();
```

### 2. **LOD配置示例**
```text
LOD 0: 0-30米 (10000个三角形)    // 高细节
LOD 1: 30-100米 (3000个三角形)  // 中细节  
LOD 2: 100-500米 (500个三角形)  // 低细节
LOD 3: >500米 (0个三角形)       // Culled（不渲染）
```

---

## 三、完整LOD系统实现

### **场景示例：开放世界树木LOD系统**

#### 1. **模型准备阶段**
```csharp
// LOD模型规范
public enum LODLevel
{
    LOD0 = 0,  // 超精细：2000+ tris
    LOD1 = 1,  // 精细：800 tris  
    LOD2 = 2,  // 中等：300 tris
    LOD3 = 3,  // 粗糙：100 tris
    LOD4 = 4,  // Billboard：2 tris
    Culled = 5 // 剔除
}
```

#### 2. **数据定义**
```csharp
[System.Serializable]
public class LODData
{
    public GameObject modelPrefab;    // 对应LOD级别的模型
    public float screenRelativeHeight; // 屏幕高度占比阈值
    public float maxDistance;         // 最大距离
    public int triangleCount;         // 三角形数量
    public bool castShadows;          // 是否投射阴影
}

[CreateAssetMenu(fileName = "LODConfig", menuName = "LOD/LOD Configuration")]
public class LODConfiguration : ScriptableObject
{
    public LODData[] lodLevels = new LODData[4];
    
    // 根据距离获取LOD级别
    public int GetLODLevel(float distance, float cameraHeight)
    {
        for (int i = 0; i < lodLevels.Length; i++)
        {
            if (distance <= lodLevels[i].maxDistance)
                return i;
        }
        return lodLevels.Length - 1;
    }
}
```

#### 3. **核心LOD管理器**
```csharp
public class AdvancedLODManager : MonoBehaviour
{
    [Header("LOD设置")]
    public LODConfiguration lodConfig;
    public float updateInterval = 0.5f;  // 更新间隔
    public bool useAsyncUpdate = true;   // 异步更新
    
    [Header("性能优化")]
    public int batchSize = 50;          // 每帧处理的物体数量
    public bool enableFrustumCulling = true;
    public bool enableOcclusionCulling = true;
    
    private List<LODInstance> lodInstances = new List<LODInstance>();
    private Transform mainCamera;
    private float updateTimer;
    private int currentIndex;
    
    void Start()
    {
        mainCamera = Camera.main.transform;
        InitializeLODInstances();
    }
    
    void Update()
    {
        updateTimer += Time.deltaTime;
        
        if (updateTimer >= updateInterval)
        {
            if (useAsyncUpdate)
                StartCoroutine(UpdateLODsAsync());
            else
                UpdateLODsBatch();
            
            updateTimer = 0;
        }
    }
    
    // 异步分批更新LOD
    IEnumerator UpdateLODsAsync()
    {
        for (int i = 0; i < lodInstances.Count; i += batchSize)
        {
            int end = Mathf.Min(i + batchSize, lodInstances.Count);
            
            for (int j = i; j < end; j++)
            {
                UpdateLODInstance(j);
            }
            
            yield return null; // 每批处理完等待一帧
        }
    }
    
    // 批量更新LOD
    void UpdateLODsBatch()
    {
        for (int i = 0; i < Mathf.Min(batchSize, lodInstances.Count); i++)
        {
            currentIndex = (currentIndex + 1) % lodInstances.Count;
            UpdateLODInstance(currentIndex);
        }
    }
    
    void UpdateLODInstance(int index)
    {
        LODInstance instance = lodInstances[index];
        
        // 视锥体裁剪
        if (enableFrustumCulling && !IsInFrustum(instance))
        {
            instance.SetVisible(false);
            return;
        }
        
        // 计算距离和LOD级别
        float distance = Vector3.Distance(instance.transform.position, mainCamera.position);
        int lodLevel = lodConfig.GetLODLevel(distance, mainCamera.position.y);
        
        // 更新LOD
        instance.UpdateLOD(lodLevel);
    }
    
    bool IsInFrustum(LODInstance instance)
    {
        // 简化的视锥体检测
        Vector3 viewportPos = Camera.main.WorldToViewportPoint(instance.transform.position);
        return viewportPos.x >= -0.2f && viewportPos.x <= 1.2f &&
               viewportPos.y >= -0.2f && viewportPos.y <= 1.2f &&
               viewportPos.z > 0;
    }
}
```

#### 4. **LOD实例类**
```csharp
public class LODInstance : MonoBehaviour
{
    [System.Serializable]
    public struct LODRenderer
    {
        public Renderer renderer;
        public MeshCollider collider; // 可选的碰撞体
        public int triangleCount;
    }
    
    public LODRenderer[] lodRenderers;  // 不同LOD级别的渲染器
    public BillboardRenderer billboardRenderer; // Billboard渲染器
    
    private int currentLOD = -1;
    private bool isVisible = true;
    
    public void UpdateLOD(int newLOD)
    {
        if (currentLOD == newLOD || newLOD >= lodRenderers.Length)
            return;
        
        // 隐藏所有LOD级别
        for (int i = 0; i < lodRenderers.Length; i++)
        {
            if (lodRenderers[i].renderer != null)
                lodRenderers[i].renderer.enabled = false;
        }
        
        // 显示当前LOD级别
        if (newLOD >= 0 && newLOD < lodRenderers.Length && 
            lodRenderers[newLOD].renderer != null)
        {
            lodRenderers[newLOD].renderer.enabled = true;
        }
        
        // Billboard处理
        if (billboardRenderer != null)
        {
            billboardRenderer.enabled = (newLOD == lodRenderers.Length);
        }
        
        currentLOD = newLOD;
    }
    
    public void SetVisible(bool visible)
    {
        if (isVisible == visible) return;
        
        isVisible = visible;
        foreach (var lod in lodRenderers)
        {
            if (lod.renderer != null)
                lod.renderer.enabled = visible;
        }
    }
}
```

#### 5. **Billboard系统（极远距离）**
```csharp
public class BillboardRenderer : MonoBehaviour
{
    public Texture2D billboardTexture;
    public Material billboardMaterial;
    public Vector2 size = new Vector2(5, 10);
    
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Transform cameraTransform;
    
    void Start()
    {
        cameraTransform = Camera.main.transform;
        CreateBillboardMesh();
    }
    
    void Update()
    {
        if (cameraTransform != null)
        {
            // 始终面向相机
            transform.LookAt(cameraTransform);
        }
    }
    
    void CreateBillboardMesh()
    {
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-size.x/2, 0, 0),
            new Vector3(size.x/2, 0, 0),
            new Vector3(-size.x/2, size.y, 0),
            new Vector3(size.x/2, size.y, 0)
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        int[] triangles = new int[6] { 0, 2, 1, 2, 3, 1 };
        
        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        
        meshFilter = gameObject.AddComponent<MeshFilter>();
        meshRenderer = gameObject.AddComponent<MeshRenderer>();
        
        meshFilter.mesh = mesh;
        meshRenderer.material = billboardMaterial;
        meshRenderer.material.mainTexture = billboardTexture;
    }
}
```

---

## 四、LOD系统优化技巧

### 1. **动态LOD切换策略**
```csharp
public class DynamicLODSwitcher : MonoBehaviour
{
    [Header("动态调整参数")]
    public bool adaptiveToPerformance = true;
    public float targetFrameRate = 60f;
    public float performanceCheckInterval = 2f;
    
    private float[] lodDistances = new float[4] { 30f, 100f, 300f, 500f };
    private float performanceTimer;
    
    void Update()
    {
        performanceTimer += Time.deltaTime;
        
        if (performanceTimer >= performanceCheckInterval && adaptiveToPerformance)
        {
            AdjustLODBasedOnPerformance();
            performanceTimer = 0;
        }
    }
    
    void AdjustLODBasedOnPerformance()
    {
        float currentFPS = 1f / Time.deltaTime;
        
        if (currentFPS < targetFrameRate * 0.8f) // 帧率过低
        {
            // 增加LOD切换距离（更早切换为低模）
            for (int i = 0; i < lodDistances.Length; i++)
            {
                lodDistances[i] *= 0.8f; // 减少20%距离
            }
        }
        else if (currentFPS > targetFrameRate * 1.2f) // 帧率过高
        {
            // 减少LOD切换距离（更晚切换为低模）
            for (int i = 0; i < lodDistances.Length; i++)
            {
                lodDistances[i] *= 1.2f; // 增加20%距离
            }
        }
    }
}
```

### 2. **LOD过渡效果**
```csharp
public class LODTransition : MonoBehaviour
{
    public float transitionDuration = 0.5f;
    private Material currentMaterial;
    private Material nextMaterial;
    private float transitionProgress;
    private bool isTransitioning;
    
    public void CrossFadeLOD(Renderer currentRenderer, Renderer nextRenderer)
    {
        StartCoroutine(DoTransition(currentRenderer, nextRenderer));
    }
    
    IEnumerator DoTransition(Renderer current, Renderer next)
    {
        isTransitioning = true;
        transitionProgress = 0f;
        
        currentMaterial = current.material;
        nextMaterial = next.material;
        
        // 淡出当前LOD，淡入新LOD
        while (transitionProgress < 1f)
        {
            transitionProgress += Time.deltaTime / transitionDuration;
            
            float alpha = Mathf.SmoothStep(1f, 0f, transitionProgress);
            Color color = currentMaterial.color;
            color.a = alpha;
            currentMaterial.color = color;
            
            nextMaterial.color = new Color(
                nextMaterial.color.r,
                nextMaterial.color.g,
                nextMaterial.color.b,
                1f - alpha
            );
            
            yield return null;
        }
        
        current.enabled = false;
        isTransitioning = false;
    }
}
```

---

## 五、不同场景的LOD配置示例

### 1. **开放世界地形**
```yaml
LOD配置:
  LOD0: 0-50米, 50000三角形, 4K纹理
  LOD1: 50-200米, 15000三角形, 2K纹理
  LOD2: 200-800米, 5000三角形, 1K纹理  
  LOD3: 800-2000米, 1000三角形, 512纹理
  Billboard: >2000米
```

### 2. **城市建筑**
```yaml
LOD配置:
  LOD0: 0-100米, 全细节, 窗户/阳台可见
  LOD1: 100-300米, 简化几何体, 纹理烘焙细节
  LOD2: 300-800米, 简单长方体, 低分辨率纹理
  LOD3: >800米, 不渲染或简并表示
```

### 3. **角色模型**
```yaml
LOD配置:
  LOD0: 0-20米, 30000三角形, 4K皮肤, 动态骨骼
  LOD1: 20-50米, 10000三角形, 2K皮肤, 简化骨骼
  LOD2: 50-150米, 3000三角形, 1K皮肤, 静态姿势
  LOD3: >150米, 500三角形, 简单形状
```

---

## 六、性能优化建议

### 1. **LOD级别设计原则**
```text
三角形数量递减比例: LOD0:LOD1:LOD2:LOD3 ≈ 1:0.3:0.1:0.03
纹理大小递减比例: 1:0.5:0.25:0.125
```

### 2. **批量处理优化**
```csharp
// 使用GPU Instancing加速相同LOD级别的渲染
MaterialPropertyBlock props = new MaterialPropertyBlock();
props.SetColor("_Color", Color.red);
renderer.SetPropertyBlock(props);
```

### 3. **异步资源加载**
```csharp
IEnumerator LoadLODModelsAsync(LODInstance instance, int targetLOD)
{
    string path = $"Models/{instance.modelId}/LOD{targetLOD}";
    
    ResourceRequest request = Resources.LoadAsync<GameObject>(path);
    yield return request;
    
    if (request.asset != null)
    {
        GameObject model = Instantiate(request.asset as GameObject);
        instance.SetLODModel(targetLOD, model);
    }
}
```

---

## 七、调试与监控

```csharp
public class LODDebugger : MonoBehaviour
{
    [Header("调试显示")]
    public bool showLODDistances = true;
    public bool showTriangleCount = true;
    public Color[] lodColors = new Color[4];
    
    void OnGUI()
    {
        if (!showLODDistances) return;
        
        GUILayout.BeginArea(new Rect(10, 10, 300, 200));
        GUILayout.Label("=== LOD系统状态 ===");
        
        LODManager manager = FindObjectOfType<LODManager>();
        if (manager != null)
        {
            foreach (var instance in manager.GetAllInstances())
            {
                string status = $"LOD {instance.currentLOD} | " +
                               $"{instance.triangleCount} tris | " +
                               $"{instance.distance:F1}m";
                
                GUILayout.Label(status);
            }
        }
        
        GUILayout.EndArea();
    }
    
    void OnDrawGizmosSelected()
    {
        // 绘制LOD距离球体
        for (int i = 0; i < lodDistances.Length; i++)
        {
            Gizmos.color = lodColors[i % lodColors.Length];
            Gizmos.DrawWireSphere(transform.position, lodDistances[i]);
        }
    }
}
```

---

## 八、最佳实践总结

### ✅ **应该做的：**
1. **渐进式细节**：确保各LOD级别视觉一致性
2. **合理阈值**：根据物体大小和重要性设置距离
3. **异步切换**：避免LOD切换造成的卡顿
4. **内存管理**：卸载不用的LOD级别资源
5. **测试验证**：在不同距离、角度测试LOD切换

### ❌ **不应该做的：**
1. **过度细分**：3-4个LOD级别通常足够
2. **硬切换**：添加过渡效果避免"弹跳"
3. **忽略碰撞体**：为不同LOD级别调整碰撞精度
4. **静态配置**：考虑动态调整适应不同硬件
5. **忘记Billboard**：极远距离使用Billboard节省性能

### ⚡ **进阶技巧：**
1. **视差贴图**：在低模上模拟深度细节
2. **LOD融合**：使用Shader混合相邻LOD级别
3. **预测性LOD**：根据相机移动方向预加载
4. **HLSOD**：基于高度层次的LOD（用于地形）
5. **GPU Driven LOD**：使用Compute Shader管理LOD

---

## 九、实际案例：MMORPG大世界

```csharp
// 大型MMORPG中的LOD系统架构
public class MMO_LODSystem : MonoBehaviour
{
    // 分层LOD管理
    Dictionary<LODZone, List<LODInstance>> zoneInstances;
    
    // 动态优先级系统
    PriorityQueue<LODInstance> updateQueue;
    
    // 网络同步LOD
    void SyncLODOverNetwork(LODInstance instance, int newLOD)
    {
        // 只同步给附近玩家
        if (IsPlayerNearby(instance))
        {
            RpcUpdateLOD(instance.id, newLOD);
        }
    }
    
    // 流式加载配合LOD
    IEnumerator StreamLODModels(LODZone zone)
    {
        while (true)
        {
            // 预加载玩家可能前往区域的LOD资源
            Vector3 predictedPosition = PredictPlayerMovement();
            LODZone targetZone = GetZoneAtPosition(predictedPosition);
            
            if (targetZone != currentZone)
            {
                yield return LoadZoneLODsAsync(targetZone, LODLevel.LOD2);
            }
            
            yield return new WaitForSeconds(1f);
        }
    }
}
```

LOD系统是大型3D应用**性能优化的核心**，合理设计可以提升**2-5倍的渲染性能**，同时保持视觉质量。关键在于**平衡细节与性能**，提供**平滑的视觉过渡**。