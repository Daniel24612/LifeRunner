using UnityEngine;
using AudioSystem;
using System.Collections.Generic;
using Player.Movement;
using System;
using VContainer;

public class PlayerSFX : MonoBehaviour
{
    [Inject] private SoundManager soundManager;
    [SerializeField] private PlayerMovementStatus _status;
    [Header("FootSteps")]
    [SerializeField] private SoundData footStepSound;
    [SerializeField] private float walkFootstepsCountPS;
    [SerializeField] private float runFootstepsCountPS;
    [Header("Jump")]
    [SerializeField] private SoundData jumpSound;
    [SerializeField] private Vector2 jumpRandomPitch;
    [Header("Slide")]
    [SerializeField] private SoundData slideSound;
    
    private bool _isFootstepEnabled = false;
    private Vector3 _lastPosition;

    private SoundEmitter _slideSoundEmitter;

    private SoundBuilder _soundBuilder;
    private Dictionary<MovementSounds, SoundData> _sounds;

    private void Awake()
    {
        _lastPosition = transform.position;
        Initialize();
    }
    private void FixedUpdate()
    {
        if(_isFootstepEnabled)
            FootStepUpdate();
    }
    public void Initialize()
    {
        _sounds = new Dictionary<MovementSounds, SoundData>();

        _sounds.Add(MovementSounds.FootStep, footStepSound);
        _sounds.Add(MovementSounds.Jump, jumpSound);

        _soundBuilder = new SoundBuilder(soundManager);

        _status.PlayerJumped += OnJump;
    }
    private void FootStepUpdate()
    {

    }
    void OnMovmentStateChanged(Type type)
    {
        _isFootstepEnabled = type == typeof(StandState) || type == typeof(WallRunState);
        SetSlideSound(type ==  typeof(SlideState) || type == typeof(WallRunState)); 
    }
    void OnJump()
    {
        PlayJump();
    }

    /// <summary>
    /// Before calling this method you should apply cleanup and random pitch to the builder if desired.
    /// </summary>
    /// <param name="movementSound"></param>
    private void PlayConcreteSound(MovementSounds movementSound)
    {
        _soundBuilder
            .WithSoundData(_sounds[movementSound])
            .Play();
    }
    public void PlayFootStep()
    {
        _soundBuilder
            .ClearData()
            .WithPosition(transform.position)
            .WithRandomPitch();
        PlayConcreteSound(MovementSounds.FootStep);
    }
    public void PlayJump()
    {
        _soundBuilder
            .ClearData()
            .WithPosition(transform.position)
            .WithRandomPitch(jumpRandomPitch.x, jumpRandomPitch.y);
        PlayConcreteSound(MovementSounds.Jump);
    }
    public void SetSlideSound(bool isActive)
    {
        if (isActive)
        {
            _soundBuilder
                .WithPosition(transform.position)
                .WithSoundData(slideSound)
                .WithParent(transform)
                .Play();
            _slideSoundEmitter = _soundBuilder.GetLastEmitter();
        }
        else
        {
            if (_slideSoundEmitter != null && _slideSoundEmitter.Data == slideSound)
                _slideSoundEmitter.Stop();
        }
    }
    private enum MovementSounds
    {
        FootStep = 0,
        Jump = 1,
    }
}