using UnityEngine;

public class Test : MonoBehaviour
{
    void Start()
    {
        var itemTable = GameManager.Instance.Tables.Item.Get(2);

        var id_0 = itemTable.RowKey;
        var name_0 = itemTable.name;
        var desc_0 = itemTable.description;

        Debug.Log($"{id_0}, {name_0}, {desc_0}");
    }
}
