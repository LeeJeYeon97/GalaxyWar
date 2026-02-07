using UnityEngine;

[System.Serializable]
public class Pool
{
    public Define.Pool type;    // 키 (Enum)
    public GameObject original;   // 밸류
    public int defaultCount = 100; // 초기 생성 개수
}
