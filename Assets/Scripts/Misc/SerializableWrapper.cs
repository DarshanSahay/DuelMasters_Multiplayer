using System;

[Serializable]
public class StringIntPair
{
    public string key;
    public int value;
}

[Serializable]
public class StringIntPairList
{
    public StringIntPair[] list;
}

[Serializable]
public class StringIntArrayPair
{
    public string key;
    public int[] values;
}

[Serializable]
public class StringIntArrayPairList
{
    public StringIntArrayPair[] list;
}

[Serializable]
public class PlayerEntry
{
    public string playerId;
    public PlayerStateDTO state;
}

[Serializable]
public class PlayerEntryList
{
    public PlayerEntry[] list;
}
