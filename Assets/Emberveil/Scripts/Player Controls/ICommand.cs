public interface ICommand
{
    void Execute();
}

public class LightAttackCommand : ICommand
{
    private PlayerAttacker playerAttacker;

    public LightAttackCommand(PlayerAttacker playerAttacker)
    {
        this.playerAttacker = playerAttacker;
    }

    public void Execute()
    {
        playerAttacker.HandleLightAttackButtonPressed();
    }
}

public class HeavyAttackCommand : ICommand
{
    private PlayerAttacker playerAttacker;

    public HeavyAttackCommand(PlayerAttacker playerAttacker)
    {
        this.playerAttacker = playerAttacker;
    }

    public void Execute()
    {
        playerAttacker.HandleHeavyAttackButtonPressed();
    }
}

public class JumpCommand : ICommand
{
    private PlayerLocomotion playerLocomotion;

    public JumpCommand(PlayerLocomotion playerLocomotion)
    {
        this.playerLocomotion = playerLocomotion;
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

    public DodgeCommand(PlayerLocomotion playerLocomotion, bool isPressed)
    {
        this.playerLocomotion = playerLocomotion;
        this.isPressed = isPressed;
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