using UnityEngine;

public class AnimatorManager : MonoBehaviour
{
    public Animator anim;
    
    public void PlayTargetAnimation(string targetAnim, bool isInMidAction)
    {
        anim.applyRootMotion = isInMidAction;
        anim.SetBool("isInMidAction", isInMidAction);
        anim.CrossFade(targetAnim, 0.1f);
    }
}
