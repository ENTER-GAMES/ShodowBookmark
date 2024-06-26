using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TurnPoint : Point<ITurnable>
{
    [Header("Value")]
    [SerializeField]
    protected bool useMoveDirection = false;
    [SerializeField]
    protected Vector3 moveDirection;

    protected override void Hit(ITurnable target)
    {
        if (useMoveDirection)
            target.Turn(moveDirection);
        else
            target.Turn();
    }
}

public interface ITurnable : IAbleForPoint
{
    public void Turn();
    public void Turn(Vector3 moveDirection);
}