using UnityEngine;

public class CharacterSkinManager : MonoBehaviour
{
    [Header("Skin Settings")]
    public GameObject[] skinPrefabs;
    [SerializeField] private Transform skinRoot;

    private GameObject currentSkin;
    private Animator currentAnimator;
    private int currentSkinIndex;
    private ShootingController shootingController;
    private EnemyBrain enemyBrain;
    public CharController charController;
    public Sprite avatar;

    private void Awake()
    {
        if (skinPrefabs == null || skinPrefabs.Length == 0)
        {
            Debug.LogError("No skin prefabs assigned!");
            return;
        }

        // Получаем ShootingController, если он есть (у игрока есть, у врага нет)
        shootingController = GetComponent<ShootingController>();
        enemyBrain = GetComponent<EnemyBrain>();

        
        // Применяем скин 
        ChangeSkin(PlayerPrefs.GetInt("skinId",0));
    }

    public void ChangeSkin(int skinIndex)
    {
        if (skinIndex < 0 || skinIndex >= skinPrefabs.Length)
        {
            Debug.LogWarning($"Invalid skin index: {skinIndex}");
            return;
        }

        // Сохраняем текущую позицию и поворот
        Vector3 currentPosition = currentSkin != null ? currentSkin.transform.position : transform.position;
        Quaternion currentRotation = currentSkin != null ? currentSkin.transform.rotation : transform.rotation;

        // Удаляем текущий скин
        if (currentSkin != null)
        {
            Destroy(currentSkin);
        }

        // Создаем новый скин
        currentSkin = Instantiate(skinPrefabs[skinIndex], skinRoot);
        currentSkin.transform.localPosition = Vector3.zero;
        currentSkin.transform.localRotation = Quaternion.identity;

        // Получаем аниматор нового скина
        currentAnimator = currentSkin.GetComponent<Animator>();
        if (currentAnimator == null)
        {
            Debug.LogWarning("New skin doesn't have Animator component!");
        }

        currentSkinIndex = skinIndex;

        charController = currentSkin.GetComponent<CharController>();

        // Устанавливаем оружие только если есть ShootingController (у игрока)
        if (shootingController != null)
        {
            shootingController.SetWeapon(charController.characterData.baseWeapon);
        }
        else
        {
            enemyBrain._enemyWeapon = charController.characterData.baseWeapon;
        }

        avatar = charController.characterData.avatar;
    }

    public int GetCurrentSkinIndex()
    {
        return currentSkinIndex;
    }

    public Animator GetCurrentAnimatorAndWeapon()
    {
        return currentAnimator;
    }
    
    public int GetSkinCount()
    {
        return skinPrefabs != null ? skinPrefabs.Length : 0;
    }
} 