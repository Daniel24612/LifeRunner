using UnityEngine;
using UnityEngine.UI;
using PrimeTween;
using Cysharp.Threading.Tasks;
using System;

public class LoadingScreen : MonoBehaviour, IProgress<float>
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private Slider _progressBar;
    [SerializeField] private float _barSpeed = 1f;
    public bool IsActive { get; private set; }
    private float _progress = 0f;
    private void Update()
    {
        if (IsActive && _progressBar.value < _progress)
        {
            _progressBar.value += _barSpeed * Time.deltaTime;
        }
        else if (_progress == 1)
            _progressBar.value = 1;
        else
            _progressBar.value = _progress;
    }
    public async UniTask SetActive(bool active)
    {
        IsActive = active;
        // Плавное появление/исчезновение через PrimeTween
        float targetAlpha = active ? 1f : 0f;
        await Tween.Alpha(_canvasGroup, targetAlpha, 0.5f).ToUniTask();

        if (!active) _progressBar.value = 0; // Сбрасываем прогресс после скрытия
    }
    public void UpdateProgress(float value)
    {
        _progress = value;
        Debug.Log("Slider value has been changed");
    }

    public void Report(float value)
    {
        UpdateProgress(value);
    }
}