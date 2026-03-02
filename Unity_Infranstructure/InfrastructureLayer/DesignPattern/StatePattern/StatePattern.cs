// ICharacterState.cs
using System.Collections.Generic;
using System.Xml;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.TextCore.Text;

public interface ICharacterState
{
    void EnterState(Character character);
    void Update(Character character);
    void ExitState(Character character);
    void HandleInput(Character character);
}

// Character.cs
//public class Character : MonoBehaviour
//{
//    [Header("角色属性")]
//    public float MoveSpeed = 5f;
//    public float AttackRange = 2f;
//    public float AttackDuration = 1f;
//    public float AttackCooldown = 1.5f;
//    public float JumpForce = 8f;
//    public float JumpDuration = 1f;

//    [Header("组件引用")]
//    public Animator Animator;

//    private ICharacterState _currentState;
//    private ICharacterState _previousState;

//    void Start()
//    {
//        Animator = GetComponent<Animator>();
//        // 初始状态为闲置
//        ChangeState(new IdleState());
//    }

//    void Update()
//    {
//        _currentState?.Update(this);
//        _currentState?.HandleInput(this);
//    }

//    // 改变状态
//    public void ChangeState(ICharacterState newState)
//    {
//        if (_currentState != null)
//        {
//            _previousState = _currentState;
//            _currentState.ExitState(this);
//        }

//        _currentState = newState;
//        _currentState.EnterState(this);
//    }

//    // 返回上一个状态
//    public void ReturnToPreviousState()
//    {
//        if (_previousState != null)
//        {
//            ChangeState(_previousState);
//        }
//    }

//    // 获取当前状态名称（用于调试）
//    public string GetCurrentStateName()
//    {
//        return _currentState?.GetType().Name ?? "None";
//    }

//    // 受伤方法
//    public void TakeDamage(int damage)
//    {
//        // 这里可以添加生命值逻辑
//        // 如果生命值 <= 0，切换到死亡状态
//        ChangeState(new DeadState());
//    }

//    // 在Inspector中调试用的方法
//    [ContextMenu("切换到闲置状态")]
//    public void DebugIdleState() => ChangeState(new IdleState());

//    [ContextMenu("切换到移动状态")]
//    public void DebugMoveState() => ChangeState(new MoveState());

//    [ContextMenu("切换到攻击状态")]
//    public void DebugAttackState() => ChangeState(new AttackState());

//    [ContextMenu("切换到死亡状态")]
//    public void DebugDeadState() => ChangeState(new DeadState());
//}

// StateMachine.cs
public class StateMachine
{
    private Dictionary<System.Type, ICharacterState> _states;
    private ICharacterState _currentState;

    public StateMachine()
    {
        _states = new Dictionary<System.Type, ICharacterState>();
    }

    public void AddState(ICharacterState state)
    {
        _states[state.GetType()] = state;
    }

    public void ChangeState<T>(Character character) where T : ICharacterState
    {
        if (_states.TryGetValue(typeof(T), out ICharacterState newState))
        {
            _currentState?.ExitState(character);
            _currentState = newState;
            _currentState.EnterState(character);
        }
    }

    public void Update(Character character)
    {
        _currentState?.Update(character);
    }

    public void HandleInput(Character character)
    {
        _currentState?.HandleInput(character);
    }
}
