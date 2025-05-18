using TMPro;
using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    private WeaponData _currentWeapon;
    private Animator _animator;
    private Vector3 _lastAimDirection;

    public Transform _shootingPoint;

    public void SetWeaponAndAnimator(WeaponData weaponData, Animator animator)
    {
        _currentWeapon = weaponData;
        _animator = animator;
    }

    public void Shoot(Vector3 _aim)
    {
        _lastAimDirection = (_aim - _shootingPoint.position).normalized;

        // Запускаем анимацию атаки
        _animator?.SetTrigger("Attack");

        // Проверяем тип оружия из данных
        if (_currentWeapon.isGrenade)
        {
            float throwDistance = GetGrenadeThrowDistance(Vector3.Distance(transform.position,_aim));
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

    }

    private float GetGrenadeThrowDistance(float inputMagnitude)
    {
        float normalizedMagnitude = Mathf.Clamp01(inputMagnitude);
        return Mathf.Lerp(
            _currentWeapon.minThrowDistance,
            _currentWeapon.maxThrowDistance,
            normalizedMagnitude
        );
    }

    private void FireSingle(bool burst)
    {
        GameObject projectile = Instantiate(
            _currentWeapon.projectilePrefab,
            _shootingPoint.position,
            Quaternion.identity
        );

        if (projectile.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.useGravity = false;
            rb.linearVelocity = _lastAimDirection * _currentWeapon.projectileSpeed;
        }

        if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
        {
            // Передаем владельца снаряда
            projectileComponent.Initialize(_currentWeapon, gameObject);
        }
    }

    // Выстрел дробовика
    private void FireShotgun()
    {
        for (int i = 0; i < _currentWeapon.pelletCount; i++)
        {
            float angle = Random.Range(-_currentWeapon.spreadAngle, _currentWeapon.spreadAngle);
            Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
            Vector3 direction = rotation * _lastAimDirection;

            // Увеличиваем расстояние создания пули от врага
            Vector3 spawnPosition = _shootingPoint.position + direction * 1.0f;

            GameObject projectile = Instantiate(
                _currentWeapon.projectilePrefab,
                spawnPosition,
                Quaternion.LookRotation(direction)
            );

            if (projectile.TryGetComponent<Rigidbody>(out var rb))
            {
                // Для дробовика лучше отключить гравитацию для более предсказуемых выстрелов
                rb.useGravity = false;
                rb.linearVelocity = direction * _currentWeapon.projectileSpeed;
            }

            // Отключаем все коллайдеры снаряда на короткое время
            Collider[] projectileColliders = projectile.GetComponentsInChildren<Collider>();
            foreach (Collider col in projectileColliders)
            {
                col.enabled = false;
                StartCoroutine(EnableColliderAfterDelay(col, 0.2f));
            }

            if (projectile.TryGetComponent<Projectile>(out var projectileComponent))
            {
                // Передаем владельца снаряда
                projectileComponent.Initialize(_currentWeapon, gameObject);
            }
        }
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

    // Запуск ракеты
    private void FireRocket()
    {
        GameObject rocket = Instantiate(
            _currentWeapon.projectilePrefab,
            _shootingPoint.position,
            Quaternion.LookRotation(_lastAimDirection)
        );

        if (rocket.TryGetComponent<Rocket>(out var rocketComponent))
        {
            // Обновляем инициализацию ракеты, чтобы передать владельца
            rocketComponent.Initialize(_currentWeapon, _lastAimDirection, _currentWeapon.range, gameObject);
        }
        else if (rocket.TryGetComponent<Projectile>(out var projectileComponent))
        {
            // Если ракеты нет, используем обычный снаряд
            projectileComponent.Initialize(_currentWeapon, gameObject);
        }
        else
        {
            Debug.LogError("Rocket component not found on prefab!");
            Destroy(rocket);
        }
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
            // Обновляем инициализацию гранаты, чтобы передать владельца
            grenadeComponent.Initialize(_currentWeapon, _lastAimDirection, throwDistance, gameObject);
        }
        else if (grenade.TryGetComponent<Projectile>(out var projectileComponent))
        {
            // Если гранаты нет, используем обычный снаряд
            projectileComponent.Initialize(_currentWeapon, gameObject);
        }
        else
        {
            Debug.LogError("Grenade component not found on prefab!");
            Destroy(grenade);
        }

    }

    // Автоматический режим стрельбы
    private void FireBurst()
    {
        // Запускаем корутину для очереди выстрелов
        StartCoroutine(FireBurstCoroutine());
    }

    // Корутина для очереди выстрелов
    private System.Collections.IEnumerator FireBurstCoroutine()
    {
        int shotsRemaining = _currentWeapon.burstSize;

        while (shotsRemaining > 0)
        {
            // Производим один выстрел
            FireSingle(true);

            // Создаем эффекты выстрела при каждом выстреле в очереди
            CreateShootEffects();

            // Уменьшаем количество оставшихся выстрелов
            shotsRemaining--;

            // Ожидаем паузу между очередными выстрелами
            if (shotsRemaining > 0)
            {
                yield return new WaitForSeconds(_currentWeapon.fireRate);
            }
        }
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
}

