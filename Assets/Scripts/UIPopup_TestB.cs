using UnityEngine;
using UnityEngine.UI;

public class UIPopup_TestB : UIPopupBase
{
    private void Awake()
    {
        Button btn = GetComponentInChildren<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(CloseSelf);
        }
    }

    public override void OnOpen(object payload)
    {
        base.OnOpen(payload);
        Debug.Log("Popup B Open");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("Popup B Close");
    }
}
