using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [SerializeField] private Image _choosenImage;
    [SerializeField] private TMP_Text _nameText;
    [SerializeField] private TMP_Text _lockText;
    [SerializeField] private TMP_Text _levelText;
    [SerializeField] private TMP_Text _levelText2;
    [SerializeField] private List<SkinButton> _buttons = new List<SkinButton>();

    private int choosenSkinId;

    [SerializeField] private Transform mainPanelCharsParent;
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text goldText;

    [Header("Panels")]
    [SerializeField] private GameObject heroesPanel;
    [SerializeField] private GameObject mainPanel;
    [SerializeField] private GameObject mapPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject loadingPanel;


    [Header("Maps")]
    [SerializeField] private Button forgeBtn;
    [SerializeField] private Button treasureBtn;
    [SerializeField] private Transform select;
    [SerializeField] private Sprite forgeSprite;
    [SerializeField] private Sprite treasureSprite;
    [SerializeField] private Image mapPreviewImage;
    [SerializeField] private TMP_Text mapPreviewText;



    private void Awake()


    {
        //Инициализация панели карт
        forgeBtn.onClick.AddListener(() => ChooseMap(forgeBtn, 1));
        treasureBtn.onClick.AddListener(() => ChooseMap(treasureBtn, 2));

        if (PlayerPrefs.GetInt("mapId", 1) == 1)
        {
            ChooseMap(forgeBtn, 1);
        }
        else if (PlayerPrefs.GetInt("mapId", 1) == 2)
        {
            ChooseMap(treasureBtn, 2);
        }

        //Инициализация панели героев
        heroesPanel.SetActive(true);

        foreach (var button in _buttons)
        {
            button.Init();
        }

        choosenSkinId = PlayerPrefs.GetInt("skinId", 0);
        ChooseSkin(_buttons[choosenSkinId]);
        SetSkin();

        heroesPanel.SetActive(false);

        _levelText.text = "Level: " + PlayerPrefs.GetInt("battles", 1);
        _levelText2.text = PlayerPrefs.GetInt("battles", 1).ToString();

        goldText.text = PlayerPrefs.GetInt("gold", 0).ToString();
    }

    private void ChooseMap(Button forgeBtn, int mapId)
    {
        PlayerPrefs.SetInt("mapId", mapId);
        select.position = forgeBtn.transform.position;

        if (mapId == 1)
        {
            mapPreviewImage.sprite = forgeSprite;
            mapPreviewText.text = "Forge";
        }
        else
        {
            mapPreviewImage.sprite = treasureSprite;
            mapPreviewText.text = "Treasure";
        }

    }

    public void ChooseSkin(SkinButton skinButton)
    {
        choosenSkinId = skinButton.id;
        _choosenImage.sprite = skinButton.sprite;
        _nameText.text = skinButton.name;
        _lockText.text = "Achieve " + skinButton.battlesToUnlock + " level to unlock";

        foreach (var b in _buttons)
        {
            if (b == skinButton)
            {
                b.ShowChar();
            }
            else
            {
                b.UnshowChar();
            }
        }

        if (skinButton.unlocked)
        {
            _lockText.gameObject.SetActive(false);
            SetSkin();
        }
        else
        {
            _lockText.gameObject.SetActive(true);
        }

        foreach (Transform t in mainPanelCharsParent)
        {
            t.gameObject.SetActive(false);
        }

        mainPanelCharsParent.GetChild(PlayerPrefs.GetInt("skinId", 0)).gameObject.SetActive(true);
    }

    public void SetSkin()
    {
        PlayerPrefs.SetInt("skinId", choosenSkinId);

        foreach (var b in _buttons)
        {
            if (b.id == choosenSkinId)
            {
                b.Select();
            }
            else
            {
                b.Deselect();
            }
        }
    }

    public void StartGame()
    {
        StartCoroutine(StartGameCor());
    }

    private IEnumerator StartGameCor()
    {
        mainPanel.SetActive(false);
        loadingPanel.SetActive(true);

        var op = SceneManager.LoadSceneAsync(PlayerPrefs.GetInt("mapId"));

        while (!op.isDone)
        {
            loadingText.text = "Loading... " + Convert.ToInt32(op.progress*100)+"%";
            Debug.Log(Convert.ToInt32(op.progress * 100));
            yield return null;  
        }
    }
}
