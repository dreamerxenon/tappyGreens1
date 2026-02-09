
using UnityEngine;

public class SceneGravity : MonoBehaviour
{
    public float gravityY = -9.81f; // Earth by default

    void Awake()
    {
        Physics.gravity = new Vector3(0, gravityY, 0);
    }
}
