using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon", menuName = "Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Basic Settings")]
    public string weaponName;
    public Sprite weaponIcon;
    public GameObject projectilePrefab;
    public float damage = 10f;
    public float range = 10f;
    public float shootingCooldown = 0.5f;
    public float projectileSpeed = 20f;
    public float minShootingDistance = 0.5f;

    [Header("Weapon Type")]
    public bool isShotgun;
    public bool isGrenade;
    public bool isAutomatic;

    [Header("Shotgun Settings")]
    public int pelletCount = 5;
    public float spreadAngle = 15f;

    [Header("Grenade Settings")]
    public float explosionRadius = 3f;
    public float fuseTime = 3f;
    public float minThrowDistance = 2f;
    public float maxThrowDistance = 10f;
    public GameObject explosionEffect;
    public AudioClip explosionSound;

    [Header("Automatic Weapon Settings")]
    public float fireRate = 0.1f;
    public int burstSize = 3;
    public float burstCooldown = 1f;

    [Header("Effects")]
    public GameObject shootEffect;
    public AudioClip shootSound;

    [Header("Projectile Effects")]
    public GameObject hitEffect;
    public float slowEffect = 0f;
    public float slowDuration = 3f;
    public GameObject stunEffect;
    public GameObject poisonEffect;
    public float effectDuration = 3f;

    [Header("Projectile Properties")]
    public bool isExplosive = false;
    public bool isPiercing = false;
} 