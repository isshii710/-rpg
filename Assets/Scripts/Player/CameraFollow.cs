using UnityEngine;

/// <summary>
/// 指定した対象をなめらかに追いかけるカメラスクリプト。
/// Main Camera にアタッチして Target に Player を設定する。
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("追従対象")]
    [SerializeField] Transform target;

    [Header("カメラのオフセット（対象からの相対位置）")]
    [SerializeField] Vector3 offset = new Vector3(0f, 7f, -6f);

    [Header("追従の滑らかさ（大きいほど素早くついてくる）")]
    [SerializeField] float smoothSpeed = 8f;

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);
    }
}
