using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider _healthSlider;

    public void UpdateHealth(float curHealth, float maxHealth)
    {
        _healthSlider.value = curHealth/maxHealth;
    }

    public void Update()
    {
        _healthSlider.transform.rotation = Camera.main.transform.rotation;
    }
}
