# Unity ECS 全面教学

## 1. ECS 核心概念

### 三大核心要素：
- **Entity**：轻量级ID，无数据无逻辑
- **Component**：纯数据
- **System**：纯逻辑

## 2. 组件类型

### 2.1 数据组件 (IComponentData)
```csharp
// 基础数据组件
public struct MovementData : IComponentData
{
    public float Speed;
    public float3 Direction;
}

// 标签组件 (无数据)
public struct PlayerTag : IComponentData { }

// 启用组件
public struct EnabledComponent : IComponentData, IEnableableComponent
{
    public bool IsActive;
}
```

### 2.2 共享组件 (ISharedComponentData)
```csharp
public struct RenderMesh : ISharedComponentData
{
    public Mesh mesh;
    public Material material;
    public int subMesh;
}
```

### 2.3 缓冲区组件 (IBufferElementData)
```csharp
public struct PathPoint : IBufferElementData
{
    public float3 Position;
    public float WaitTime;
}
```

## 3. 转换系统

### 3.1 自动转换 ([GenerateAuthoringComponent])
```csharp
[GenerateAuthoringComponent]
public struct HealthData : IComponentData
{
    public int CurrentHealth;
    public int MaxHealth;
}

// 使用：直接在 GameObject 添加 "Health Data" 组件
```

### 3.2 手动转换 (IConvertGameObjectToEntity)
```csharp
public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Speed = 5f;
    public int Health = 100;
    public GameObject ProjectilePrefab;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // 添加基础组件
        dstManager.AddComponentData(entity, new MovementData { Speed = Speed });
        dstManager.AddComponentData(entity, new HealthData { 
            CurrentHealth = Health, 
            MaxHealth = Health 
        });
        
        // 添加标签
        dstManager.AddComponent<PlayerTag>(entity);
        
        // 转换 GameObject 引用为 Entity 引用
        if (ProjectilePrefab != null)
        {
            Entity projectileEntity = conversionSystem.GetPrimaryEntity(ProjectilePrefab);
            dstManager.AddComponentData(entity, new WeaponData 
            { 
                ProjectilePrefab = projectileEntity 
            });
        }
        
        // 添加缓冲区
        var buffer = dstManager.AddBuffer<InventoryItem>(entity);
        buffer.Add(new InventoryItem { ItemId = 1, Count = 5 });
    }
}
```

### 3.3 EntityManager 核心 API
```csharp
// 创建和销毁
Entity entity = EntityManager.CreateEntity();
EntityManager.DestroyEntity(entity);

// 添加组件
EntityManager.AddComponent<T>(entity);                    // 标签组件
EntityManager.AddComponentData<T>(entity, data);         // 数据组件
EntityManager.AddSharedComponent<T>(entity, sharedData); // 共享组件

// 移除组件
EntityManager.RemoveComponent<T>(entity);

// 检查组件
bool hasComponent = EntityManager.HasComponent<T>(entity);
var componentData = EntityManager.GetComponentData<T>(entity);

// 设置组件数据
EntityManager.SetComponentData<T>(entity, data);

// 动态缓冲区
DynamicBuffer<T> buffer = EntityManager.AddBuffer<T>(entity);
DynamicBuffer<T> buffer = EntityManager.GetBuffer<T>(entity);
```

## 4. 系统开发

### 4.1 系统基类 (SystemBase)
```csharp
public partial class MovementSystem : SystemBase
{
    private EntityQuery movementQuery;
    private BeginInitializationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        // 创建 EntityQuery
        movementQuery = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MovementData, LocalTransform>()
            .WithNone<FrozenTag>()
            .Build(this);
            
        // 获取 ECB 系统
        ecbSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 方式1：Entities.ForEach
        Entities
            .WithAll<MovementData>()
            .WithName("MovementJob")
            .ForEach((ref LocalTransform transform, in MovementData movement) =>
            {
                transform.Position += movement.Direction * movement.Speed * deltaTime;
            })
            .ScheduleParallel();
            
        // 方式2：IJobEntity
        new MoveJob { DeltaTime = deltaTime }.ScheduleParallel();
        
        // 方式3：使用 EntityCommandBuffer
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        Entities
            .WithAll<PlayerTag>()
            .ForEach((Entity entity, int entityInQueryIndex, in HealthData health) =>
            {
                if (health.CurrentHealth <= 0)
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            })
            .ScheduleParallel();
    }
}
```

