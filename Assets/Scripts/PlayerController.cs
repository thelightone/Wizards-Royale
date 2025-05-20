using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterSkinManager))]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Управление движением")]
    [SerializeField] private FloatingJoystick _joystick;
    [SerializeField] private float _maxMoveSpeed = 5f;
    [SerializeField] private float _rotateSpeed = 10f;
    [SerializeField] private float _accelerationSpeed = 5f;
    [SerializeField] private float _decelerationSpeed = 8f;
    [SerializeField] private float _movementThreshold = 0.1f;
    
    // Приватные переменные
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private Animator _animator;
    private Rigidbody _rb;
    private ShootingController _shootingController;
    private CharacterSkinManager _skinManager;
    private Health _health;

    [SerializeField] private Image _cooldownIndicator;
    private float _lastRushTime;
    private bool _rushReload;

    private void Start()
    {
        // Получаем необходимые компоненты
        _rb = GetComponent<Rigidbody>();
        _skinManager = GetComponent<CharacterSkinManager>();
        _shootingController = GetComponent<ShootingController>();
        _animator = _skinManager.GetCurrentAnimatorAndWeapon();
        _health = GetComponent<Health>();

        // Настраиваем физику
        ConfigureRigidbody();
    }
    
    private void ConfigureRigidbody()
    {
        _rb.freezeRotation = true;
        _rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        if (MatchManager.instance.inMatch && !GetComponent<Health>().dead)
        {
            HandleMovement();
        }
        else
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;

        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_cooldownIndicator != null)
        {
            float cooldownProgress = (Time.time - _lastRushTime) / 5;
            _cooldownIndicator.fillAmount = Mathf.Clamp01(cooldownProgress);
        }
    }

    private void HandleMovement()
    {
        // Сбрасываем скорости
        //_rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        
        // Получаем ввод игрока
        Vector2 input = new Vector2(_joystick.Horizontal, _joystick.Vertical);
        float inputMagnitude = input.magnitude;
        
        // Обработка начала движения
        bool wasMoving = IsMoving();
        UpdateMovementSpeed(inputMagnitude);
        
        // Обновляем вектор движения
        _moveDirection = new Vector3(input.x, 0, input.y).normalized * _currentSpeed;
        
        // При движении всегда поворачиваем персонажа в направлении движения
        if (inputMagnitude > _movementThreshold && _currentSpeed > 0.1f)
        {
            Vector3 lookDir = new Vector3(input.x, 0, input.y).normalized;
            if (lookDir.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * _rotateSpeed);
            }
        }
        
        // Применяем движение
        ApplyMovement();
        
        // Обновляем анимацию
        UpdateAnimation();
    }
    
    private void UpdateMovementSpeed(float inputMagnitude)
    {
        if (inputMagnitude > _movementThreshold)
        {
            // Ускорение
            float targetSpeed = _maxMoveSpeed * inputMagnitude;
            _currentSpeed = Mathf.Lerp(_currentSpeed, targetSpeed, Time.fixedDeltaTime * _accelerationSpeed);
        }
        else
        {
            // Замедление
            _currentSpeed = Mathf.Lerp(_currentSpeed, 0f, Time.fixedDeltaTime * _decelerationSpeed);
        }
    }
    
    private void HandleRotation(float inputMagnitude)
    {
        // Этот метод больше не используется, поворот происходит в HandleMovement
    }
    
    private void ApplyMovement()
    {
        if (IsMoving())
        {
            // Применяем горизонтальное движение, сохраняя вертикальную скорость
            _rb.linearVelocity = new Vector3(_moveDirection.x, _rb.linearVelocity.y, _moveDirection.z);
        }
    }
    
    private void UpdateAnimation()
    {
        float animationSpeed = _currentSpeed / _maxMoveSpeed;
        _animator?.SetFloat("MoveSpeed", animationSpeed);
    }
    
    // Публичные методы
    
    public bool IsMoving()
    {
        return _currentSpeed > _movementThreshold;
    }
    
    public void ChangeSkin(int skinIndex)
    {
        _skinManager.ChangeSkin(skinIndex);
        _animator = _skinManager.GetCurrentAnimatorAndWeapon();
    }

    public void Roll()
    {
        _animator.SetTrigger("Roll");
    }

    public void Rush()
    {
        if (!_rushReload)
        {
            _rushReload = true;
            _maxMoveSpeed = 30;
            _accelerationSpeed = 30;
            _lastRushTime = Time.time;
            StartCoroutine(RushCor());
        }
    }

    private IEnumerator RushCor()
    {
        yield return new WaitForSeconds(0.1f);
        _maxMoveSpeed = 5;
        _accelerationSpeed = 5;
        yield return new WaitForSeconds(5f);
        _rushReload = false;
    }
}
