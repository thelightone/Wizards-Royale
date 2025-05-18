using UnityEngine;
using UnityEngine.UI;

public class WeaponPickupButton : MonoBehaviour
{
    public static WeaponPickupButton Instance { get; private set; }

    [SerializeField] private GameObject _pickupButton;
    private WeaponData _currentWeapon;
    private ShootingController _playerShootingController;
    private PickableWeapon _currentPickableWeapon;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogError("На сцене уже есть экземпляр WeaponPickupButton!");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (_pickupButton == null)
        {
            Debug.LogError("Кнопка не назначена в инспекторе!");
            return;
        }
        
        Debug.Log("WeaponPickupButton initialized");
        _pickupButton.SetActive(false);
    }

    public void ShowPickupButton(WeaponData weaponData, ShootingController playerController, PickableWeapon pickableWeapon)
    {
        Debug.Log($"ShowPickupButton called with weapon: {weaponData?.name}");
        
        if (_pickupButton == null)
        {
            Debug.LogError("Кнопка не назначена!");
            return;
        }

        _currentWeapon = weaponData;
        _playerShootingController = playerController;
        _currentPickableWeapon = pickableWeapon;
        _pickupButton.SetActive(true);
        
        Debug.Log("Pickup button shown");
    }

    public void HidePickupButton()
    {
        if (_pickupButton == null)
        {
            Debug.LogError("Кнопка не назначена!");
            return;
        }

        _pickupButton.SetActive(false);
        _currentWeapon = null;
        _playerShootingController = null;
        _currentPickableWeapon = null;
        
        Debug.Log("Pickup button hidden");
    }

    public void OnPickupButtonClicked()
    {
        Debug.Log("Pickup button clicked");
        
        if (_currentWeapon == null)
        {
            Debug.LogError("Нет текущего оружия!");
            return;
        }
        
        if (_playerShootingController == null)
        {
            Debug.LogError("Нет ссылки на ShootingController!");
            return;
        }

        _playerShootingController.EquipWeapon(_currentWeapon);
        
        if (_currentPickableWeapon != null)
        {
            Debug.Log("Destroying weapon object...");
            _currentPickableWeapon.OnWeaponPickedUp();
        }
        else
        {
            Debug.LogError("Нет ссылки на PickableWeapon!");
        }
        
        HidePickupButton();
    }
} 