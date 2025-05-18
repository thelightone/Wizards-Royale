using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Projectile : MonoBehaviour
{
    private WeaponData _weaponData;
    private List<IDamageable> _hitTargets = new List<IDamageable>();
    private Vector3 _startPosition;
    private float _distanceTraveled;
    private GameObject _owner; // Объект, выпустивший снаряд
    private int _ownerLayer; // Слой владельца снаряда
    private bool _collisionsEnabled = true;
    private float _activationDelay = 0.1f; // Задержка активации коллизий

    public void Initialize(WeaponData weaponData)
    {
        _weaponData = weaponData;
        _startPosition = transform.position;
        _distanceTraveled = 0f;

        // Для всех снарядов гарантированно отключаем коллизии на короткий период
        DisableCollisions();
        StartCoroutine(EnableCollisionsAfterDelay(_activationDelay));

        Destroy(gameObject, 5f);
    }

    // Расширенная инициализация с владельцем снаряда
    public void Initialize(WeaponData weaponData, GameObject owner)
    {
        Initialize(weaponData);
        _owner = owner;
        _ownerLayer = owner.layer;

        // Увеличиваем задержку для снарядов, выпущенных врагами
        if (_owner.CompareTag("Enemy"))
        {
            // Если это еще и взрывной или дробовой снаряд, увеличиваем задержку
            if (_weaponData.isExplosive || _weaponData.isShotgun)
            {
                // Гарантированно выключаем коллизии
                DisableCollisions();
                // Увеличиваем задержку активации для снарядов врага
                StartCoroutine(EnableCollisionsAfterDelay(0.1f));
            }
        }

        // Игнорируем коллизии с владельцем
        Collider projectileCollider = GetComponent<Collider>();
        if (projectileCollider != null && owner != null)
        {
            Collider[] ownerColliders = owner.GetComponentsInChildren<Collider>();
            foreach (Collider ownerCollider in ownerColliders)
            {
                Physics.IgnoreCollision(projectileCollider, ownerCollider);
            }

            // Если снаряд от игрока, проверяем метку игрока
            if (owner.CompareTag("Player"))
            {
                // Игнорируем коллизии со всеми объектами игрока
                GameObject[] playerObjects = GameObject.FindGameObjectsWithTag("Player");
                foreach (GameObject playerObj in playerObjects)
                {
                    if (playerObj == owner) continue; // Уже обработано выше

                    Collider[] playerColliders = playerObj.GetComponentsInChildren<Collider>();
                    foreach (Collider playerCollider in playerColliders)
                    {
                        Physics.IgnoreCollision(projectileCollider, playerCollider);
                    }
                }
            }
            // Если снаряд от врага, игнорируем коллизии со всеми врагами
            //else if (owner.CompareTag("Enemy"))
            //{
            //    // Игнорируем коллизии со всеми врагами
            //    GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            //    foreach (GameObject enemy in enemies)
            //    {
            //        Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
            //        foreach (Collider enemyCollider in enemyColliders)
            //        {
            //            Physics.IgnoreCollision(projectileCollider, enemyCollider);
            //        }
            //    }
            //}
        }
    }

    // Временно отключаем коллизии
    private void DisableCollisions()
    {
        _collisionsEnabled = false;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = false;
        }
    }

    // Включаем коллизии через заданное время
    private IEnumerator EnableCollisionsAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        _collisionsEnabled = true;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (Collider col in colliders)
        {
            col.enabled = true;
        }
    }

    private void Update()
    {
        // Проверяем пройденное расстояние
        _distanceTraveled = Vector3.Distance(_startPosition, transform.position);

        // Если снаряд превысил дальность, уничтожаем его
        if (_distanceTraveled >= _weaponData.range)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Если коллизии ещё не активированы, пропускаем
        if (!_collisionsEnabled) return;

        //// Проверяем, не принадлежит ли объект тому же слою, что и владелец
        //if (_owner != null
        //    && collision.gameObject.layer == _ownerLayer)
        //{
        //    // Пропускаем столкновения с объектами того же слоя
        //    return;
        //}

        if (collision.gameObject.TryGetComponent<Player>(out Player player))
        {
            if (_owner.GetComponent<Player>().team == player.team)
            {
                return;
            }
        }

        // Проверяем, не является ли объект владельцем или его дочерним объектом
        if (_owner != null)
        {
            // Проверка на прямое попадание во владельца
            if (collision.gameObject == _owner)
            {
                return;
            }

            // Проверка на попадание в дочерние объекты владельца
            Transform current = collision.transform;
            while (current != null)
            {
                if (current.gameObject == _owner)
                {
                    return; // Пропускаем столкновение с владельцем
                }
                current = current.parent;
            }
        }

        // Проверяем, это враг или игрок
        bool isEnemyProjectile = _owner != null && _owner.CompareTag("Enemy");
        bool isPlayerProjectile = _owner != null && _owner.CompareTag("Player");

        // Если снаряд от игрока, он не должен наносить урон игроку
        if (isPlayerProjectile && collision.gameObject.CompareTag("Player"))
        {
            return;
        }



        // Проверяем, можем ли нанести урон цели
        IDamageable damageable = collision.gameObject.GetComponent<IDamageable>();
        if (damageable != null && !_hitTargets.Contains(damageable))
        {
            _hitTargets.Add(damageable);
            damageable.TakeDamage(_weaponData.damage);

            // Применяем эффект замедления если есть
            if (_weaponData.slowEffect > 0)
            {
                IMovable movable = collision.gameObject.GetComponent<IMovable>();
                if (movable != null)
                {
                    movable.ApplySlowEffect(_weaponData.slowEffect, _weaponData.slowDuration);
                }
            }
        }

        // Создаем эффект попадания
        if (_weaponData.hitEffect != null)
        {
            var hitEffect = Instantiate(_weaponData.hitEffect, transform.position, Quaternion.identity);
            Destroy(hitEffect.gameObject, 1f);
        }

        // Проверяем взрывной эффект
        if (_weaponData.isExplosive)
        {
            Explode();
        }

        // Уничтожаем снаряд если он не проникающий
        if (!_weaponData.isPiercing)
        {
            Destroy(gameObject);
        }
    }

    private void Explode()
    {
        // Находим все объекты в радиусе взрыва
        Collider[] colliders = Physics.OverlapSphere(transform.position, _weaponData.explosionRadius);
        foreach (Collider collider in colliders)
        {
            // Пропускаем владельца и объекты того же типа
            if (_owner != null)
            {
                // Пропускаем самого владельца
                if (collider.gameObject == _owner)
                {
                    continue;
                }

                // Проверяем, не принадлежит ли объект владельцу
                Transform current = collider.transform;
                bool isOwnerChild = false;
                while (current != null)
                {
                    if (current.gameObject == _owner)
                    {
                        isOwnerChild = true;
                        break;
                    }
                    current = current.parent;
                }
                if (isOwnerChild)
                {
                    continue;
                }


                // Если снаряд от игрока, он не должен наносить урон игроку
                if (_owner.CompareTag("Player") && collider.gameObject.CompareTag("Player"))
                {
                    continue;
                }
            }

            // Наносим урон всем остальным объектам в радиусе
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null && !_hitTargets.Contains(damageable))
            {
                _hitTargets.Add(damageable);

                // Вычисляем урон в зависимости от расстояния
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                float damagePercent = 1f - Mathf.Clamp01(distance / _weaponData.explosionRadius);
                float damage = _weaponData.damage * damagePercent;

                damageable.TakeDamage(damage);

                // Применяем эффект замедления
                if (_weaponData.slowEffect > 0)
                {
                    IMovable movable = collider.GetComponent<IMovable>();
                    if (movable != null)
                    {
                        movable.ApplySlowEffect(_weaponData.slowEffect, _weaponData.slowDuration);
                    }
                }
            }
        }

        // Создаем эффект взрыва
        if (_weaponData.explosionEffect != null)
        {
            var effect = Instantiate(_weaponData.explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f);
        }

        // Воспроизводим звук взрыва
        if (_weaponData.explosionSound != null)
        {
            AudioSource.PlayClipAtPoint(_weaponData.explosionSound, transform.position);
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        if (_weaponData != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, _weaponData.explosionRadius);
        }
    }
}