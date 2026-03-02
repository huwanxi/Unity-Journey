# ECS 经典示例：太空射击游戏

## 1. 组件定义

### 1.1 标签组件
```csharp
using Unity.Entities;
using Unity.Mathematics;

// 🟢 标签组件 - 仅用于标记
public struct PlayerTag : IComponentData { }
public struct EnemyTag : IComponentData { }
public struct BulletTag : IComponentData { }
public struct NeedsInitialization : IComponentData { }

// 🟢 生成标签
public struct SpawnerTag : IComponentData 
{
    public float Timer;
    public float SpawnInterval;
}

public struct SpawnEnemyTag : IComponentData 
{
    public Entity Prefab;
    public int Count;
}
```

### 1.2 数据组件
```csharp
// 🟢 移动相关
public struct MovementData : IComponentData
{
    public float Speed;
    public float3 Direction;
}

public struct RotationData : IComponentData
{
    public float RadiansPerSecond;
}

// 🟢 战斗相关
public struct HealthData : IComponentData
{
    public float CurrentHealth;
    public float MaxHealth;
}

public struct AttackData : IComponentData
{
    public float Damage;
    public float FireRate;
    public float Cooldown;
}

// 🟢 生命周期
public struct LifetimeData : IComponentData
{
    public float TimeRemaining;
}
```

### 1.3 共享组件
```csharp
public struct RenderColor : ISharedComponentData
{
    public float4 Value;
}
```

## 2. Authoring 组件

### 2.1 玩家 Authoring
```csharp
using UnityEngine;

public class PlayerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float MoveSpeed = 5f;
    public float FireRate = 0.2f;
    public float BulletSpeed = 10f;
    public GameObject BulletPrefab;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // 🟢 必需：变换和渲染组件
        dstManager.AddComponentData(entity, new LocalTransform
        {
            Position = transform.position,
            Rotation = transform.rotation,
            Scale = 1f
        });
        dstManager.AddComponent<LocalToWorld>(entity);
        
        // 🟢 添加渲染
        var renderer = GetComponent<Renderer>();
        if (renderer != null)
        {
            dstManager.AddSharedComponent(entity, new RenderMesh
            {
                mesh = GetComponent<MeshFilter>().sharedMesh,
                material = renderer.sharedMaterial
            });
        }
        
        // 🟢 逻辑组件
        dstManager.AddComponent<PlayerTag>(entity);
        dstManager.AddComponentData(entity, new MovementData { Speed = MoveSpeed });
        dstManager.AddComponentData(entity, new AttackData 
        { 
            FireRate = FireRate,
            Damage = 10f,
            Cooldown = 0f
        });
        dstManager.AddComponentData(entity, new HealthData { CurrentHealth = 100, MaxHealth = 100 });
        
        // 🟢 存储子弹预制体引用
        if (BulletPrefab != null)
        {
            Entity bulletPrefab = conversionSystem.GetPrimaryEntity(BulletPrefab);
            dstManager.AddComponentData(entity, new BulletSpawnData { Prefab = bulletPrefab, Speed = BulletSpeed });
        }
    }
}

public struct BulletSpawnData : IComponentData
{
    public Entity Prefab;
    public float Speed;
}
```

### 2.2 敌人生成器 Authoring
```csharp
public class EnemySpawnerAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    public float SpawnInterval = 2f;
    public GameObject EnemyPrefab;
    
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SpawnerTag 
        { 
            Timer = 0f,
            SpawnInterval = SpawnInterval 
        });
        
        if (EnemyPrefab != null)
        {
            Entity enemyPrefab = conversionSystem.GetPrimaryEntity(EnemyPrefab);
            dstManager.AddComponentData(entity, new SpawnEnemyTag 
            { 
                Prefab = enemyPrefab,
                Count = 10  // 生成数量限制
            });
        }
    }
}
```

## 3. 系统完整执行顺序

### 3.1 初始化阶段
```csharp
[UpdateInGroup(typeof(InitializationSystemGroup))]
[UpdateBefore(typeof(BeginInitializationEntityCommandBufferSystem))]
public partial class SetupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 🟢 第一执行：游戏初始化
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("游戏开始初始化...");
        }
    }
}
```

