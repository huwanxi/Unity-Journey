场景切换的转换行为：

场景类型	是否触发 Convert	推荐方案
SubScene	🟢 自动触发	最佳实践
传统 Scene	❌ 不触发	不推荐用于 ECS
运行时加载	❌ 不触发	使用 EntityPrefab




EntityPrefab：

| **2. 实例化调用** | `EntityManager.Instantiate(prefab)` | ```
[Archetype A]
LocalTransform: [Prefab_T]
MovementData:   [Prefab_M]
HealthData:     [Prefab_H]
``` | 系统找到对应的 Archetype 内存块 |
| **3. 内存分配** | 在 Archetype 内存块末尾分配空间 | ```
[Archetype A]
LocalTransform: [Prefab_T] [New_T?]
MovementData:   [Prefab_M] [New_M?]
HealthData:     [Prefab_H] [New_H?]
``` | 为所有组件类型同时分配空间 |
| **4. 数据复制** | 从模板复制数据到新位置 | ```
[Archetype A]
LocalTransform: [Prefab_T] [New_T←Prefab_T]
MovementData:   [Prefab_M] [New_M←Prefab_M]
HealthData:     [Prefab_H] [New_H←Prefab_H]
``` | 批量复制所有组件数据 |
| **5. 自定义设置** | `SetComponent` 修改特定数据 | ```
[Archetype A]
LocalTransform: [Prefab_T] [New_T(修改后)]
MovementData:   [Prefab_M] [New_M(修改后)]
HealthData:     [Prefab_H] [New_H(保持不变)]
``` | 只修改需要自定义的字段 |
| **6. 最终状态** | 实例化完成 | ```
[Archetype A]
LocalTransform: [Prefab_T] [Instance1_T] [Instance2_T]...
MovementData:   [Prefab_M] [Instance1_M] [Instance2_M]...
HealthData:     [Prefab_H] [Instance1_H] [Instance2_H]...
``` | 所有实例数据连续存储，缓存友好 |

## 具体示例对比

### 实例化 3 个敌人的内存变化：

| 实例化过程 | LocalTransform 数组 | MovementData 数组 | HealthData 数组 |
|------------|---------------------|-------------------|-----------------|
| **初始** | `[Prefab_T]` | `[Prefab_M]` | `[Prefab_H]` |
| **实例1** | `[Prefab_T, Enemy1_T]` | `[Prefab_M, Enemy1_M]` | `[Prefab_H, Enemy1_H]` |
| **实例2** | `[Prefab_T, Enemy1_T, Enemy2_T]` | `[Prefab_M, Enemy1_M, Enemy2_M]` | `[Prefab_H, Enemy1_H, Enemy2_H]` |
| **实例3** | `[Prefab_T, Enemy1_T, Enemy2_T, Enemy3_T]` | `[Prefab_M, Enemy1_M, Enemy2_M, Enemy3_M]` | `[Prefab_H, Enemy1_H, Enemy2_H, Enemy3_H]` |

## 关键特点

| 特点 | 传统 GameObject | EntityPrefab |
|------|-----------------|--------------|
| **内存布局** | 按实体分散存储 | 按组件连续存储 |
| **实例化开销** | 高（创建+初始化） | 低（内存复制） |
| **缓存效率** | 差 | 优秀 |
| **批量处理** | 困难 | 天然支持 |

**简单总结：** EntityPrefab 实例化就是在对应 Archetype 的内存块末尾"追加"一份数据副本，然后修改需要自定义的字段。