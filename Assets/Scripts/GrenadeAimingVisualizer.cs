using UnityEngine;

public class GrenadeAimingVisualizer : MonoBehaviour
{
    [Header("Trajectory Settings")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private int _trajectoryPointCount = 20;
    [SerializeField] private float _arcHeight = 2f; // This will be dynamically calculated now
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private Color _lineColor = Color.red;

    [Header("Explosion Area Settings")]
    [SerializeField] private GameObject _explosionAreaPrefab;
    [SerializeField] private Color _explosionAreaColor = new Color(1f, 0f, 0f, 0.3f); // Красный с прозрачностью 0.3
    [SerializeField] private int _explosionAreaSegments = 32;

    private WeaponData _weaponData;
    private GameObject _explosionArea;
    private Vector3[] _trajectoryPoints;
    private bool _isAiming;

    public void SetLineRenderer(LineRenderer lineRenderer)
    {
        _lineRenderer = lineRenderer;
        InitializeLineRenderer();
    }

    private void Start()
    {
        InitializeLineRenderer();
        CreateExplosionArea();
        
        // Инициализируем массив точек траектории
        _trajectoryPoints = new Vector3[_trajectoryPointCount];
    }

    private void InitializeLineRenderer()
    {
        if (_lineRenderer == null)
        {
            _lineRenderer = GetComponent<LineRenderer>();
            if (_lineRenderer == null)
            {
                GameObject lineRendererObj = new GameObject("GrenadeLineRenderer");
                lineRendererObj.transform.SetParent(transform);
                _lineRenderer = lineRendererObj.AddComponent<LineRenderer>();
            }
        }

        _lineRenderer.startWidth = _lineWidth;
        _lineRenderer.endWidth = _lineWidth;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.material.color = _lineColor;
        _lineRenderer.positionCount = _trajectoryPointCount;
        _lineRenderer.enabled = false;

        _trajectoryPoints = new Vector3[_trajectoryPointCount];
    }

    private void CreateExplosionArea()
    {
        if (_explosionAreaPrefab != null)
        {
            _explosionArea = Instantiate(_explosionAreaPrefab);
            _explosionArea.transform.SetParent(null);
            Debug.Log("Created explosion area from prefab");
        }
        else
        {
            _explosionArea = new GameObject("ExplosionArea");
            _explosionArea.transform.SetParent(null);
            Debug.Log("Created explosion area GameObject");

            // Создаем цилиндр для области взрыва
            MeshFilter meshFilter = _explosionArea.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _explosionArea.AddComponent<MeshRenderer>();

            // Создаем цилиндр
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            meshFilter.sharedMesh = cylinder.GetComponent<MeshFilter>().sharedMesh;
            Destroy(cylinder);

            // Создаем материал с прозрачностью для URP
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _explosionAreaColor;
            
            // Настраиваем прозрачность для URP
            material.SetFloat("_Surface", 1); // 1 = Transparent
            material.SetFloat("_Blend", 0); // 0 = Alpha
            material.SetFloat("_AlphaClip", 0);
            material.renderQueue = 3000;
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            meshRenderer.material = material;
            meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            meshRenderer.receiveShadows = false;

            // Устанавливаем начальный размер и позицию
            _explosionArea.transform.localPosition = Vector3.zero;
            _explosionArea.transform.localRotation = Quaternion.identity;
            _explosionArea.transform.localScale = new Vector3(1f, 0.01f, 1f);
            Debug.Log("Created explosion area cylinder and material");
        }

        _explosionArea.SetActive(false);
    }

    public void SetWeaponData(WeaponData weaponData)
    {
        _weaponData = weaponData;
        UpdateExplosionAreaMesh();
    }

    private void UpdateExplosionAreaMesh()
    {
        if (_explosionArea == null)
        {
            Debug.LogWarning("Explosion area is null");
            return;
        }
        if (_weaponData == null)
        {
            Debug.LogWarning("Weapon data is null");
            return;
        }

        // Обновляем размер цилиндра
        float radius = _weaponData.explosionRadius;
        _explosionArea.transform.localScale = new Vector3(radius * 2f, 0.01f, radius * 2f);

        // Обновляем цвет материала
        MeshRenderer meshRenderer = _explosionArea.GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            // Убедимся, что цвет остается полупрозрачным
            Color currentColor = _explosionAreaColor;
            currentColor.a = 0.3f; // Фиксированная прозрачность
            meshRenderer.material.color = currentColor;
        }

        Debug.Log($"Updated explosion area size to radius {radius}");
    }

