using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform hedef;
    public Vector3 offset = new Vector3(0, 0, -10);
    public float yumusaklik = 0.125f;
    
    void LateUpdate()
    {
        if (hedef == null) return;
        
        Vector3 hedefPoz = hedef.position + offset;
        Vector3 yumusakPoz = Vector3.Lerp(transform.position, hedefPoz, yumusaklik);
        transform.position = yumusakPoz;
    }
}