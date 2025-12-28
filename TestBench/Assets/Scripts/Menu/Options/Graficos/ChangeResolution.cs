using UnityEngine;
using TMPro;

public class ChangeResolutionTMP : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown dropdown;

    private void Awake()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        if (dropdown == null)
        {
            Debug.LogError("TMP_Dropdown es null. Asigna la referencia en el Inspector o pon el script en el mismo GO.");
            return;
        }

        dropdown.onValueChanged.AddListener(OnDropdownChanged);
        Debug.Log("First Value: " + dropdown.value);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        switch (index)
        {
            case 0:
                Screen.SetResolution(1920, 1080, true);
                break;
            case 1:
                Screen.SetResolution(1280, 720, true);
                break;
        }
    }
}