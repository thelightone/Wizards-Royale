using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    private Vector3 _direction;
    private float _speed;
    private float _distanceTraveled;
    private Vector3 _startPosition;
    private float _maxDistance = 100f; // Максимальная дистанция полета
    private WeaponData _weaponData;

    public void Initialize(Vector3 direction, float speed, WeaponData weaponData)
    {
        _direction = direction;
        _speed = speed;
        _weaponData = weaponData;
        _startPosition = transform.position;
        _distanceTraveled = 0f;

        // Добавляем Rigidbody для физических взаимодействий
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }
        rb.useGravity = false;
        rb.isKinematic = true; // Делаем кинематическим, чтобы не реагировать на физику, но при этом обнаруживать столкновения

        // Добавляем коллайдер, если его нет
        if (GetComponent<Collider>() == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.radius = 0.1f; // Небольшой радиус для точного попадания
            collider.isTrigger = false; // Делаем обычным коллайдером для взаимодействия с физикой
        }
    }

    private void Update()
    {
        // Двигаем снаряд вперед
        float moveDistance = _speed * Time.deltaTime;
        transform.position += _direction * moveDistance;
        _distanceTraveled += moveDistance;

        // Уничтожаем снаряд, если он пролетел слишком далеко
        if (_distanceTraveled >= _maxDistance)
        {
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other.gameObject);
    }

    private void HandleCollision(GameObject other)
    {
        Debug.Log("Collision with "+other.name);
        // Проверяем, есть ли у объекта компонент для получения урона
        IDamageable damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            // Наносим урон
            damageable.TakeDamage(_weaponData.damage);

            // Создаем эффект попадания
            if (_weaponData.hitEffect != null)
            {
                GameObject effect = Instantiate(_weaponData.hitEffect, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }
        else
        {
            // Если попали в препятствие, создаем эффект попадания
            if (_weaponData.hitEffect != null)
            {
                GameObject effect = Instantiate(_weaponData.hitEffect, transform.position, Quaternion.identity);
                Destroy(effect, 1f);
            }
        }

        // Уничтожаем снаряд при любом столкновении
        Destroy(gameObject);
    }
} 