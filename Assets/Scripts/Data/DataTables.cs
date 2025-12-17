public class DataTables
{
    public Item Item { get; private set; }
    public Quest Quest { get; private set; }
    public Monster Monster { get; private set; }

    public void Init()
    {
        Item = TableLoader.Load<Item>("GeneratedTables/Item");
        Quest = TableLoader.Load<Quest>("GeneratedTables/Quest");
        Monster = TableLoader.Load<Monster>("GeneratedTables/Monster");
    }
}
