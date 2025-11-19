using System;
using UnityEngine;
// 武器接口
public interface IWeapon
{
    string Name { get; }
    void Attack();
    void Reload();
}

// 具体武器实现
public class Pistol : IWeapon
{
    public string Name => "手枪";

    public void Attack()
    {
        Debug.Log($"使用{Name}射击，伤害：10");
    }

    public void Reload()
    {
        Debug.Log($"为{Name}装弹，耗时：1秒");
    }
}

public class Shotgun : IWeapon
{
    public string Name => "霰弹枪";

    public void Attack()
    {
        Debug.Log($"使用{Name}射击，伤害：25");
    }

    public void Reload()
    {
        Debug.Log($"为{Name}装弹，耗时：2秒");
    }
}

public class Rifle : IWeapon
{
    public string Name => "步枪";

    public void Attack()
    {
        Debug.Log($"使用{Name}射击，伤害：15");
    }

    public void Reload()
    {
        Debug.Log($"为{Name}装弹，耗时：1.5秒");
    }
}