### 4.2 IJobEntity (推荐)
```csharp
[BurstCompile]
public partial struct MoveJob : IJobEntity
{
    public float DeltaTime;
    
    [BurstCompile]
    public void Execute(ref LocalTransform transform, in MovementData movement)
    {
        transform.Position += movement.Direction * movement.Speed * DeltaTime;
    }
}

// 自定义查询条件
[BurstCompile]
[WithAll(typeof(PlayerTag), typeof(MovementData))]
[WithNone(typeof(DeadTag))]
public partial struct PlayerMoveJob : IJobEntity
{
    public float DeltaTime;
    
    public void Execute(ref LocalTransform transform, in MovementData movement)
    {
        // 只处理玩家实体的移动
    }
}
```

### 4.3 IJobChunk (高级)
```csharp
[BurstCompile]
public struct ProcessChunkJob : IJobChunk
{
    public float DeltaTime;
    public ComponentTypeHandle<LocalTransform> TransformHandle;
    public ComponentTypeHandle<MovementData> MovementHandle;
    public EntityTypeHandle EntityHandle;
    
    public void Execute(in ArchetypeChunk chunk, int chunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
    {
        var transforms = chunk.GetNativeArray(ref TransformHandle);
        var movements = chunk.GetNativeArray(ref MovementHandle);
        var entities = chunk.GetNativeArray(EntityHandle);
        
        for (int i = 0; i < chunk.Count; i++)
        {
            var transform = transforms[i];
            var movement = movements[i];
            
            transform.Position += movement.Direction * movement.Speed * DeltaTime;
            transforms[i] = transform;
        }
    }
}
```

## 5. Burst 编译

### 5.1 BurstCompile 特性
```csharp
[BurstCompile]
public partial struct OptimizedJob : IJobEntity
{
    public float DeltaTime;
    
    [BurstCompile(FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    public void Execute(ref LocalTransform transform, in MovementData movement)
    {
        // 使用快速数学运算
        transform.Position += movement.Direction * movement.Speed * DeltaTime;
    }
}

// 禁用 Burst（用于调试）
[BurstCompile(DisableSafetyChecks = true, OptimizeFor = OptimizeFor.Performance)]
public partial struct UnsafeJob : IJobEntity
{
    public void Execute(ref LocalTransform transform) { }
}
```

### 5.2 Mathematics 库
```csharp
using Unity.Mathematics;

public struct MathData : IComponentData
{
    public float3 Position;
    public quaternion Rotation;
    public float4x4 Matrix;
    
    // 常用数学操作
    public static float3 MoveTowards(float3 current, float3 target, float maxDistance)
    {
        float3 direction = target - current;
        float distance = math.length(direction);
        
        if (distance <= maxDistance || distance == 0f)
            return target;
            
        return current + direction / distance * maxDistance;
    }
}
```

## 6. 系统执行顺序

### 6.1 系统组
```csharp
// 初始化系统组
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial class SetupSystem : SystemBase
{
    // 最先执行
}

// 模拟系统组（主要游戏逻辑）
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
public partial class MovementSystem : SystemBase
{
    // 主要的游戏逻辑系统
}

// 演示系统组
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class RenderSystem : SystemBase
{
    // 渲染前执行
}

// 自定义系统组
public partial class MyCustomSystemGroup : ComponentSystemGroup { }
```

### 6.2 执行顺序控制
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateBefore(typeof(MovementSystem))]  // 在 MovementSystem 之前执行
[UpdateAfter(typeof(InputSystem))]      // 在 InputSystem 之后执行
public partial class DecisionSystem : SystemBase { }

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(DecisionSystem))]   // 在 DecisionSystem 之后执行  
public partial class MovementSystem : SystemBase { }
```

## 7. EntityCommandBuffer

### 7.1 命令缓冲区系统
```csharp
public partial class CombatSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        Entities
            .WithAll<DamageEvent>()
            .ForEach((Entity entity, int entityInQueryIndex, ref HealthData health, in DamageEvent damage) =>
            {
                // 应用伤害
                health.CurrentHealth -= damage.Amount;
                
                // 记录命令（在主线程序执行）
                if (health.CurrentHealth <= 0)
                {
                    ecb.AddComponent<DeadTag>(entityInQueryIndex, entity);
                    ecb.RemoveComponent<DamageEvent>(entityInQueryIndex, entity);
                    
                    // 创建死亡效果实体
                    var deathEntity = ecb.CreateEntity(entityInQueryIndex);
                    ecb.AddComponent(entityInQueryIndex, deathEntity, new DeathEffect
                    {
                        Position = SystemAPI.GetComponent<LocalTransform>(entity).Position
                    });
                }
                else
                {
                    ecb.RemoveComponent<DamageEvent>(entityInQueryIndex, entity);
                }
            })
            .ScheduleParallel();
            
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 7.2 ECB 常用方法
```csharp
// 实体操作
ecb.CreateEntity(int sortKey);
ecb.DestroyEntity(int sortKey, Entity entity);
ecb.Instantiate(int sortKey, Entity prefab);

// 组件操作
ecb.AddComponent<T>(int sortKey, Entity entity);
ecb.AddComponent<T>(int sortKey, Entity entity, T component);
ecb.SetComponent<T>(int sortKey, Entity entity, T component);
ecb.RemoveComponent<T>(int sortKey, Entity entity);

// 缓冲区操作
ecb.AddBuffer<T>(int sortKey, Entity entity);
var buffer = ecb.SetBuffer<T>(int sortKey, Entity entity);
```

