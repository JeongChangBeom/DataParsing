using UnityEngine;

public abstract class UIPopupBase : MonoBehaviour
{
    public bool IsOpen { get; private set; }

    internal System.Type PopupType { get; private set; }

    internal void SetPopupType(System.Type t)
    {
        PopupType = t;
    }

    public virtual void OnOpen(object payload)
    {
        IsOpen = true;
        gameObject.SetActive(true);
    }

    public virtual void OnClose()
    {
        IsOpen = false;
        gameObject.SetActive(false);
    }

    public void CloseSelf()
    {
        if (UIManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }

        UIManager.Instance.ClosePopup(this);
    }
}