    public void UpdateAiming(Vector3 direction, bool isAiming, float distance)
    {
        _isAiming = isAiming;

        if (!_isAiming)
        {
            _lineRenderer.enabled = false;
            if (_explosionArea != null)
            {
                _explosionArea.SetActive(false);
            }
            return;
        }

        UpdateTrajectory(direction, distance);
    }

    // Расчет высоты дуги точно так же, как в классе Grenade
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
    
    // Расчет длительности полета точно так же, как в классе Grenade
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

    private void UpdateTrajectory(Vector3 direction, float distance)
    {
        if (_lineRenderer == null) return;

        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * distance;
        
        // Вычисляем высоту дуги и длительность полета соответственно
        float arcHeight = CalculateArcHeight(distance);
        float throwDuration = CalculateFlightDuration(distance);

        // Генерируем точки параболической траектории
        for (int i = 0; i < _trajectoryPointCount; i++)
        {
            float normalizedTime = (float)i / (_trajectoryPointCount - 1);
            
            // Линейная интерполяция для позиции и параболическая для высоты
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, normalizedTime);
            float heightOffset = Mathf.Sin(normalizedTime * Mathf.PI) * arcHeight;
            currentPos.y += heightOffset;
            
            _trajectoryPoints[i] = currentPos;
        }

        _lineRenderer.SetPositions(_trajectoryPoints);
        _lineRenderer.enabled = true;

        // Обновляем область взрыва сразу после обновления траектории
        UpdateExplosionArea();
    }

    private void UpdateExplosionArea()
    {
        if (_explosionArea == null)
        {
            Debug.LogWarning("Explosion area is null in UpdateExplosionArea");
            return;
        }
        if (_trajectoryPoints == null)
        {
            Debug.LogWarning("Trajectory points is null in UpdateExplosionArea");
            return;
        }
        if (_weaponData == null)
        {
            Debug.LogWarning("Weapon data is null in UpdateExplosionArea");
            return;
        }

        // Получаем последнюю точку траектории
        Vector3 endPoint = _trajectoryPoints[_trajectoryPointCount - 1];
        
        // Отладочная информация
        Debug.Log($"End point of trajectory: {endPoint}");
        Debug.Log($"Current explosion area position: {_explosionArea.transform.position}");
        
        // Устанавливаем позицию области взрыва
        _explosionArea.transform.position = endPoint;
        _explosionArea.transform.rotation = Quaternion.identity;
        
        // Обновляем размер области взрыва
        UpdateExplosionAreaMesh();
        
        // Убедимся, что область взрыва активна и видима
        _explosionArea.SetActive(true);
        
        // Проверяем видимость
        MeshRenderer meshRenderer = _explosionArea.GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            meshRenderer.enabled = true;
        }

        // Проверяем финальную позицию
        Debug.Log($"Final explosion area position: {_explosionArea.transform.position}");
    }

    private void OnDisable()
    {
        if (_lineRenderer != null)
        {
            _lineRenderer.enabled = false;
        }
        if (_explosionArea != null)
        {
            _explosionArea.SetActive(false);
        }
    }

    private void OnDrawGizmos()
    {
        if (!_isAiming || _trajectoryPoints == null) return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < _trajectoryPoints.Length - 1; i++)
        {
            Gizmos.DrawLine(_trajectoryPoints[i], _trajectoryPoints[i + 1]);
        }

        if (_weaponData != null)
        {
            Gizmos.color = _explosionAreaColor;
            Gizmos.DrawWireSphere(_trajectoryPoints[_trajectoryPointCount - 1], _weaponData.explosionRadius);
        }
    }
} 