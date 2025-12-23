using UnityEngine;

public class UIManagerTester : MonoBehaviour
{
    public UIPopupBase popupA;
    public UIPopupBase popupB;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            UIManager.Instance.RequestPopup(
                popupA,
                EPopupPriority.Normal
            );
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            UIManager.Instance.RequestPopup(
                popupB,
                EPopupPriority.High
            );
        }

        if (Input.GetKeyDown(KeyCode.C))
        {
            UIManager.Instance.CloseTopPopup();
        }
    }
}