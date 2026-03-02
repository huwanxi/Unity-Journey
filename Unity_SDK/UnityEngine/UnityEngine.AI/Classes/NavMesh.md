# NavMesh

class in Unityengine.AI

## 描述

用于访问烘培导航网格的单例类

Use the NavMesh class to perform spatial queries such as pathfinding and walkability tests.This class also lets you set the pathfinding cost foe specific area types, and tweak the global behavior of pathfinding and avoidance.

Before you can use spatial queries,youmust first back the NavMesh to your scene.

# 静态变量

# 1.NavMesh.AllAreas

public static int AllAreas;

## 描述

包含所有导航网格区域的区域遮罩常量。

```cshape
//targetReachable
using UnityEngine;
using UnityEngine.AI;
public class TargetReachable:MonoBehaviour
{
    public Transform target;
    private NavMeshHit hit;
    private bool blocked = false; 

    void Update()
    {
        // Allow pass through all area types when testing if the target position
       // is reachable from the transform location 
       blocked = NavMesh.Raycast(transform.position,target.position, out hit,NavMesh.AllAreas);
       Debug.DrawLine(transform.position,target.position,blocked? Color.red : Color.green);
       if(blocked)
           Debug.DrawRay(hit.position,Vector3.up,Color.red);
    }
}
```
# 2.NavMesh.avoidancePredictionTime

public static float avoidancePredictionTime;

## 描述

描述所有代理agent在未来多久后预测碰撞，以便进行规避。

值越大，代理就会越早开始避开彼此（如果它们处于碰撞轨迹中）。该值以秒为单位进行测量。默认值为2.0，合适的调整范围介于0.5和5.0之间。

# 3.NavMesh.onPreUpdate

public static AI.NavMesh.OnNavMeshPreUpdate onPreUpdate;

## 描述

设置一个要在执行帧更新期间且在导航网格更新之前调用的函数。

借助此属性，你可以设置一个要在每帧且在导航网格系统更新之前立即调用的委托函数。

# 4.NavMesh.pathfindingIterationsPerFrame

public static int pathfindingIterationsPerFrame;

## 描述

一个用于性能优化的静态属性。它控制 Unity 的寻路系统每帧花多少次数来处理寻路请求`agent.SetDestination`。



















