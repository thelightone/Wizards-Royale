using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static Player;

public class MatchManager : MonoBehaviour
{
    public static MatchManager instance;

    public List<Player> playersList = new List<Player>();

    public Transform respawnBlue;
    public Transform respawnRed;

    private float matchTime = 45;
    public bool inMatch = true;
    public TMP_Text timer;

    public GameObject winPanel;
    public GameObject losePanel;

    private int blueScores;
    private int redScores;
    public TMP_Text blueScoresText;
    public TMP_Text redScoresText;

    public Image blueImg1;
    public Image blueImg2;
    public Image blueImg3;
    public Image redImg1;
    public Image redImg2;
    public Image redImg3;

    public GameObject startPanel;

    private bool bigFont;
    public TMP_Text timeOverText;

    private void Awake()
    {
        inMatch = false;
        StartCoroutine(PreMatchCor());
        instance = this;
    }

    public void SetAvatars()
    {
        blueImg1.sprite = playersList[0].GetComponent<CharacterSkinManager>().avatar;
        blueImg2.sprite = playersList[1].GetComponent<CharacterSkinManager>().avatar;
        blueImg3.sprite = playersList[2].GetComponent<CharacterSkinManager>().avatar;
        redImg1.sprite = playersList[3].GetComponent<CharacterSkinManager>().avatar;
        redImg2.sprite = playersList[4].GetComponent<CharacterSkinManager>().avatar;
        redImg3.sprite = playersList[5].GetComponent<CharacterSkinManager>().avatar;
    }

    private IEnumerator PreMatchCor()
    {
        startPanel.SetActive(true);
        yield return new WaitForEndOfFrame();
        SetAvatars();
        yield return new WaitForSeconds(3);
        startPanel.SetActive(false);

        yield return new WaitForSeconds(1);
        inMatch = true;
    }

    public int ReturnPlayersCount()
    {
        return playersList.Count;
    }

    private void Update()
    {
        if (inMatch)
        {
            matchTime -= Time.deltaTime;

            if (matchTime < 6 && !bigFont)
            {
                bigFont = true;
                timer.fontSize = 100;
                timer.rectTransform.localPosition = new Vector3(timer.rectTransform.localPosition.x, timer.rectTransform.localPosition.y-130, timer.rectTransform.localPosition.z);
                timer.color = new Color(1,1,1,0.7f);
            }

            if (matchTime <= 0)
            {
                FinishMatch();
            }
        }

        timer.text = String.Format("{0}", (int)matchTime);

    }

    private void FinishMatch()
    {
        StartCoroutine(FinishMatchCor());
    }

    private IEnumerator FinishMatchCor()
    {
        inMatch = false;

        if (blueScores > redScores)
        {
            Audio.instance.PlayWin();
        }
        else
        {
            Audio.instance.PlayLose();
        }

        timeOverText.gameObject.SetActive(true);
        timer.gameObject.SetActive(false);

        PlayerPrefs.SetInt("battles", PlayerPrefs.GetInt("battles") + 1);
        PlayerPrefs.SetInt("gold", PlayerPrefs.GetInt("gold") + 100);

        yield return new WaitForSeconds(2);

        if (blueScores > redScores)
        {
            winPanel.SetActive(true);
        }
        else
        {
            losePanel.SetActive(true);
        }
    }


    public void PlayerDeath(GameObject player)
    {

        var team = player.GetComponent<Player>().team;

        if (team == Team.Red)
        {
            blueScores++;
            blueScoresText.text = blueScores.ToString();
        }
        else
        {
            redScores++;
            redScoresText.text = redScores.ToString();
        }

        player.GetComponent<Health>().Revive();
    }

    public void Retry()
    {
        SceneManager.LoadScene(PlayerPrefs.GetInt("mapId"));

    }

    public void ToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
