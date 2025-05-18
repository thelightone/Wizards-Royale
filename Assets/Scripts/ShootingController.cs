using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class ShootingController : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private FloatingJoystick _shootingJoystick;
    [SerializeField] private Transform _shootingPoint;
    [SerializeField] private WeaponData _defaultWeapon;

    [Header("Интерфейс")]
    [SerializeField] private Image _weaponIcon;
    [SerializeField] private Image _cooldownIndicator;

    [Header("Визуализация прицеливания")]
    [SerializeField] private AimingVisualizer _aimingVisualizer;
    [SerializeField] private GrenadeAimingVisualizer _grenadeAimingVisualizer;

    [Header("Настройки поворота")]
    [SerializeField] private float _rotationSpeed = 15f;
    [SerializeField] private float _aimSensitivity = 0.1f;
    [SerializeField] private float _minShootThreshold = 0.05f;
    [SerializeField] private float _postShootRotationSpeed = 8f;
    [SerializeField] private float _postShootStabilizationTime = 0.15f;

    // Компоненты
    private PlayerController _playerController;
    private Animator _animator;
    private WeaponData _currentWeapon;

    // Состояние прицеливания
    private bool _isAiming;
    private bool _inShot;
    private Vector3 _aimDirection;
    private Vector3 _lastAimDirection;
    private float _lastShootTime;
    private bool _wasAiming;

    // Состояние поворота
    private bool _shouldMaintainRotation = false;
    private Quaternion _targetRotation;

    private void Start()
    {
        // Получаем необходимые компоненты
        _playerController = GetComponent<PlayerController>();
        _animator = GetComponent<CharacterSkinManager>()?.GetCurrentAnimatorAndWeapon();

        // Проверка необходимых компонентов
        if (_shootingJoystick == null || _shootingPoint == null || _defaultWeapon == null)
        {
            Debug.LogError("ShootingController: не заданы обязательные компоненты!");
        }

        // Инициализируем начальное направление
        _lastAimDirection = transform.forward;
        _targetRotation = transform.rotation;
    }

    private void Update()
    {
        if (_currentWeapon == null) return;

        if (MatchManager.instance.inMatch && !GetComponent<Health>().dead)
        {
            // Обрабатываем прицеливание
            HandleAiming();

            // Обрабатываем поворот персонажа
            HandleRotation();

            // Обновляем UI
            UpdateUI();
        }
    }

    // Обработка прицеливания
    private void HandleAiming()
    {
        // Получаем ввод с джойстика стрельбы
        Vector2 input = new Vector2(_shootingJoystick.Horizontal, _shootingJoystick.Vertical);
        float inputMagnitude = input.magnitude;

        // Запоминаем предыдущее состояние прицеливания
        _wasAiming = _isAiming;

        // Определяем, прицеливается ли игрок
        _isAiming = inputMagnitude > _aimSensitivity;

        // Если начали прицеливаться или продолжаем прицеливаться
        if (_isAiming)
        {
            _inShot = true;
            // Вычисляем направление прицеливания
            _aimDirection = new Vector3(input.x, 0, input.y).normalized;
            _lastAimDirection = _aimDirection;

            // Обновляем целевую ротацию плавно
            // _targetRotation = Quaternion.LookRotation(_aimDirection);

            // Устанавливаем флаг сохранения поворота
            _shouldMaintainRotation = true;

            // Обновляем визуализацию прицела
            UpdateAimingVisualizer(inputMagnitude);
            
            // Если используем гранату, сохраняем позицию прицеливания
            if (_currentWeapon != null && _currentWeapon.isGrenade)
            {
                // Рассчитываем целевую точку на основе дистанции броска
                float throwDistance = GetGrenadeThrowDistance(inputMagnitude);
                Vector3 aimPosition = transform.position + _lastAimDirection * throwDistance;
                
                // Сохраняем позицию прицеливания для гранаты
                Grenade.SetPlayerAimPosition(aimPosition);
            }
        }
        else if (_wasAiming && !_isAiming)
        {  
            // Скрываем визуализаторы прицеливания
            _aimingVisualizer?.UpdateAiming(_lastAimDirection, false, 0);
            _grenadeAimingVisualizer?.UpdateAiming(_lastAimDirection, false, 0);

            // Проверяем, нужно ли выстрелить при отпускании джойстика
            CheckForShot();
        }
    }

    // Проверка возможности выстрела при отпускании джойстика
    private void CheckForShot()
    {
        // Получаем текущее направление джойстика
        Vector2 currentInput = new Vector2(_shootingJoystick.Horizontal, _shootingJoystick.Vertical);
        float inputMagnitude = currentInput.magnitude;

        // Если прицеливание завершилось недавно, а джойстик находится в нейтральной позиции
        if (inputMagnitude < _minShootThreshold)
        {
            // Проверяем, прошло ли время перезарядки
            if (Time.time - _lastShootTime >= _currentWeapon.shootingCooldown)
            {
                // Всегда стреляем после прицеливания, если оно было достаточным
                Shoot(1.0f); // Используем полную силу для гарантии выстрела
            }
        }
    }

    // Обновление визуализатора прицеливания
    private void UpdateAimingVisualizer(float inputMagnitude)
    {
        if (_currentWeapon.isGrenade)
        {
            float throwDistance = GetGrenadeThrowDistance(inputMagnitude);
            _grenadeAimingVisualizer?.UpdateAiming(
                _lastAimDirection,
                true,
                throwDistance
            );
        }
        else
        {
            _aimingVisualizer?.UpdateAiming(
                _lastAimDirection,
                true,
                _currentWeapon.range,
                _currentWeapon.isShotgun,
                _currentWeapon.spreadAngle
            );
        }
    }

    // Обработка поворота персонажа
    private void HandleRotation()
    {
        if (_inShot)
        {
            Quaternion shootDirection = Quaternion.LookRotation(_lastAimDirection);
            transform.rotation = shootDirection;
            _targetRotation = shootDirection;
        }

    }

    // Получить дистанцию броска гранаты
    private float GetGrenadeThrowDistance(float inputMagnitude)
    {
        float normalizedMagnitude = Mathf.Clamp01(inputMagnitude);
        return Mathf.Lerp(
            _currentWeapon.minThrowDistance,
            _currentWeapon.maxThrowDistance,
            normalizedMagnitude
        );
    }

    // Выполнение выстрела
    private void Shoot(float inputMagnitude)
    {
        // Запускаем анимацию атаки
        _animator?.SetTrigger("Attack");

        // Мгновенно устанавливаем правильное направление
         Quaternion shootDirection = Quaternion.LookRotation(_lastAimDirection);
        transform.rotation = shootDirection;
         _targetRotation = shootDirection;

        // Устанавливаем флаг удержания ротации и начинаем стабилизацию
        _shouldMaintainRotation = true;

        // Выбираем тип стрельбы по оружию
        if (_currentWeapon.isGrenade)
        {
            float throwDistance = GetGrenadeThrowDistance(inputMagnitude);
            FireGrenade(throwDistance);
        }
        else if (_currentWeapon.isAutomatic)
        {
            FireBurst();
        }
        else if (_currentWeapon.isShotgun)
        {
            FireShotgun();
        }
        else if (_currentWeapon.isExplosive)
        {
            FireRocket();
        }
        else
        {
            FireSingle(false);
        }

        // Создаем эффекты выстрела
        CreateShootEffects();

        // Обновляем время последнего выстрела
        _lastShootTime = Time.time;
    }

    // Стрельба одиночным снарядом
    private void FireSingle(bool burst)
    {
        // Смещаем точку появления снаряда немного вперед, чтобы избежать коллизий
        Vector3 spawnPosition = _shootingPoint.position + _lastAimDirection * 0.3f;
        
        GameObject projectile = Instantiate(
            _currentWeapon.projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(_lastAimDirection)
        );

        if (projectile.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.useGravity = false;
            rb.linearVelocity = _lastAimDirection * _currentWeapon.projectileSpeed;
        }

        if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
        {
            // Передаем gameObject как владельца снаряда
            projectileComponent.Initialize(_currentWeapon, gameObject);
        }

        if (!burst)
        {
            _inShot = false;
        }
    }

    // Стрельба дробовиком
    private void FireShotgun()
    {
        for (int i = 0; i < _currentWeapon.pelletCount; i++)
        {
            float angle = Random.Range(-_currentWeapon.spreadAngle, _currentWeapon.spreadAngle);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 direction = rotation * _lastAimDirection;

            // Создаем снаряд на некотором расстоянии от точки стрельбы, чтобы избежать мгновенных коллизий
            Vector3 spawnPosition = _shootingPoint.position + direction * 0.5f;
            
            GameObject projectile = Instantiate(
                _currentWeapon.projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(direction)
            );

            if (projectile.TryGetComponent<Rigidbody>(out var rb))
            {
                // Для дробовика используем низкую гравитацию, а не полное её включение
                rb.useGravity = false;
                rb.linearVelocity = direction * _currentWeapon.projectileSpeed;
            }

            // Проверяем и отключаем все коллайдеры снаряда на короткое время
            Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
            foreach (Collider col in projectileColliders)
            {
                col.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(col, 0.1f));
            }

            if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
            {
                // Важно: передаем gameObject как владельца снаряда
                projectileComponent.Initialize(_currentWeapon, gameObject);
            }
        }
        _inShot = false;
    }

    // Вспомогательный метод для включения коллайдера с задержкой
    private System.Collections.IEnumerator EnableColliderAfterDelay(Collider collider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (collider != null)
        {
            collider.enabled = true;
        }
    }

    // Выстрел ракетой
    private void FireRocket()
    {
        // Смещаем точку появления ракеты немного вперед, чтобы избежать коллизий
        Vector3 spawnPosition = _shootingPoint.position + _lastAimDirection * 0.3f;
        
        GameObject rocket = Instantiate(
            _currentWeapon.projectilePrefab,
            spawnPosition,
            Quaternion.LookRotation(_lastAimDirection)
        );

        if (rocket.TryGetComponent<Rocket>(out var rocketComponent))
        {
            // Для ракеты передаем gameObject как владельца
            rocketComponent.Initialize(_currentWeapon, _lastAimDirection, _currentWeapon.range, gameObject);
        }
        else if (rocket.TryGetComponent<Projectile>(out var projectileComponent))
        {
            // Если используется обычный Projectile вместо Rocket
            projectileComponent.Initialize(_currentWeapon, gameObject);
        }
        else
        {
            Debug.LogError("Rocket component not found on prefab!");
            Destroy(rocket);
        }
        _inShot = false;
    }

    // Бросок гранаты
    private void FireGrenade(float throwDistance)
    {
        GameObject grenade = Instantiate(
            _currentWeapon.projectilePrefab,
            _shootingPoint.position,
            Quaternion.identity
        );

        if (grenade.TryGetComponent<Grenade>(out var grenadeComponent))
        {
            // Важно: передаем gameObject как владельца гранаты
            grenadeComponent.Initialize(_currentWeapon, _lastAimDirection, throwDistance, gameObject);
        }
        else
        {
            Debug.LogError("Grenade component not found on prefab!");
            Destroy(grenade);
        }
        _inShot = false;
    }

    // Автоматическая стрельба очередью
    private void FireBurst()
    {
        // Создаем корутину для стрельбы очередью
        StartCoroutine(FireBurstCoroutine());
    }

    // Корутина для стрельбы очередью
    private System.Collections.IEnumerator FireBurstCoroutine()
    {
        int shotsRemaining = _currentWeapon.burstSize;

        while (shotsRemaining > 0)
        {
            // Стреляем один снаряд
            FireSingle(true);

            // Создаем эффекты выстрела для каждого снаряда в очереди
            CreateShootEffects();

            // Уменьшаем количество оставшихся выстрелов
            shotsRemaining--;

            // Ожидаем заданное время между выстрелами
            if (shotsRemaining > 0)
            {
                yield return new WaitForSeconds(_currentWeapon.fireRate);
            }
        }

        _inShot = false;
    }

    // Создание эффектов выстрела
    private void CreateShootEffects()
    {
        // Визуальный эффект
        if (_currentWeapon.shootEffect != null)
        {
            GameObject effect = Instantiate(
                _currentWeapon.shootEffect,
                _shootingPoint.position,
                Quaternion.LookRotation(_lastAimDirection)
            );
            Destroy(effect, 1f);
        }

        // Звуковой эффект
        if (_currentWeapon.shootSound != null)
        {
            AudioSource.PlayClipAtPoint(_currentWeapon.shootSound, _shootingPoint.position);
        }
    }

    // Обновление пользовательского интерфейса
    private void UpdateUI()
    {
        if (_cooldownIndicator != null)
        {
            float cooldownProgress = (Time.time - _lastShootTime) / _currentWeapon.shootingCooldown;
            _cooldownIndicator.fillAmount = Mathf.Clamp01(cooldownProgress);
        }
    }

    // Экипировка оружия
    public void EquipWeapon(WeaponData weaponData)
    {
        if (weaponData == null)
        {
            Debug.LogError("Попытка экипировать null оружие!");
            return;
        }

        _currentWeapon = weaponData;

        // Обновляем визуализаторы
        if (_grenadeAimingVisualizer != null)
        {
            _grenadeAimingVisualizer.SetWeaponData(_currentWeapon);
        }

        // Обновляем UI
        if (_weaponIcon != null)
        {
            _weaponIcon.sprite = weaponData.weaponIcon;
            _weaponIcon.gameObject.SetActive(true);
        }

        if (_cooldownIndicator != null)
        {
            _cooldownIndicator.gameObject.SetActive(true);
        }
    }

    // Установка оружия извне
    public void SetWeapon(WeaponData weaponData)
    {
        EquipWeapon(weaponData);
    }

    // API для PlayerController - проверка блокировки поворота
    public bool IsRotationLockedForShooting()
    {
        return _shouldMaintainRotation || _isAiming;
    }

    // API для PlayerController - запрос на разблокировку поворота
    public void RequestRotationUnlock()
    {
        _shouldMaintainRotation = false;
    }

    // API для получения текущего направления стрельбы
    public Vector3 GetShootingDirection()
    {
        return _lastAimDirection;
    }
}