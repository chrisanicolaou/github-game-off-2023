using Godot;
using System;
using System.Threading.Tasks;

public class Player : KinematicBody2D
{
    [Export] private float _speed = 1;
    [Export] private float _jumpSpeed = 1;
    [Export] private float _jumpFloatyness = 0.1f;
    [Export] private float _gravityMultiplier = 1;
    [Export] private float _elongatedJumpMultiplier = 25;
    [Export] private float _snappyFallMultiplier = 25;
    [Export] private NodePath _animPlayerPath;
    [Export] private NodePath _spriteNodePath;
    
    private Vector2 _velocity;
    private float _gravity = Convert.ToInt32(ProjectSettings.GetSetting("physics/2d/default_gravity"));
    private float _timeInAir;
    private AnimationPlayer _animPlayer;
    private Sprite _sprite;
    
    public bool Freeze { get; set; }

    public override void _Ready()
    {
        _animPlayer = GetNode<AnimationPlayer>(_animPlayerPath);
        _sprite = GetNode<Sprite>(_spriteNodePath);
        _animPlayer.Play("idle");
    }

    public async Task EnterLevel(Door door)
    {
        var playerScale = 0.5f;
        var exitDoorDuration = 0.5f;
        
        GlobalPosition = door.GlobalPosition;
        var playerOriginalAlpha = Modulate.a;
        Modulate = new Color(Modulate.r, Modulate.g, Modulate.b, 0);
        var playerOriginalScale = Scale;
        Scale = new Vector2(playerScale, playerScale);
        
        PlayAnimationIfNotPlaying("enter_level");
        var tween = GetTree().CreateTween().SetParallel();
        tween.TweenProperty(this, "scale", playerOriginalScale, exitDoorDuration);
        tween.TweenProperty(this, "modulate:a", playerOriginalAlpha, exitDoorDuration);
        await ToSignal(tween, "finished");
        PlayAnimationIfNotPlaying("idle");
    }
    
    public async Task ExitLevel(Door door)
    {
        var playerScale = 0.5f;
        var exitDoorDuration = 0.5f;
        
        GlobalPosition = door.GlobalPosition;
        
        PlayAnimationIfNotPlaying("exit_level");
        var tween = GetTree().CreateTween().SetParallel();
        tween.TweenProperty(this, "scale", new Vector2(playerScale, playerScale), exitDoorDuration);
        tween.TweenProperty(this, "modulate:a", 0f, exitDoorDuration);
        await ToSignal(tween, "finished");
    }

    public override void _PhysicsProcess(float delta)
    {
        if (Freeze) return;
        
        var direction = Input.GetAxis("left", "right");
        if (direction > 0.001) _sprite.FlipH = false;
        if (direction < -0.001) _sprite.FlipH = true;

        if (IsOnFloor())
        {
            PlayAnimationIfNotPlaying(Mathf.Abs(direction) > 0.001 ? "walk" : "idle");
        }
        
        _velocity.x = direction * _speed;

        MoveAndSlide(_velocity, Vector2.Up);

        if (!IsOnFloor())
        {
            _timeInAir += delta;
            if (IsOnCeiling()) _velocity.y = 0;
            _velocity.y += _gravity * _gravityMultiplier * delta;
            if (_velocity.y < 0)
            {
                if (Input.IsActionPressed("jump")) _velocity.y -= _elongatedJumpMultiplier * delta;
                _velocity.y += (1 / _jumpFloatyness) * _timeInAir;
            }
            else
            {
                _velocity.y += _snappyFallMultiplier * delta;
            }
        }
        else
        {
            _timeInAir = 0;
            _velocity.y = Input.IsActionPressed("jump") ? -_jumpSpeed : 0;
        }
    }

    private void PlayAnimationIfNotPlaying(string animName)
    {
        if (_animPlayer.CurrentAnimation == animName) return;
        _animPlayer.Play(animName);
    }
}
