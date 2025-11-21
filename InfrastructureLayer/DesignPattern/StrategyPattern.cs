using UnityEngine;
// IAttackStrategy.cs
public interface IAttackStrategy
{
    void Attack();
}
// MeleeAttackStrategy.cs
public class MeleeAttackStrategy : IAttackStrategy
{
    public void Attack()
    {
        Debug.Log("进行近战攻击！造成 10 点伤害");
        // 近战攻击的具体逻辑
    }
}

// RangedAttackStrategy.cs
public class RangedAttackStrategy : IAttackStrategy
{
    public void Attack()
    {
        Debug.Log("进行远程攻击！造成 8 点伤害");
        // 远程攻击的具体逻辑
    }
}

// MagicAttackStrategy.cs
public class MagicAttackStrategy : IAttackStrategy
{
    public void Attack()
    {
        Debug.Log("进行魔法攻击！造成 15 点伤害，消耗 5 点魔法值");
        // 魔法攻击的具体逻辑
    }
}

// AttackStrategyFactory.cs
public static class AttackStrategyFactory
{
    public static IAttackStrategy CreateStrategy(AttackType type)
    {
        switch (type)
        {
            case AttackType.Melee:
                return new MeleeAttackStrategy();
            case AttackType.Ranged:
                return new RangedAttackStrategy();
            case AttackType.Magic:
                return new MagicAttackStrategy();
            default:
                return new MeleeAttackStrategy();
        }
    }
}

public enum AttackType
{
    Melee,
    Ranged,
    Magic
}
