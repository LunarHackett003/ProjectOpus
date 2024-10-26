using UnityEngine;

namespace Opus
{
    public class UIRotator : MonoBehaviour
    {
        public float rotateSpeed = 15;
        
        private void Update()
        {
            if(transform is RectTransform r)
            {
                r.Rotate(new Vector3(0, 0, rotateSpeed * Time.deltaTime));
            }   
        }
    }
}
