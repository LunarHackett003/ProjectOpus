using UnityEngine;

namespace Opus
{
    public class TestCameraFly : MonoBehaviour
    {
        public float lookSpeed = 15;
        public float movespeed = 5;
        public float boostSpeed = 1.5f;
        public float slowSpeed = .3f;
        float aimPitch;
        float aimYaw;
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
        }
        private void Update()
        {

                aimYaw += Input.GetAxis("Mouse X") * Time.deltaTime * lookSpeed;
                aimYaw %= 360;
                aimPitch -= Input.GetAxis("Mouse Y") * Time.deltaTime * lookSpeed;
                aimPitch = Mathf.Clamp(aimPitch, -89, 89);
                transform.rotation = Quaternion.Euler(aimPitch, aimYaw, 0);
            

        }
        private void FixedUpdate()
        {

                Vector3 move = new()
                {
                    x = Input.GetAxis("Horizontal"),
                    y = (Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.Q)) ? -1 : (Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.E) ? 1 : 0),
                    z = Input.GetAxis("Vertical"),
                };
                float boost = Input.GetKey(KeyCode.LeftShift) ? boostSpeed : (Input.GetKey(KeyCode.LeftControl) ? slowSpeed : 1);
                transform.Translate(boost * movespeed * Time.fixedDeltaTime * move, Space.Self);
            
        }

    }
}
