using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Scene2 : MonoBehaviour
{
    public TMP_InputField redInputName;
    public TMP_InputField blueInputName;
    public static string redName;
    public static string blueName;

    void Update()
    {
        redName = redInputName.text;
        blueName = blueInputName.text;
    }

    public void NextScene()
    {
        SceneManager.LoadScene(2);
    }
}
