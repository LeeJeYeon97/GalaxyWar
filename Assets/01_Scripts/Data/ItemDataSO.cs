using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemData")]
public class ItemDataSO : ScriptableObject
{
    public Define.ItemType type;
    public string itemName;
    public bool isDrop;

    public GameObject originalPrefab;

    public int minValue;
    public int maxValue;
}
