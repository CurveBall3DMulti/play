using UnityEngine;
using Mirror;

public class PlayerController : NetworkBehaviour
{
    public float speed = 10f;
    public float boundary = 4.5f;

    void Update()
    {
        if (!isLocalPlayer) return;

        float move = Input.GetAxis("Horizontal") * speed * Time.deltaTime;
        transform.position = new Vector3(Mathf.Clamp(transform.position.x + move, -boundary, boundary), transform.position.y, transform.position.z);
    }
}
