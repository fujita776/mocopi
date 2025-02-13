using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHand : MonoBehaviour, IHandHolder
{
    // �茳�Ƃ��ė��p���� Transform �� Inspector �Őݒ�
    [SerializeField] private Transform handTransform;
    public Transform HandTransform => handTransform;

    // ���ݎ茳�ɕێ����Ă���A�C�e���i�O������͎擾�̂݉\�j
    public GameObject HeldItem { get; private set; } = null;

    /// <summary>
    /// �w�肳�ꂽ�A�C�e�����茳�ɃA�^�b�`����
    /// </summary>
    public void AttachItem(GameObject item)
    {
        if (item == null) return;

        HeldItem = item;
        // �A�C�e�����茳�ihandTransform�j�̎q�Ƃ��Đݒ�
        item.transform.SetParent(handTransform);
        // �茳�̈ʒu�Ɖ�]�ɍ��킹��
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // �����������ꎞ��~����i�茳�ɕێ����͕����V�~�����[�V�����s�v�j
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
        }
    }

    /// <summary>
    /// ���ݕێ����Ă���A�C�e����������i�f�^�b�`����j
    /// </summary>
    public void DetachItem()
    {
        if (HeldItem == null) return;

        // �e�q�֌W���������āA�����
        HeldItem.transform.SetParent(null);
        // �����������ĊJ����
        Rigidbody rb = HeldItem.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
        }

        HeldItem = null;
    }

    /// <summary>
    /// ���ݕێ����Ă���A�C�e�����w�肳�ꂽ�^�[�Q�b�g�ʒu�֓�����
    /// </summary>
    /// <param name="target">������� Transform</param>
    public void ThrowItem(Transform target)
    {
        if (HeldItem == null || target == null) return;

        // ������A�C�e�����擾���āA�茳�����������
        GameObject itemToThrow = HeldItem;
        HeldItem = null;
        itemToThrow.transform.SetParent(null);

        // Rigidbody ���擾�B���݂��Ȃ��ꍇ�͒ǉ�����B
        Rigidbody rb = itemToThrow.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = itemToThrow.AddComponent<Rigidbody>();
        }
        rb.isKinematic = false;

        // ������������v�Z�i�茳����^�[�Q�b�g�ւ̕����j
        Vector3 throwDirection = (target.position - handTransform.position).normalized;
        // �Ⴆ�΁A�኱������֎����グ�邽�߂ɁA������̕␳��ǉ�
        throwDirection += Vector3.up * 0.3f;
        throwDirection.Normalize();

        // ������͂�ݒ�i�K�v�ɉ����Ē����j
        float throwForce = 25f;
        // Rigidbody �ɃC���p���X�Ƃ��ė͂�������
        rb.AddForce(throwDirection * throwForce, ForceMode.Impulse);
    }
}
