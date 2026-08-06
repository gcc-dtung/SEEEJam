using System;

[Serializable]
public class GameData
{
    public int saveVersion = 1;
    public int currentLevelIndex;
    public int coinCount;
    public int undoBoosterCount;
    public int removeConditionsBoosterCount;
    public int hintBoosterCount;
    public long savedAtUtcTicks;
}
