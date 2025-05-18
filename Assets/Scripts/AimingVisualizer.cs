using UnityEngine;

public class AimingVisualizer : MonoBehaviour
{
    [Header("Line Settings")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private Material _lineMaterial;
    
    [Header("Spread Settings")]
    [SerializeField] private int _spreadSegments = 20;
    [SerializeField] private float _spreadLineWidth = 0.05f;
    
    private Vector3 _currentDirection;
    private float _currentRange;
    private bool _isAiming;
    private bool _isShotgun;
    private float _spreadAngle;

    public LineRenderer LineRenderer => _lineRenderer;

    private void Start()
    {
        InitializeLineRenderer();
    }

    private void InitializeLineRenderer()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (_lineMaterial == null)
        {
            _lineMaterial = new Material(Shader.Find("Sprites/Default"));
            _lineMaterial.color = Color.red;
        }

        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.material = _lineMaterial;
        _lineRenderer.positionCount = 2;
        _lineRenderer.enabled = false;
    }

    public void UpdateAiming(Vector3 direction, bool isAiming, float range, bool isShotgun = false, float spreadAngle = 0f)
    {
        _currentDirection = direction;
        _currentRange = range;
        _isAiming = isAiming;
        _isShotgun = isShotgun;
        _spreadAngle = spreadAngle;

        if (!_isAiming)
        {
            _lineRenderer.enabled = false;
            return;
        }

        if (_isShotgun)
        {
            UpdateSpreadCone();
        }
        else
        {
            UpdateMainLine();
        }
    }

    private void UpdateMainLine()
    {
        _lineRenderer.enabled = true;
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.material = _lineMaterial;
        _lineRenderer.SetPosition(0, transform.position);
        _lineRenderer.SetPosition(1, transform.position + _currentDirection * _currentRange);
    }

    private void UpdateSpreadCone()
    {
        _lineRenderer.enabled = true;
        // Увеличиваем количество точек для создания заполненного конуса
        int totalPoints = (_spreadSegments + 1) * 2;
        _lineRenderer.positionCount = totalPoints;
        _lineRenderer.startWidth = _spreadLineWidth;
        _lineRenderer.endWidth = _spreadLineWidth;
        _lineRenderer.material = _lineMaterial;
        
        // Создаем точки для конуса разброса
        for (int i = 0; i <= _spreadSegments; i++)
        {
            float angle = (i / (float)_spreadSegments) * _spreadAngle - (_spreadAngle / 2);
            
            // Вычисляем направление для текущей точки конуса
            Vector3 spreadDirection = Quaternion.Euler(0, angle, 0) * _currentDirection;
            
            // Устанавливаем позиции для создания заполненного конуса
            // Первая линия - от центра к краю конуса
            _lineRenderer.SetPosition(i * 2, transform.position);
            _lineRenderer.SetPosition(i * 2 + 1, transform.position + spreadDirection * _currentRange);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_isAiming) return;

        // Основная линия прицеливания
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + _currentDirection * _currentRange);

        if (_isShotgun)
        {
            // Визуализация конуса разброса
            Gizmos.color = Color.yellow;
            int segments = 20;
            float angleStep = _spreadAngle / segments;
            
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep - (_spreadAngle / 2);
                Vector3 spreadDirection = Quaternion.Euler(0, angle, 0) * _currentDirection;
                // Рисуем линии от центра к краям конуса
                Gizmos.DrawLine(transform.position, transform.position + spreadDirection * _currentRange);
            }
        }
    }
} 