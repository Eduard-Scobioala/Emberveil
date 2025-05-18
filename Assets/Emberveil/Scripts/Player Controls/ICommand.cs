using System;

public interface ICommand
{
    bool CanExecute();
    void Execute();
}

public class LightAttackCommand : ICommand
{
    private readonly PlayerAttacker playerAttacker;
    private readonly Func<(bool, bool)> executeParams;

    public LightAttackCommand(PlayerAttacker playerAttacker, Func<(bool, bool)> executeParams)
    {
        this.playerAttacker = playerAttacker;
        this.executeParams = executeParams;
    }

    public bool CanExecute()
    {
        var (isInMidAction, canDoCombo) = executeParams();
        return !isInMidAction || canDoCombo; // you are allowed to instant attack for combos
    }

    public void Execute()
    {
        playerAttacker.HandleLightAttackButtonPressed();
    }
}

public class HeavyAttackCommand : ICommand
{
    private readonly PlayerAttacker playerAttacker;
    private readonly Func<(bool, bool)> executeParams;

    public HeavyAttackCommand(PlayerAttacker playerAttacker, Func<(bool, bool)> executeParams)
    {
        this.playerAttacker = playerAttacker;
        this.executeParams = executeParams;
    }

    public bool CanExecute()
    {
        var (isInMidAction, canDoCombo) = executeParams();
        return !isInMidAction || canDoCombo; // you are allowed to instant attack for combos
    }

    public void Execute()
    {
        playerAttacker.HandleHeavyAttackButtonPressed();
    }
}

public class JumpCommand : ICommand
{
    private readonly PlayerLocomotion playerLocomotion;
    private readonly Func<bool> isInMidAction;

    public JumpCommand(PlayerLocomotion playerLocomotion, Func<bool> isInMidAction)
    {
        this.playerLocomotion = playerLocomotion;
        this.isInMidAction = isInMidAction;
    }

    public bool CanExecute()
    {
        return !isInMidAction();
    }

    public void Execute()
    {
        playerLocomotion.HandleJumpButtonPressed();
    }
}

public class DodgeCommand : ICommand
{
    private readonly PlayerLocomotion playerLocomotion;
    private readonly bool isPressed;
    private readonly Func<bool> isInMidAction;

    public DodgeCommand(PlayerLocomotion playerLocomotion, bool isPressed, Func<bool> isInMidAction)
    {
        this.playerLocomotion = playerLocomotion;
        this.isPressed = isPressed;
        this.isInMidAction = isInMidAction;
    }

    public bool CanExecute()
    {
        return !isInMidAction();
    }

    public void Execute()
    {
        if (isPressed)
        {
            playerLocomotion.HandleDodgeButtonPressed();
        }
        else
        {
            playerLocomotion.HandleDodgeButtonReleased();
        }
    }
}