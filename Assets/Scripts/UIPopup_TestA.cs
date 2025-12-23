using UnityEngine;
using UnityEngine.UI;

public class UIPopup_TestA : UIPopupBase
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
        Debug.Log("Popup A Open");
    }

    public override void OnClose()
    {
        base.OnClose();
        Debug.Log("Popup A Close");
    }
}