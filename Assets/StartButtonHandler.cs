using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonHandler : MonoBehaviour
{
    public void StartButtonClicked()
    {
        SceneManager.LoadScene("NewScene");
    }
}