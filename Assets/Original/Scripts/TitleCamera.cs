using UnityEngine;

public class TitleCamera : MonoBehaviour
{
    // Update is called once per frame
    void Update()
    {
        // 自転させる(X軸固定)
        transform.RotateAround(transform.position, Vector3.up, Time.deltaTime * 10);
    }
}
