using System;

[Serializable]
public class PopupRequest
{
    public UIPopupBase prefab;
    public EPopupPriority priority;
    public object payload;
    public bool unique;
    public int sequence;
}
