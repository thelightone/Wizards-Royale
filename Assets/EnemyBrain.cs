using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Animations;

public class EnemyBrain : MonoBehaviour
{
    private CharacterSkinManager _skinManager;
    [SerializeField] private Animator _animator;
    private NavMeshAgent _agent;
    private Transform _player;
    private EnemyAttack _enemyAttack;

    private enum State { Patrol, Chase, Attack }
    private State state;

    private Vector3 _currentPatrolPoint;

    public WeaponData _enemyWeapon;
    private float _enemyRange;
    private float _reloadTime;

    [Header("Настройки поведения")]
    [SerializeField] private float _detectionRange = 15f; // Дальность обнаружения игрока (должна быть больше дальности атаки)
    [SerializeField] private float _rotationSpeed = 15f; // Скорость поворота к игроку
    [SerializeField] private bool _instantRotation = false; // Мгновенный поворот при атаке

    public bool freeze;

    public Transform _nearestPlayer;
    public Player aimPlayer;

    private Vector3 _prevPos;

    private float patrolTimeChange = 2;

    [SerializeField] private TMP_Text _name;

    private void Start()
    {
        _player = FindAnyObjectByType<PlayerController>().transform;
        _skinManager = GetComponent<CharacterSkinManager>();
        _animator = GetComponent<CharacterSkinManager>()?.GetCurrentAnimatorAndWeapon();
        SetRandomSkinAndWeapon();
        _enemyAttack = GetComponent<EnemyAttack>();
        _enemyAttack.SetWeaponAndAnimator(_enemyWeapon, _animator);
        _enemyRange = _enemyWeapon.range;
        _reloadTime = _enemyWeapon.shootingCooldown;

        _agent = GetComponent<NavMeshAgent>();

        state = State.Patrol;

        _name.text = "Player" + Random.Range(100, 999);
    }

    private void SetRandomSkinAndWeapon()
    {
        // Выбираем случайный скин
        var _skinIndex = Random.Range(0, _skinManager.skinPrefabs.Length);
        _skinManager.ChangeSkin(_skinIndex);

        // Получаем аниматор после смены скина
        _animator = _skinManager.GetCurrentAnimatorAndWeapon();
    }

    private void Update()
    {
        if (MatchManager.instance.inMatch && !GetComponent<Health>().dead)
        {
            CheckNearestPlayer();

            float distanceToPlayer = Vector3.Distance(transform.position, _nearestPlayer.position);

            if (distanceToPlayer <= _detectionRange && GetComponent<Player>().team != _nearestPlayer.GetComponent<Player>().team)
            {
                // В зоне атаки - мгновенный поворот
                if (distanceToPlayer <= _enemyRange)
                {
                    if (_instantRotation)
                    {
                        // Мгновенный поворот при атаке
                        transform.LookAt(new Vector3(_nearestPlayer.position.x, transform.position.y, _nearestPlayer.position.z));
                    }
                    else
                    {
                        // Плавный поворот при атаке
                        RotateTowardsPlayer();
                    }

                    if (_reloadTime > 2 && _nearestPlayer.GetComponent<Player>().team != GetComponent<Player>().team)
                    {
                        _enemyAttack.Shoot(_nearestPlayer.position);
                        _reloadTime = 0;
                        state = State.Attack;
                    }
                    else
                    {
                        _reloadTime += Time.deltaTime;
                        state = State.Patrol;
                    }
                }
                // В зоне обнаружения, но не в зоне атаки - просто смотрит
                else
                {
                    RotateTowardsPlayer();
                    _reloadTime += Time.deltaTime;
                    state = State.Patrol;
                }
            }
            else
            {
                _reloadTime += Time.deltaTime;
                state = State.Patrol;
            }

            switch (state)
            {
                case State.Patrol:
                    UpdatePatrol();
                    break;
                case State.Attack:
                    UpdateAttack();
                    break;
            }
        }
    }

    private void CheckNearestPlayer()
    {
        _nearestPlayer = null;
        float distanceToPlayer = 100000;

        foreach (var player in MatchManager.instance.playersList)
        {
            if (Vector3.Distance(transform.position, player.transform.position) < distanceToPlayer && player != GetComponent<Player>())
            {
                distanceToPlayer = Vector3.Distance(transform.position, player.transform.position);
                _nearestPlayer = player.transform;
            }
        }
    }

    // Плавно поворачивает противника к игроку
    private void RotateTowardsPlayer()
    {
        Vector3 targetPosition = new Vector3(_nearestPlayer.position.x, transform.position.y, _nearestPlayer.position.z);
        Vector3 direction = (targetPosition - transform.position).normalized;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * _rotationSpeed);
        }
    }

    private void UpdateAttack()
    {
        // Враг продолжает смотреть на игрока во время атаки
        if (_player != null)
        {
            RotateTowardsPlayer();
        }
    }

    private void UpdateChase()
    {
        throw new System.NotImplementedException();
    }

    private void UpdatePatrol()
    {
        _animator.SetFloat("MoveSpeed", 1);

        if (Vector3.Distance(_currentPatrolPoint, transform.position) < 2 || _prevPos == transform.position || patrolTimeChange > 2)
        {
            patrolTimeChange = 0;
            SetPatrolPoint();
        }
        patrolTimeChange += Time.deltaTime; 
        _prevPos = transform.position;
    }

    private void SetPatrolPoint()
    {
        aimPlayer = MatchManager.instance.playersList[Random.Range(0, MatchManager.instance.playersList.Count)];
        Transform newAim = aimPlayer.transform;

        _currentPatrolPoint = new Vector3(newAim.position.x + Random.Range(-10, 10), transform.position.y, newAim.position.z + Random.Range(-10, 10));
        if (!freeze)
            _agent.SetDestination(_currentPatrolPoint);
    }

    // Визуализация радиусов обнаружения и атаки в редакторе
    private void OnDrawGizmosSelected()
    {
        // Рисуем радиус обнаружения
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        // Рисуем радиус атаки
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _enemyRange);
    }
}
