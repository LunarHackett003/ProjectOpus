using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Opus
{
    public class ObjectiveUI : ONetBehaviour
    {
        public const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        public RectTransform rect;

        public bool useDynamicText;
        public bool useLetter = false;
        public NetworkVariable<FixedString32Bytes> dynamicText = new("", NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public Image spriteDisplay;
        public TMP_Text text;

        public bool useDistanceText;
        public TMP_Text distanceText;

        public float distance;
        float lastdistance;

        public float scalePerDistance;


        public Image progressImage;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(text != null)
            text.gameObject.SetActive(useDynamicText);
            if(spriteDisplay != null)
                spriteDisplay.gameObject.SetActive(!useDynamicText);
            if (distanceText != null)
                distanceText.gameObject.SetActive(useDistanceText);
            if (useDynamicText)
                dynamicText.OnValueChanged += DynamicTextChanged;
            if (progressImage != null)
                progressImage.enabled = false;
        }

        void DynamicTextChanged(FixedString32Bytes oldValue, FixedString32Bytes newValue)
        {
            text.text = dynamicText.Value.ToString();
        }

        public override void OFixedUpdate()
        {
            base.OFixedUpdate();
            if (useDistanceText)
            {
                distance = Vector3.Distance(transform.position, Camera.main.transform.position);
                if(!Mathf.Approximately(distance, lastdistance))
                {
                    distanceText.text = distance.ToString("0") + "M";
                    lastdistance = distance;
                    rect.localScale = Mathf.Max(2, distance) * (1 + scalePerDistance) * Vector3.one;
                }
            }
        }
    }
}