## 8. 完整实战示例

### 8.1 玩家角色系统
```csharp
// 组件定义
public struct PlayerTag : IComponentData { }
public struct InputData : IComponentData { public float2 Move; public bool Jump; }
public struct MovementData : IComponentData { public float Speed; public float3 Velocity; }
public struct GroundCheckData : IComponentData { public bool IsGrounded; public float3 Normal; }

// Authoring 组件
public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float Speed = 8f;
    public float JumpForce = 10f;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponent<PlayerTag>(entity);
        dstManager.AddComponent<InputData>(entity);
        dstManager.AddComponentData(entity, new MovementData { Speed = Speed });
        dstManager.AddComponent<GroundCheckData>(entity);
        dstManager.AddComponent<PhysicsVelocity>(entity);
        dstManager.AddComponent<PhysicsMass>(entity);
    }
}

// 输入系统
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float2 moveInput = new float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool jumpInput = Input.GetKeyDown(KeyCode.Space);
        
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref InputData input) =>
            {
                input.Move = moveInput;
                input.Jump = jumpInput;
            })
            .Run();  // 必须在主线程
    }
}

// 移动系统
[BurstCompile]
public partial class PlayerMovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        new MoveJob { DeltaTime = deltaTime }.Schedule();
    }
    
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        
        public void Execute(ref PhysicsVelocity velocity, in InputData input, in MovementData movement, in GroundCheckData ground)
        {
            float3 moveDirection = new float3(input.Move.x, 0, input.Move.y);
            float3 targetVelocity = moveDirection * movement.Speed;
            
            // 应用移动
            velocity.Linear.xz = targetVelocity.xz;
            
            // 处理跳跃
            if (input.Jump && ground.IsGrounded)
            {
                velocity.Linear.y = 8f;
            }
        }
    }
}
```

## 9. 性能最佳实践

### 9.1 内存管理
```csharp
// 使用正确的 Allocator
NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
// 使用后立即释放
entities.Dispose(Dependency);

// 或者使用 using 语句
using (var entities = query.ToEntityArray(Allocator.TempJob))
{
    // 使用 entities
}
```

### 9.2 查询优化
```csharp
// 预先创建查询
private EntityQuery playerQuery;

protected override void OnCreate()
{
    playerQuery = new EntityQueryBuilder(Allocator.Temp)
        .WithAll<PlayerTag, MovementData, LocalTransform>()
        .WithNone<DeadTag, FrozenTag>()
        .Build(this);
}

// 使用 SystemAPI.Query（最新推荐方式）
protected override void OnUpdate()
{
    foreach (var (transform, movement) in SystemAPI.Query<RefRW<LocalTransform>, RefRO<MovementData>>())
    {
        transform.ValueRW.Position += movement.ValueRO.Direction * SystemAPI.Time.DeltaTime;
    }
}
```

### 9.3 启用组件
```csharp
public struct HealthData : IComponentData, IEnableableComponent
{
    public float CurrentHealth;
    public float MaxHealth;
}

// 动态启用/禁用组件
EntityManager.SetComponentEnabled<HealthData>(entity, false);
bool isEnabled = EntityManager.IsComponentEnabled<HealthData>(entity);
```

这份教学涵盖了 ECS 的核心概念和实际应用，从基础组件到高级系统开发，包含了完整的 API 参考和最佳实践。