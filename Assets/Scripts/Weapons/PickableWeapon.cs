using UnityEngine;

public class PickableWeapon : MonoBehaviour
{
    [SerializeField] private WeaponData _weaponData;
    [SerializeField] private float _rotationSpeed = 50f;
    [SerializeField] private float _hoverHeight = 0.5f;
    [SerializeField] private float _hoverSpeed = 1f;
    
    private Vector3 _startPosition;
    private float _timeOffset;
    private bool _isPickedUp;

    private void Start()
    {
        _startPosition = transform.position;
        _timeOffset = Random.Range(0f, 2f * Mathf.PI);

        if (_weaponData == null)
        {
            Debug.LogError($"WeaponData не назначен на {gameObject.name}!");
        }

        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            Debug.LogError($"Нет коллайдера на {gameObject.name}!");
        }
        else if (!collider.isTrigger)
        {
            Debug.LogWarning($"Коллайдер на {gameObject.name} не является триггером!");
            collider.isTrigger = true;
        }

        if (WeaponPickupButton.Instance == null)
        {
            Debug.LogError("WeaponPickupButton.Instance не найден! Убедитесь, что компонент WeaponPickupButton добавлен на сцену.");
        }
    }

    private void Update()
    {
        if (_isPickedUp) return;

        transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        float newY = _startPosition.y + Mathf.Sin((Time.time + _timeOffset) * _hoverSpeed) * _hoverHeight;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isPickedUp) return;

        Debug.Log($"Trigger entered by: {other.gameObject.name}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detected!");
            
            var shootingController = other.GetComponent<ShootingController>() ?? other.GetComponentInParent<ShootingController>();
            if (shootingController != null && WeaponPickupButton.Instance != null)
            {
                Debug.Log("ShootingController found, showing pickup button...");
                WeaponPickupButton.Instance.ShowPickupButton(_weaponData, shootingController, this);
            }
            else
            {
                if (shootingController == null)
                    Debug.LogError("ShootingController не найден на игроке!");
                if (WeaponPickupButton.Instance == null)
                    Debug.LogError("WeaponPickupButton.Instance не найден!");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (_isPickedUp) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited trigger, hiding pickup button");
            WeaponPickupButton.Instance?.HidePickupButton();
        }
    }

    public void OnWeaponPickedUp()
    {
        Debug.Log($"OnWeaponPickedUp called on {gameObject.name}");
        _isPickedUp = true;
        WeaponPickupButton.Instance?.HidePickupButton();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        WeaponPickupButton.Instance?.HidePickupButton();
    }
} 