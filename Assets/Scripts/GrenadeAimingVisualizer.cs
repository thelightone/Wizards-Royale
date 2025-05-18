using UnityEngine;

public class GrenadeAimingVisualizer : MonoBehaviour
{
    [Header("Trajectory Settings")]
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private int _trajectoryPointCount = 20;
    [SerializeField] private float _arcHeight = 2f;
    [SerializeField] private float _lineWidth = 0.1f;
    [SerializeField] private Color _lineColor = Color.red;

    [Header("Explosion Area Settings")]
    [SerializeField] private GameObject _explosionAreaPrefab;
    [SerializeField] private Color _explosionAreaColor = new Color(1f, 0f, 0f, 0.2f);
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
            _explosionArea = Instantiate(_explosionAreaPrefab, transform);
        }
        else
        {
            _explosionArea = new GameObject("ExplosionArea");
            _explosionArea.transform.SetParent(transform);

            // Создаем меш для области взрыва
            MeshFilter meshFilter = _explosionArea.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = _explosionArea.AddComponent<MeshRenderer>();

            // Создаем материал с прозрачностью
            Material material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.color = _explosionAreaColor;
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
            meshRenderer.sortingOrder = 1; // Убедимся, что область взрыва отображается поверх других объектов
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
        if (_explosionArea == null || _weaponData == null) return;

        MeshFilter meshFilter = _explosionArea.GetComponent<MeshFilter>();
        if (meshFilter == null) return;

        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[_explosionAreaSegments + 1];
        int[] triangles = new int[_explosionAreaSegments * 3];
        Vector2[] uvs = new Vector2[_explosionAreaSegments + 1];

        // Центр круга
        vertices[0] = Vector3.zero;
        uvs[0] = new Vector2(0.5f, 0.5f);

        // Вершины окружности
        for (int i = 0; i < _explosionAreaSegments; i++)
        {
            float angle = i * 2 * Mathf.PI / _explosionAreaSegments;
            float x = Mathf.Cos(angle);
            float z = Mathf.Sin(angle);
            vertices[i + 1] = new Vector3(x, 0.01f, z) * _weaponData.explosionRadius; // Добавляем небольшой отступ по Y
            uvs[i + 1] = new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f);
        }

        // Треугольники
        for (int i = 0; i < _explosionAreaSegments; i++)
        {
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = (i + 1) % _explosionAreaSegments + 1;
        }

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        meshFilter.mesh = mesh;
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
        UpdateExplosionArea();
    }

    private void UpdateTrajectory(Vector3 direction, float distance)
    {
        if (_lineRenderer == null) return;

        float gravity = Physics.gravity.magnitude;
        float timeToPeak = Mathf.Sqrt(2 * _arcHeight / gravity);
        float totalTime = timeToPeak * 2;
        float horizontalSpeed = distance / totalTime;
        float verticalSpeed = Mathf.Sqrt(2 * gravity * _arcHeight);

        Vector3 startPos = transform.position;
        Vector3 velocity = direction * horizontalSpeed;
        velocity.y = verticalSpeed;

        for (int i = 0; i < _trajectoryPointCount; i++)
        {
            float t = (float)i / (_trajectoryPointCount - 1) * totalTime;
            Vector3 pos = startPos + velocity * t;
            pos.y += -0.5f * gravity * t * t;
            _trajectoryPoints[i] = pos;
        }

        _lineRenderer.SetPositions(_trajectoryPoints);
        _lineRenderer.enabled = true;
    }

    private void UpdateExplosionArea()
    {
        if (_explosionArea == null || _trajectoryPoints == null || _weaponData == null) return;

        Vector3 endPoint = _trajectoryPoints[_trajectoryPointCount - 1];
        _explosionArea.transform.position = endPoint;
        _explosionArea.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.down);
        _explosionArea.SetActive(true);

        // Обновляем меш области взрыва при каждом обновлении
        UpdateExplosionAreaMesh();
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