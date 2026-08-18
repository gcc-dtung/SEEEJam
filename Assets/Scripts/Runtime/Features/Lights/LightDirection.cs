using UnityEngine;

public enum LightDirection
{
    Up,
    Down,
    Left,
    Right
}

public static class LightDirectionExtensions
{
    public static Vector2 ToVector(this LightDirection direction)
    {
        switch (direction)
        {
            case LightDirection.Down:
                return Vector2.down;
            case LightDirection.Left:
                return Vector2.left;
            case LightDirection.Right:
                return Vector2.right;
            default:
                return Vector2.up;
        }
    }
}
