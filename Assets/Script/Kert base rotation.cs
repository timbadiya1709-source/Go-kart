using UnityEngine;

public class Kertbaserotation : MonoBehaviour
{   public float rotation = 15f;

    void Start()
    {
        
    }
    void Update()
    {
        transform.Rotate(Vector2.up, rotation * Time.deltaTime);
    }
}
