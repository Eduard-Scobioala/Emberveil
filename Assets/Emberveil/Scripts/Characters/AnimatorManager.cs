using System.Runtime.CompilerServices;
using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    public Animator anim;

    public bool IsInMidAction
    {
        get => GetAnimatorBool();
        set => SetAnimatorBool(value);
    }

    public bool IsInvulnerable
    {
        get => GetAnimatorBool();
        set => SetAnimatorBool(value);
    }

    protected bool GetAnimatorBool([CallerMemberName] string name = "") => anim.GetBool(name);
    protected void SetAnimatorBool(bool value, [CallerMemberName] string name = "") => anim.SetBool(name, value);

    protected int GetAnimatorInt([CallerMemberName] string name = "") => anim.GetInteger(name);
    protected void SetAnimatorInt(int value, [CallerMemberName] string name = "") => anim.SetInteger(name, value);

    public void PlayTargetAnimation(string targetAnim, bool isInMidAction)
    {
        anim.applyRootMotion = isInMidAction;
        IsInMidAction = isInMidAction;
        anim.CrossFade(targetAnim, 0.1f);
    }

    public void ApplyRootMotion(bool rootMotion)
    {
        anim.applyRootMotion = rootMotion;
    }
}
