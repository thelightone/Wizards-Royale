using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class Health : MonoBehaviour, IDamageable
{
    [SerializeField] private float _maxHealth = 5f;
    [SerializeField] private float _currentHealth;

    private Animator _animator;
    private HealthBar _healthBar;

    public bool dead;

    public Transform respawnPoint;

    public TMP_Text damageText;

    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();
        _healthBar = GetComponent<HealthBar>();

        _currentHealth = _maxHealth;
        damageText.gameObject.SetActive(false);
    }

    public void TakeDamage(float damage)
    {
        if (_animator == null) 
        { 
            _animator = GetComponentInChildren<Animator>(); 
        }
        _animator.SetTrigger("GetHit");
        _currentHealth -= damage;
        _healthBar.UpdateHealth(_currentHealth,_maxHealth);

        if(_currentHealth<=0 && !dead)
        {
            Death();
        }

        StartCoroutine(ShowDamageText(damage));
            
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