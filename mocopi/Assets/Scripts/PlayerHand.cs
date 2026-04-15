using UnityEngine;

public class PlayerHand : MonoBehaviour, IHandHolder
{
    [SerializeField] private Transform handTransform;
    public Transform HandTransform => handTransform;

    public GameObject HeldItem { get; private set; } = null;

    public void AttachItem(GameObject item)
    {
        if (item == null) return;

        HeldItem = item;
        item.transform.SetParent(handTransform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    public void DetachItem()
    {
        if (HeldItem == null) return;

        HeldItem.transform.SetParent(null);
        Rigidbody rb = HeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        HeldItem = null;
    }

    /// <summary>
    /// 現在保持しているアイテムを、指定のターゲット位置に投げる処理。
    /// throwForce の値に応じて、到達するための発射角度を計算します。
    /// </summary>
    /// <param name="target">投げ先のターゲット Transform</param>
    /// <param name="force">投げる力（初速）</param>
    public void ThrowItem(Transform target, float force)
    {
        if (HeldItem == null || target == null) return;

        GameObject itemToThrow = HeldItem;
        HeldItem = null;
        itemToThrow.transform.SetParent(null);

        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = itemToThrow.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;

        // 現在の手元位置からターゲットまでの変位を求める
        Vector3 displacement = target.position - handTransform.position;
        // 水平成分のみ（Y成分を除く）
        Vector3 horizontalDisplacement = new Vector3(displacement.x, 0, displacement.z);
        float d = horizontalDisplacement.magnitude;  // 水平距離
        float h = displacement.y;                      // 高さの差
        float v = force;                               // 初速としての throwForce
        float g = -Physics.gravity.y;                  // 重力加速度（正の値）

        // 判別式を計算：v^4 - g*(g*d^2 + 2*h*v^2)
        float disc = v * v * v * v - g * (g * d * d + 2 * h * v * v);
        if (disc < 0)
        {
            // 投げる力が足りずターゲットに到達できない場合は、通常の方向に力を加える
            Debug.LogWarning("指定の力ではターゲットに到達できません。");
            rb.AddForce((target.position - handTransform.position).normalized * v, ForceMode.Impulse);
            return;
        }

        float sqrtDisc = Mathf.Sqrt(disc);
        // 低軌道の解を選ぶ（v^2 - sqrtDisc）
        float angle = Mathf.Atan((v * v - sqrtDisc) / (g * d));

        // 初速ベクトルを計算：水平成分と垂直成分に分ける
        Vector3 initialVelocity = horizontalDisplacement.normalized * (v * Mathf.Cos(angle)) + Vector3.up * (v * Mathf.Sin(angle));
        rb.AddForce(initialVelocity, ForceMode.Impulse);
    }
}
