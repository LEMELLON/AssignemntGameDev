using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class Scene3 : MonoBehaviour
{
    public VideoClip blueAttack;
    public VideoClip redAttack;
    public VideoClip blueStance;
    public VideoClip redStance;
    public VideoPlayer blueVidPlayer;
    public VideoPlayer redVidPlayer;
    public TMP_Text redGameName;
    public TMP_Text blueGameName;
    public TMP_Text blueHPText;
    public TMP_Text redHPText;
    public int blueHP = 50;
    public int redHP = 50;

    void Start()
    {
        redGameName.text = Scene2.redName;
        blueGameName.text = Scene2.blueName;
        blueHPText.text = blueHP.ToString();
        redHPText.text = redHP.ToString();
    }

    public void AttackRed()
    {
        Attack(redAttack, redVidPlayer);
    }

    public void AttackBlue()
    {
        Attack(blueAttack, blueVidPlayer);
    }

    void Attack(VideoClip vcAttack, VideoPlayer vp)
    {
        vp.clip = vcAttack;
        vp.isLooping = false;
        vp.loopPointReached += OnVideoFinish;
        vp.Play();
    }

    void OnVideoFinish(VideoPlayer vp)
    {
        vp.loopPointReached -= OnVideoFinish;
        vp.isLooping = true;

        if (vp == redVidPlayer)
        {
            blueHP -= 10;
            blueHPText.text = blueHP.ToString();
            vp.clip = redStance;
        }
        else if (vp == blueVidPlayer)
        {
            redHP -= 10;
            redHPText.text = redHP.ToString();
            vp.clip = blueStance;
        }

        vp.Play();

        if (redHP <= 0 || blueHP <= 0)
        {
            SceneManager.LoadScene(3);
        }
    }
}
