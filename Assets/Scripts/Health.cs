using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 5f;
    [SerializeField] private float _currentHealth;

    [SerializeField] private Animator _animator;
    private HealthBar _healthBar;

    public bool dead;

    public Transform respawnPoint;

    public TMP_Text damageText;

    private bool _inShield;
    [SerializeField] private Button _shieldBtn;
    [SerializeField] private GameObject _shield;
    private bool _shildReload;
    [SerializeField] private Image _shieldReloadImage;
    private float _lastShieldTime;

    private void Start()
    {
        if (transform.childCount > 3)
        {
            _animator = transform.GetChild(4).GetComponent<Animator>();
        }
        else
        {
            _animator = GetComponentInChildren<Animator>();
        }
        _healthBar = GetComponent<HealthBar>();

        _currentHealth = _maxHealth;
        damageText.gameObject.SetActive(false);

        _shieldBtn?.onClick.AddListener(() => 
        ActivateShield());
    }

    public void ActivateShield()
    {

        if (!_shildReload)
        {
            StartCoroutine(ActivateShieldCor());
        }
    }

    private IEnumerator ActivateShieldCor()
    {
        _shield.SetActive(true);
        _inShield = true;
        _shildReload = true;

        float elapsTime = 0;
        while (elapsTime<0.5f)
        {
           elapsTime += Time.deltaTime;
            _shield.transform.localScale = Vector3.Lerp(new Vector3(0, 0, 0), new Vector3(1.5f, 1.5f, 1.5f), elapsTime / 0.5f);
            yield return null;
        }


        yield return new WaitForSeconds(1);

        elapsTime = 0;
        while (elapsTime < 0.5f)
        {
            elapsTime += Time.deltaTime;
            _shield.transform.localScale = Vector3.Lerp( new Vector3(1.5f, 1.5f, 1.5f), new Vector3(0, 0, 0), elapsTime / 0.5f);
            yield return null;
        }

        _shield.SetActive(false);
        _inShield = false;
        _lastShieldTime = Time.time;
        yield return new WaitForSeconds(5);
        _shildReload = false;
    }

    private void Update()
    {
        UpdateUI();    
    }

    private void UpdateUI()
    {
        if (_shieldReloadImage != null)
        {
            float cooldownProgress = (Time.time - _lastShieldTime) / 5;
            _shieldReloadImage.fillAmount = Mathf.Clamp01(cooldownProgress);
        }
    }

    public void TakeDamage(float damage)
    {
        if (!_inShield)
        {
            if (_animator == null)
            {
                _animator = GetComponentInChildren<Animator>();
            }
            _animator.SetTrigger("GetHit");
            _currentHealth -= damage;
            _healthBar.UpdateHealth(_currentHealth, _maxHealth);

            if (_currentHealth <= 0 && !dead)
            {
                Death();
            }

            StartCoroutine(ShowDamageText(damage));
        }
            
    }

    private IEnumerator ShowDamageText(float damage)
    {
        damageText.gameObject.SetActive(false);
        damageText.text = Convert.ToInt32(damage).ToString();
        damageText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1);
        damageText.gameObject.SetActive(false);

    }

    private void Death()
    {
        StartCoroutine(DeathCor());

    }

    private IEnumerator DeathCor()
    {
        dead = true;
        _animator.SetTrigger("Death");
        yield return new WaitForSeconds(2);
        gameObject.SetActive(false);
        MatchManager.instance.PlayerDeath(gameObject);
    }

    public void Revive()

    {
        transform.position = respawnPoint.position;
        _animator.SetTrigger("Revive");
        _currentHealth = _maxHealth;
        _healthBar.UpdateHealth(_currentHealth, _maxHealth);
        dead = false;

        gameObject.SetActive(true);
    }
}