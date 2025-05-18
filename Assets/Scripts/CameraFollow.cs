using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform _player;
    public Vector3 _offset = new Vector3(0, 5, -3);

    private void Update()
    {
        transform.position = _player.position + _offset;
    }
}
