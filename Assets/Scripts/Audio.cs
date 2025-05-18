using UnityEngine;

public class Audio : MonoBehaviour
{
    [SerializeField] private AudioSource _soundSource;
    [SerializeField] private AudioClip _tapClip;

    [SerializeField] private AudioSource _musicSource;
    [SerializeField] private AudioClip _winClip;
    [SerializeField] private AudioClip _loseClip;

    [SerializeField] private GameObject musicOnBtn;
    [SerializeField] private GameObject musicOffBtn;

    public static Audio instance;

    private void Start()
    {
        instance = this;
        if (_musicSource != null)
            _musicSource.volume = PlayerPrefs.GetInt("music", 1);
        if (_soundSource != null)
            _soundSource.volume = PlayerPrefs.GetInt("music", 1);
    }

    public void OnButtonTap()
    {
        _soundSource.PlayOneShot(_tapClip);
    }

    public void PlayLose()
    {
        _musicSource.clip = _loseClip;
        _musicSource.Play();
    }

    public void PlayWin()
    {
        _musicSource.clip = _winClip;
        _musicSource.Play();
    }

    public void MusicOn()
    {
        _musicSource.volume = 1;
        _soundSource.volume = 1;

        PlayerPrefs.SetInt("music", 1);

        musicOffBtn.SetActive(false);
        musicOnBtn.SetActive(true);
    }

    public void MusicOff()
    {
        _musicSource.volume = 0;
        _soundSource.volume = 0;

        PlayerPrefs.SetInt("music", 0);

        musicOffBtn.SetActive(true);
        musicOnBtn.SetActive(false);
    }
}
