using UnityEngine;
using System.Collections;
using System.Collections.Generic;
public class Ladder : MonoBehaviour
{

    public GameObject ladderSegment;
    public float heightOfSegment;
    public float boxWidth, boxDepth;
    public BoxCollider ladderCollider;
    public Vector3 startPosition;
    public Vector3 endPosition;
    public float ladderLength;
    [ContextMenu("Build Ladder")]
    public void BuildLadder()
    {
        if (ladderSegment == null || heightOfSegment <= 0 && startPosition != endPosition)
        {
            print("Could not build ladder!");
            return;
        }
        //We are able to build the ladder
        float distance = Vector3.Distance(startPosition, endPosition);
        int num = Mathf.FloorToInt(distance / heightOfSegment);
        print(num);
        for (int i = 0; i < num; i++)
        {
            Transform t = Instantiate(ladderSegment, transform).transform;
            t.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, endPosition, Mathf.InverseLerp(0, num, i)),
                Quaternion.LookRotation(Vector3.forward, endPosition - startPosition));
        }
        if (!ladderCollider)
            ladderCollider = gameObject.AddComponent<BoxCollider>();

        ladderCollider.size = new(boxWidth, num * heightOfSegment, boxDepth);
        ladderCollider.transform.SetLocalPositionAndRotation(Vector3.Lerp(startPosition, endPosition, 0.5f),
            Quaternion.LookRotation(Vector3.forward, endPosition - startPosition));
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireSphere(startPosition, .5f);
        Gizmos.DrawWireSphere(endPosition, .5f);
    }
}
