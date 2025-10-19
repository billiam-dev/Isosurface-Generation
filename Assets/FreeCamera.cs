using UnityEngine;

[RequireComponent(typeof(Camera))]
public class FreeCamera : MonoBehaviour
{
    [Range(1f, 10f)]
    public float Acceleration = 3.0f;

    [Range(0.1f, 1f)]
    public float MouseSensitivity = 0.3f;

    Vector3 m_Rotation;
    Vector3 m_Velocity;

    void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        m_Rotation = transform.localEulerAngles;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.Confined;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMove();
        HandleLook();

        transform.localPosition += m_Velocity;
        m_Velocity *= 0.95f;
    }

    void HandleMove()
    {
        Vector2 moveInput = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool boostHeld = Input.GetKey(KeyCode.LeftShift);
        bool descendHeld = Input.GetKey(KeyCode.LeftControl);
        bool ascendHeld = Input.GetKey(KeyCode.Space);

        Vector3 move = transform.right * moveInput.x + transform.forward * moveInput.y;
        if (boostHeld) move *= 3;
        if (ascendHeld) move += Vector3.up;
        if (descendHeld) move += Vector3.down;

        m_Velocity += Acceleration * Time.deltaTime * move;
    }

    void HandleLook()
    {
        Vector2 lookInput = new(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        Vector3 mouseMove = new(-lookInput.y, lookInput.x, 0);
        m_Rotation += MouseSensitivity * mouseMove;
        m_Rotation.x = Mathf.Clamp(m_Rotation.x, -90, 90);

        transform.localEulerAngles = m_Rotation;
    }
}
