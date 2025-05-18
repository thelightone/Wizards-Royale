using UnityEngine;

[CreateAssetMenu(fileName = "New Character", menuName = "Characters/Character Data")]
public class CharacterData : ScriptableObject
{
    public WeaponData baseWeapon;
    public Sprite avatar;
} 