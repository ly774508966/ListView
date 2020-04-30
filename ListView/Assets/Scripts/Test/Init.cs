using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Init : MonoBehaviour
{
    private void OnEnable()
    {
        foreach (var button in btnList)
        {
            button.onClick.AddListener(() =>
            {
                SceneManager.LoadScene(button.name);
            });
        }
    }

    private void OnDisable()
    {
        foreach (var button in btnList)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    [SerializeField]
    private List<Button> btnList;
    internal const string initSceneName = "0-Init";

}