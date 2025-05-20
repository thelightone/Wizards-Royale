using UnityEngine;
using UnityEngine.UI;

public class SkinButton : MonoBehaviour
{
    public int id;
    public Sprite sprite;
    public string name;
    public int battlesToUnlock;
    public GameObject locked;
    public bool unlocked;
    public GameObject character;

    [Header("Weapon Data")]
    public int damage;
    public int range;
    public int cooldown;
    public string feature;

    public void Init()
    {
        locked = transform.GetChild(0).gameObject;
        Deselect();
        CheckLock();
    }

    private void CheckLock()
    {
        var completedBattles = PlayerPrefs.GetInt("battles", 0);
        if (completedBattles>= battlesToUnlock)
        {
            Unlock();
        }
    }

    public void Unlock()
    {
        locked.SetActive(false);
        unlocked = true;
    }

    public void Select()
    {
        transform.GetChild(1).gameObject.SetActive(true);
    }

    public void ShowChar()
    {
        character.SetActive(true);
    }

    public void Deselect()
    {
        transform.GetChild(1).gameObject.SetActive(false);
    }

    public void UnshowChar()
    {
        character.SetActive(false);
    }
}