### 3.2 输入处理阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(BeginSimulationEntityCommandBufferSystem))]
public partial class PlayerInputSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float2 moveInput = new float2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        bool fireInput = Input.GetKey(KeyCode.Space);
        
        // 🟢 第二执行：处理玩家输入
        Entities
            .WithAll<PlayerTag>()
            .ForEach((ref MovementData movement, ref AttackData attack) =>
            {
                movement.Direction = new float3(moveInput.x, 0, moveInput.y);
                
                if (fireInput && attack.Cooldown <= 0f)
                {
                    attack.Cooldown = attack.FireRate;
                }
            })
            .Run();  // 必须在主线程
    }
}
```

### 3.3 敌人生成阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(PlayerInputSystem))]
public partial class EnemySpawnSystem : SystemBase
{
    private BeginInitializationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        // 🟢 第三执行：敌人生成逻辑
        Entities
            .WithAll<SpawnerTag, SpawnEnemyTag>()
            .ForEach((Entity spawner, int entityInQueryIndex, ref SpawnerTag spawnerData, in SpawnEnemyTag spawnEnemy) =>
            {
                spawnerData.Timer += deltaTime;
                
                if (spawnerData.Timer >= spawnerData.SpawnInterval)
                {
                    spawnerData.Timer = 0f;
                    
                    // 生成敌人
                    Entity enemy = ecb.Instantiate(entityInQueryIndex, spawnEnemy.Prefab);
                    ecb.SetComponent(entityInQueryIndex, enemy, new LocalTransform
                    {
                        Position = new float3(UnityEngine.Random.Range(-8f, 8f), 0, 10f),
                        Rotation = quaternion.identity,
                        Scale = 1f
                    });
                    
                    Debug.Log("生成敌人！");
                }
            })
            .ScheduleParallel();
            
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 3.4 攻击系统阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(EnemySpawnSystem))]
public partial class AttackSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        // 🟢 第四执行：攻击和子弹生成
        Entities
            .WithAll<PlayerTag, BulletSpawnData>()
            .ForEach((Entity player, int entityInQueryIndex, ref AttackData attack, in BulletSpawnData bulletData, in LocalTransform transform) =>
            {
                // 冷却计时
                if (attack.Cooldown > 0f)
                {
                    attack.Cooldown -= deltaTime;
                }
                
                // 发射子弹
                if (attack.Cooldown <= 0f)
                {
                    attack.Cooldown = attack.FireRate;
                    
                    Entity bullet = ecb.Instantiate(entityInQueryIndex, bulletData.Prefab);
                    ecb.SetComponent(entityInQueryIndex, bullet, new LocalTransform
                    {
                        Position = transform.Position + new float3(0, 0.5f, 0),
                        Rotation = quaternion.identity,
                        Scale = 0.2f
                    });
                    
                    ecb.SetComponent(entityInQueryIndex, bullet, new MovementData
                    {
                        Speed = bulletData.Speed,
                        Direction = new float3(0, 0, 1)
                    });
                    
                    ecb.AddComponent<BulletTag>(entityInQueryIndex, bullet);
                    ecb.AddComponent(entityInQueryIndex, bullet, new LifetimeData { TimeRemaining = 3f });
                }
            })
            .ScheduleParallel();
            
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 3.5 移动系统阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(AttackSystem))]
public partial class MovementSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        
        // 🟢 第五执行：所有实体移动
        new MoveJob { DeltaTime = deltaTime }.ScheduleParallel();
    }
    
    [BurstCompile]
    public partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        
        [BurstCompile]
        public void Execute(ref LocalTransform transform, in MovementData movement)
        {
            transform.Position += movement.Direction * movement.Speed * DeltaTime;
            
            // 边界检查
            if (math.abs(transform.Position.x) > 9f)
            {
                transform.Position.x = math.clamp(transform.Position.x, -9f, 9f);
            }
        }
    }
}
```

### 3.6 碰撞检测阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(MovementSystem))]
public partial class CollisionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        // 🟢 第六执行：简单的碰撞检测
        Entities
            .WithAll<BulletTag>()
            .ForEach((Entity bullet, int bulletIndex, in LocalTransform bulletTransform) =>
            {
                Entities
                    .WithAll<EnemyTag>()
                    .ForEach((Entity enemy, int enemyIndex, ref HealthData enemyHealth, in LocalTransform enemyTransform) =>
                    {
                        float distance = math.distance(bulletTransform.Position, enemyTransform.Position);
                        
                        if (distance < 1f)  // 简单距离检测
                        {
                            // 子弹击中敌人
                            enemyHealth.CurrentHealth -= 25f;
                            ecb.DestroyEntity(bulletIndex, bullet);
                            
                            if (enemyHealth.CurrentHealth <= 0f)
                            {
                                ecb.DestroyEntity(enemyIndex, enemy);
                                Debug.Log("敌人被消灭！");
                            }
                        }
                    })
                    .Run();  // 内层循环在主线程
            })
            .Run();  // 外层循环在主线程
            
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 3.7 生命周期系统阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(CollisionSystem))]
public partial class LifetimeSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem ecbSystem;
    
    protected override void OnCreate()
    {
        ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = ecbSystem.CreateCommandBuffer().AsParallelWriter();
        
        // 🟢 第七执行：生命周期管理
        Entities
            .WithAll<LifetimeData>()
            .ForEach((Entity entity, int entityInQueryIndex, ref LifetimeData lifetime) =>
            {
                lifetime.TimeRemaining -= deltaTime;
                
                if (lifetime.TimeRemaining <= 0f)
                {
                    ecb.DestroyEntity(entityInQueryIndex, entity);
                }
            })
            .ScheduleParallel();
            
        ecbSystem.AddJobHandleForProducer(Dependency);
    }
}
```

### 3.8 清理阶段
```csharp
[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(LifetimeSystem))]
[UpdateBefore(typeof(EndSimulationEntityCommandBufferSystem))]
public partial class CleanupSystem : SystemBase
{
    protected override void OnUpdate()
    {
        // 🟢 第八执行：清理和统计
        int enemyCount = SystemAPI.Query<EnemyTag>().CalculateEntityCount();
        int bulletCount = SystemAPI.Query<BulletTag>().CalculateEntityCount();
        
        if (SystemAPI.Time.ElapsedTime % 5f < 0.1f)  // 每5秒输出一次
        {
            Debug.Log($"统计 - 敌人: {enemyCount}, 子弹: {bulletCount}");
        }
    }
}
```

## 4. 系统执行顺序总结

```
1. SetupSystem (初始化)
2. PlayerInputSystem (输入处理)
3. EnemySpawnSystem (敌人生成)
4. AttackSystem (攻击和子弹生成)
5. MovementSystem (移动处理)
6. CollisionSystem (碰撞检测)
7. LifetimeSystem (生命周期)
8. CleanupSystem (清理统计)
```

## 5. 使用方式

1. 创建 Player、Enemy、Bullet 的 Prefab
2. 添加对应的 Authoring 组件
3. 创建 EnemySpawner GameObject
4. 运行游戏，按空格发射子弹

这个完整示例展示了 ECS 的典型使用模式：标签标记、数据驱动、系统分层、命令缓冲区和完整的执行顺序控制。