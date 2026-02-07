using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PoolingData", menuName = "ScriptableObjects/PoolingData")]
public class PoolingDataSO : ScriptableObject
{
    public List<Pool> poolList = new List<Pool>();
}
