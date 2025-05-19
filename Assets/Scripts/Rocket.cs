using UnityEngine;

public class Rocket : MonoBehaviour
{
    private WeaponData _weaponData;
    private bool _hasExploded;
    private Vector3 _direction;
    private float _speed;
    private float _distance;
    private float _distanceTraveled;
    private Vector3 _startPosition;
    private GameObject _owner;

    public void Initialize(WeaponData weaponData, Vector3 direction, float distance, GameObject owner)
    {
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is null!");
            return;
        }

        _owner = owner;

        _weaponData = weaponData;
        _direction = direction;
        _speed = weaponData.projectileSpeed;
        _distance = distance;
        _startPosition = transform.position;
        _hasExploded = false;
        _distanceTraveled = 0f;

        // Поворачиваем ракету в направлении полета
        transform.rotation = Quaternion.LookRotation(direction);
    }

    private void Update()
    {
        if (_hasExploded) return;

        // Двигаем ракету вперед
        float moveDistance = _speed * Time.deltaTime;
        transform.position += _direction * moveDistance;
        _distanceTraveled += moveDistance;

        // Проверяем, не превысили ли максимальную дистанцию
        if (_distanceTraveled >= _distance)
        {
            Explode();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {

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

        if (!_hasExploded)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (_hasExploded) return;
        _hasExploded = true;

        // Находим все объекты в радиусе взрыва
        Collider[] colliders = Physics.OverlapSphere(transform.position, _weaponData.explosionRadius);
        foreach (Collider collider in colliders)
        {
            // Пропускаем самого владельца
            if (collider.gameObject == _owner)
            {
                continue;
            }
            
            // Проверяем, не является ли объект дочерним объектом владельца
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
            
            // Проверка на командную принадлежность, если есть компонент Player
            if (_owner != null && _owner.TryGetComponent<Player>(out Player ownerPlayer))
            {
                if (collider.gameObject.TryGetComponent<Player>(out Player targetPlayer))
                {
                    // Если они в одной команде, пропускаем
                    if (ownerPlayer.team == targetPlayer.team)
                    {
                        continue;
                    }
                }
            }

            // Наносим урон всем объектам в радиусе
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Наносим полный урон всем целям в радиусе взрыва
                damageable.TakeDamage(_weaponData.damage);
                Debug.Log($"Нанесен урон {_weaponData.damage} объекту {collider.gameObject.name}");
            }
        }

        // Создаем эффект взрыва
        if (_weaponData.explosionEffect != null)
        {
            GameObject effect = Instantiate(_weaponData.explosionEffect, transform.position, Quaternion.identity);
            Destroy(effect, 1f); // Уничтожаем эффект через секунду
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