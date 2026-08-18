using System.Collections;
using UnityEngine;

public class GachaMachineAnimator : MonoBehaviour
{
    [Header("Targets")]
    public Transform machineRoot;
    public Transform handleKnob;

    [Header("Handle")]
    public float handleDuration = 0.35f;
    public float handleRotationDegrees = -360f;

    [Header("Machine Shake")]
    public float shakeDuration = 0.42f;
    public float shakeAmount = 0.06f;
    public float shakeSpeed = 42f;

    [Header("Timing")]
    public float endPause = 0.08f;

    public bool IsPlaying { get; private set; }

    private void Awake()
    {
        AutoAssignTargets();
    }

    public IEnumerator PlayRollAnimationRoutine()
    {
        AutoAssignTargets();

        if (IsPlaying)
        {
            yield break;
        }

        IsPlaying = true;

        Vector3 machineStartLocalPosition = Vector3.zero;
        Quaternion handleStartLocalRotation = Quaternion.identity;

        if (machineRoot != null)
        {
            machineStartLocalPosition = machineRoot.localPosition;
        }

        if (handleKnob != null)
        {
            handleStartLocalRotation = handleKnob.localRotation;
        }

        float totalDuration = Mathf.Max(handleDuration, shakeDuration);
        float timer = 0f;

        while (timer < totalDuration)
        {
            timer += Time.deltaTime;

            AnimateHandle(timer, handleStartLocalRotation);
            AnimateMachineShake(timer, machineStartLocalPosition);

            yield return null;
        }

        if (machineRoot != null)
        {
            machineRoot.localPosition = machineStartLocalPosition;
        }

        if (handleKnob != null)
        {
            handleKnob.localRotation = handleStartLocalRotation;
        }

        if (endPause > 0f)
        {
            yield return new WaitForSeconds(endPause);
        }

        IsPlaying = false;
    }

    public void PlayRollAnimation()
    {
        StartCoroutine(PlayRollAnimationRoutine());
    }

    private void AnimateHandle(float timer, Quaternion startRotation)
    {
        if (handleKnob == null)
        {
            return;
        }

        if (handleDuration <= 0f)
        {
            handleKnob.localRotation = startRotation;
            return;
        }

        float progress = Mathf.Clamp01(timer / handleDuration);
        float easedProgress = EaseOutBack(progress);

        float angle = handleRotationDegrees * easedProgress;
        handleKnob.localRotation = startRotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void AnimateMachineShake(float timer, Vector3 startPosition)
    {
        if (machineRoot == null)
        {
            return;
        }

        if (shakeDuration <= 0f)
        {
            machineRoot.localPosition = startPosition;
            return;
        }

        float progress = Mathf.Clamp01(timer / shakeDuration);
        float power = 1f - progress;

        float offsetX = Mathf.Sin(timer * shakeSpeed) * shakeAmount * power;
        float offsetY = Mathf.Sin(timer * shakeSpeed * 1.37f) * shakeAmount * 0.55f * power;

        machineRoot.localPosition = startPosition + new Vector3(offsetX, offsetY, 0f);
    }

    private float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }

    private void AutoAssignTargets()
    {
        if (machineRoot == null)
        {
            machineRoot = transform;
        }

        if (handleKnob == null)
        {
            Transform foundHandle = FindChildByName(transform, "HandleKnob");

            if (foundHandle != null)
            {
                handleKnob = foundHandle;
            }
        }
    }

    private Transform FindChildByName(Transform parent, string targetName)
    {
        if (parent.name == targetName)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindChildByName(parent.GetChild(i), targetName);

            if (result != null)
            {
                return result;
            }
        }

        return null;
    }
}