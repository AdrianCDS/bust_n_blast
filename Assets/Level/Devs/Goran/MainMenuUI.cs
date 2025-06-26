using UnityEngine;
using UnityEditor;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private GameObject startButtonsPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject shopPanel;

    public void OpenSettings()
    {
        startButtonsPanel.SetActive(false);
        settingsPanel.SetActive(true);
        shopPanel.SetActive(false);
    }

    public void OpenShop()
    {
        startButtonsPanel.SetActive(false);
        settingsPanel.SetActive(false);
        shopPanel.SetActive(true);
    }

    public void BackToMenu()
    {
        settingsPanel.SetActive(false);
        startButtonsPanel.SetActive(true);
        shopPanel.SetActive(false);
    }

    public void ExitApplication()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}