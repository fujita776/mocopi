using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NullHandHolder : IHandHolder
{
    // HandTransform はダミーとして null を返す（必要なら安全なダミー Transform を返す実装も可能）
    public Transform HandTransform => null;

    // HeldItem は常に何も保持していないので null を返す
    public GameObject HeldItem { get; private set; } = null;

    // 何もしない実装
    public void AttachItem(GameObject item) { }

    // 何もしない実装
    public void DetachItem() { }

    public void ThrowItem(Transform target, float force) { }
}