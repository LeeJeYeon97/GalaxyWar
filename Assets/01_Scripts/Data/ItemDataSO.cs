using UnityEngine;


[CreateAssetMenu(fileName = "ItemData", menuName = "ScriptableObjects/ItemData")]
public class ItemDataSO : ScriptableObject
{
    public Define.ItemType type;
    public string itemName;
    public Sprite sprite;
    public float value;

}
