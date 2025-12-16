using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public DataTables Tables { get; private set; }

    private void Awake()
    {
        Instance = this;

        Tables = new DataTables();
        Tables.Init();
    }
}
