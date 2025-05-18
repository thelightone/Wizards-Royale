using UnityEngine;
using System.Collections;

public class Grenade : MonoBehaviour
{
    private WeaponData _weaponData;
    private bool _hasExploded;
    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _throwDuration;
    private float _currentTime;
    private float _arcHeight;
    private bool _isMoving;
    private GameObject _owner; // Владелец гранаты
    
    // Для игрока нужна позиция цели
    private static Vector3 _lastPlayerAimPosition;
    
    // Публичный метод для установки цели игрока
    public static void SetPlayerAimPosition(Vector3 aimPosition)
    {
        _lastPlayerAimPosition = aimPosition;
        Debug.Log($"Установлена точка прицеливания игрока: {_lastPlayerAimPosition}");
    }

    private void Awake()
    {
        // Удаляем Rigidbody, так как он больше не нужен
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Destroy(rb);
        }
    }

    public void Initialize(WeaponData weaponData, Vector3 throwDirection, float throwDistance, GameObject owner)
    {
        if (weaponData == null)
        {
            Debug.LogError("WeaponData is null!");
            return;
        }

        _weaponData = weaponData;
        _hasExploded = false;
        _owner = owner;
        
        // Сохраняем начальную позицию
        _startPosition = transform.position;
        
        // 🔴 ОПРЕДЕЛЕНИЕ ЦЕЛЕВОЙ ТОЧКИ 🔴
        if (_owner != null)
        {
            if (_owner.CompareTag("Enemy"))
            {
                // ====== ЛОГИКА ДЛЯ ВРАГА ======
                PlayerController player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    // Вектор от врага к игроку
                    Vector3 dirToPlayer = (player.transform.position - _owner.transform.position).normalized;
                    dirToPlayer.y = 0; // Убираем вертикальную составляющую
                    
                    // Вычисляем дистанцию до игрока
                    float distToPlayer = Vector3.Distance(_owner.transform.position, player.transform.position);
                    
                    // Добавляем небольшое предсказание движения игрока
                    Vector3 targetLeadPosition = player.transform.position;
                    if (player.IsMoving())
                    {
                        // Получаем направление движения игрока
                        Rigidbody playerRb = player.GetComponent<Rigidbody>();
                        Vector3 playerVelocity = playerRb != null ? playerRb.linearVelocity.normalized : Vector3.zero;
                        
                        // Если не удалось получить скорость через Rigidbody, используем предположительное направление
                        if (playerVelocity.magnitude < 0.1f)
                        {
                            // Используем направление, в котором смотрит игрок
                            playerVelocity = player.transform.forward.normalized;
                        }
                        
                        targetLeadPosition += playerVelocity * 1.5f; // Предугадываем перемещение
                    }
                    
                    // Определяем конечную позицию - либо точно позиция игрока, либо максимальная дальность
                    if (distToPlayer <= throwDistance)
                    {
                        // Игрок в пределах досягаемости - целимся прямо в него
                        _targetPosition = targetLeadPosition;
                        Debug.Log($"Граната врага: цель в пределах досягаемости! {_targetPosition}");
                    }
                    else
                    {
                        // Игрок слишком далеко - бросаем на максимальную дистанцию в его направлении
                        _targetPosition = _owner.transform.position + dirToPlayer * throwDistance;
                        Debug.Log($"Граната врага: цель слишком далеко! Максимальная дистанция: {_targetPosition}");
                    }
                }
                else
                {
                    // Если игрок не найден, используем стандартную логику
                    _targetPosition = _startPosition + throwDirection * throwDistance;
                    Debug.Log($"Граната врага: игрок не найден, используем стандартное направление");
                }
            }
            else if (_owner.CompareTag("Player"))
            {
                // ====== ЛОГИКА ДЛЯ ИГРОКА ======
                // Проверяем, есть ли сохраненная точка прицеливания
                if (_lastPlayerAimPosition != Vector3.zero)
                {
                    // Используем последнюю точку прицеливания игрока
                    Vector3 dirToTarget = (_lastPlayerAimPosition - _startPosition).normalized;
                    float distToTarget = Vector3.Distance(_startPosition, _lastPlayerAimPosition);
                    
                    // Если точка прицеливания в пределах досягаемости
                    if (distToTarget <= throwDistance)
                    {
                        _targetPosition = _lastPlayerAimPosition;
                        Debug.Log($"Граната игрока: цель в пределах досягаемости! {_targetPosition}");
                    }
                    else
                    {
                        // Цель слишком далеко - бросаем на максимальную дистанцию в этом направлении
                        _targetPosition = _startPosition + dirToTarget * throwDistance;
                        Debug.Log($"Граната игрока: цель слишком далеко! Максимальная дистанция: {_targetPosition}");
                    }
                    
                    // Сбрасываем точку прицеливания после использования
                    _lastPlayerAimPosition = Vector3.zero;
                }
                else
                {
                    // Если нет точки прицеливания, используем переданное направление
                    _targetPosition = _startPosition + throwDirection * throwDistance;
                    Debug.Log($"Граната игрока: используем переданное направление, нет сохраненной цели");
                }
            }
            else
            {
                // Стандартная логика для других объектов
                _targetPosition = _startPosition + throwDirection * throwDistance;
                Debug.Log($"Граната: стандартное поведение для объекта {_owner.name}");
            }
        }
        else
        {
            // Стандартная логика, если нет владельца
            _targetPosition = _startPosition + throwDirection * throwDistance;
            Debug.Log($"Граната: стандартное поведение (нет владельца)");
        }
        
        // Настраиваем параметры траектории в зависимости от дистанции
        float flightDistance = Vector3.Distance(_startPosition, _targetPosition);
        _arcHeight = CalculateArcHeight(flightDistance);
        _throwDuration = CalculateFlightDuration(flightDistance);
        _currentTime = 0f;
        _isMoving = true;
        
        // Игнорируем коллизии с владельцем
        IgnoreCollisionsWithOwner();
        
        Debug.Log($"Граната инициализирована: старт={_startPosition}, цель={_targetPosition}, владелец={_owner?.name}, дальность={flightDistance:F2}м, высота={_arcHeight:F2}м, время={_throwDuration:F2}с");
    }
    
    // Отдельный метод для настройки игнорирования коллизий с владельцем
    private void IgnoreCollisionsWithOwner()
    {
        if (_owner == null) return;
        
        Collider grenadeCollider = GetComponent<Collider>();
        if (grenadeCollider == null) return;
        
        // Игнорируем коллизии с владельцем
        Collider[] ownerColliders = _owner.GetComponentsInChildren<Collider>();
        foreach (Collider ownerCollider in ownerColliders)
        {
            Physics.IgnoreCollision(grenadeCollider, ownerCollider);
        }
        
        // Если владелец враг, игнорируем коллизии со всеми врагами
        if (_owner.CompareTag("Enemy"))
        {
            // Находим всех врагов на сцене
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            foreach (GameObject enemy in enemies)
            {
                // Не игнорируем коллизии с самим собой (хотя мы уже это сделали выше)
                if (enemy == _owner) continue;
                
                Collider[] enemyColliders = enemy.GetComponentsInChildren<Collider>();
                foreach (Collider enemyCollider in enemyColliders)
                {
                    Physics.IgnoreCollision(grenadeCollider, enemyCollider);
                }
            }
        }
    }
    
    // Рассчитывает высоту дуги в зависимости от дистанции полета
    private float CalculateArcHeight(float distance)
    {
        // Короткие броски - очень низкая дуга (почти прямая линия)
        if (distance < 2f)
        {
            return Mathf.Lerp(0.2f, 0.5f, distance / 2f);
        }
        // Короткие броски - низкая дуга
        else if (distance < 5f)
        {
            return Mathf.Lerp(0.5f, 2f, (distance - 2f) / 3f);
        }
        // Средние броски - стандартная дуга
        else if (distance < 12f)
        {
            return 2f;
        }
        // Дальние броски - более высокая дуга
        else
        {
            return Mathf.Lerp(2f, 4.5f, (distance - 12f) / 8f);
        }
    }
    
    // Рассчитывает длительность полета в зависимости от дистанции
    private float CalculateFlightDuration(float distance)
    {
        // Короткие дистанции - быстрый полет
        if (distance < 3f)
        {
            return Mathf.Lerp(0.3f, 0.7f, distance / 3f);
        }
        // Средние дистанции - стандартная скорость
        else if (distance < 10f)
        {
            return 0.7f + Mathf.Sqrt(distance - 3f) * 0.12f;
        }
        // Дальние дистанции - более длительный полет
        else
        {
            return 1.1f + Mathf.Sqrt(distance - 10f) * 0.18f;
        }
    }

    private void Update()
    {
        if (!_isMoving || _hasExploded) return;

        _currentTime += Time.deltaTime;
        float normalizedTime = _currentTime / _throwDuration;

        if (normalizedTime >= 1f)
        {
            // Достигли конечной точки
            transform.position = _targetPosition;
            _isMoving = false;
            Explode(); // Взрываемся при достижении конечной точки
            return;
        }

        // Вычисляем текущую позицию по параболической траектории
        Vector3 currentPos = Vector3.Lerp(_startPosition, _targetPosition, normalizedTime);
        float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * _arcHeight;
        currentPos.y += heightOffset;

        transform.position = currentPos;
        
        // Добавляем вращение для реалистичности
        transform.Rotate(Time.deltaTime * 180f, Time.deltaTime * 90f, 0);
        
        // Визуализируем траекторию для отладки
        Debug.DrawLine(_startPosition, _targetPosition, Color.yellow);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Проверяем, не является ли объект владельцем или объектом того же типа
        if (_owner != null)
        {
            // Если граната от игрока, она не должна наносить урон игроку
            if (_owner.CompareTag("Player") && collision.gameObject.CompareTag("Player"))
            {
                return;
            }
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
        
        Debug.Log($"Граната взорвалась в точке {transform.position}");

        // Находим все объекты в радиусе взрыва
        Collider[] colliders = Physics.OverlapSphere(transform.position, _weaponData.explosionRadius);
        
        // Создаем физический взрыв
        Vector3 explosionPos = transform.position;
        foreach (Collider hit in Physics.OverlapSphere(explosionPos, _weaponData.explosionRadius))
        {
            Rigidbody rb = hit.GetComponent<Rigidbody>();
            
            if (rb != null)
            {
                // Не применяем силу к игроку и врагам
                if (hit.CompareTag("Player") || hit.CompareTag("Enemy"))
                {
                    continue;
                }
                
                // Применяем физическое воздействие к объектам
                rb.AddExplosionForce(_weaponData.damage * 20f, explosionPos, _weaponData.explosionRadius, 
                    1.0f, ForceMode.Impulse);
            }
        }
        
        // Наносим урон всем объектам в радиусе
        foreach (Collider collider in colliders)
        {
            // Проверяем, не является ли объект владельцем или объектом того же типа
            if (_owner != null)
            {
                // Если граната от врага, она не должна наносить урон другим врагам
                if (_owner.CompareTag("Enemy") && collider.gameObject.CompareTag("Enemy"))
                {
                    continue;
                }
                
                // Если граната от игрока, она не должна наносить урон игроку
                if (_owner.CompareTag("Player") && collider.gameObject.CompareTag("Player"))
                {
                    continue;
                }
            }

            // Наносим урон всем объектам в радиусе
            IDamageable damageable = collider.GetComponent<IDamageable>();
            if (damageable != null)
            {
                // Вычисляем урон в зависимости от расстояния
                float distance = Vector3.Distance(transform.position, collider.transform.position);
                
                // Улучшенная формула урона с более сильным центральным поражением
                float damagePercent;
                if (distance < _weaponData.explosionRadius * 0.3f)
                {
                    // В центре взрыва - полный урон
                    damagePercent = 1.0f;
                }
                else
                {
                    // Дальше от центра - спадающий урон по квадратичной формуле
                    float normalizedDistance = (distance - _weaponData.explosionRadius * 0.3f) / 
                                               (_weaponData.explosionRadius * 0.7f);
                    damagePercent = 1.0f - normalizedDistance * normalizedDistance;
                }
                
                float damage = _weaponData.damage * damagePercent;
                
                damageable.TakeDamage(damage);
                Debug.Log($"Нанесен урон {damage:F1} объекту {collider.gameObject.name} (дист: {distance:F2}, %: {damagePercent:P0})");
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

    private void OnDrawGizmos()
    {
        // Отображаем информацию только в режиме игры
        if (!Application.isPlaying) return;
        
        // Радиус взрыва
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _weaponData?.explosionRadius ?? 1f);
        
        // Путь полета
        if (_targetPosition != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_targetPosition, 0.3f);
            Gizmos.DrawLine(_startPosition, _targetPosition);
            
            // Текущая траектория
            float currentHeight = Mathf.Sin((_currentTime / _throwDuration) * Mathf.PI) * _arcHeight;
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, Vector3.up * currentHeight);
        }
    }
} 