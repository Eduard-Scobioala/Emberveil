using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using DG.Tweening;

public class MenuEventSystemHandler : MonoBehaviour
{
    [Header("References")]
    public List<Selectable> Selectables = new();
    [SerializeField] protected Selectable firstSelected;

    [Header("Controls")]
    [SerializeField] protected InputActionReference navigateReference;

    [Header("Animations")]
    [SerializeField] protected float selectedAnimationScale = 1.1f;
    [SerializeField] protected float scaleDuration = .25f;
    [SerializeField] protected List<GameObject> animationExclusions = new List<GameObject>();

    protected Selectable lastSelected;

    protected Dictionary<Selectable, Vector3> scales = new();

    protected Tween scaleUpTween;
    protected Tween scaleDownTween;

    public virtual void Awake()
    {
        foreach (var selectable in Selectables)
        {
            AddSelectionListeners(selectable);
            scales.Add(selectable, selectable.transform.localScale);
        }
    }

    public virtual void OnEnable()
    {
        navigateReference.action.performed += OnNavigate;

        // ensure all selectables are reset back to original size
        for (int i = 0; i < Selectables.Count; i++)
        {
            Selectables[i].transform.localScale = scales[Selectables[i]];
        }

        StartCoroutine(SelectAfterDelay());
    }

    public virtual IEnumerator SelectAfterDelay()
    {
        yield return null;
        EventSystem.current.SetSelectedGameObject(firstSelected.gameObject);
    }

    public virtual void OnDisable()
    {
        navigateReference.action.performed -= OnNavigate;

        scaleUpTween?.Kill(true);
        scaleDownTween?.Kill(true);
    }

    protected virtual void AddSelectionListeners(Selectable selectable)
    {
        // add listener
        EventTrigger trigger = selectable.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = selectable.gameObject.AddComponent<EventTrigger>();
        }

        // add SELECT event
        EventTrigger.Entry SelectEntry = new()
        {
            eventID = EventTriggerType.Select
        };
        SelectEntry.callback.AddListener(OnSelect);
        trigger.triggers.Add(SelectEntry);

        // add DESELECT event
        EventTrigger.Entry DeselectEntry = new()
        {
            eventID = EventTriggerType.Deselect
        };
        DeselectEntry.callback.AddListener(OnDeselect);
        trigger.triggers.Add(DeselectEntry);

        // add ONPOINTERENTER event
        EventTrigger.Entry PointEnter = new()
        {
            eventID = EventTriggerType.PointerEnter
        };
        PointEnter.callback.AddListener(OnPointEnter);
        trigger.triggers.Add(PointEnter);

        // add ONPOINTEREXIT event
        EventTrigger.Entry PointExit = new()
        {
            eventID = EventTriggerType.PointerExit
        };
        PointExit.callback.AddListener(OnPointExit);
        trigger.triggers.Add(PointExit);
    }

    public void OnSelect(BaseEventData eventData)
    {
        lastSelected = eventData.selectedObject.GetComponent<Selectable>();

        if (animationExclusions.Contains(eventData.selectedObject))
            return;

        Vector3 newScale = eventData.selectedObject.transform.localScale * selectedAnimationScale;
        scaleUpTween = eventData.selectedObject.transform.DOScale(newScale, scaleDuration);
    }

    public void OnDeselect(BaseEventData eventData)
    {
        if (animationExclusions.Contains(eventData.selectedObject))
            return;

        Selectable sel = eventData.selectedObject.GetComponent<Selectable>();
        scaleDownTween = eventData.selectedObject.transform.DOScale(scales[sel], scaleDuration);
    }

    public void OnPointEnter(BaseEventData eventData)
    {
        if (eventData is PointerEventData pointerEventData)
        {
            Selectable sel = pointerEventData.pointerEnter.GetComponentInParent<Selectable>();
            if (sel == null)
            {
                sel = pointerEventData.pointerEnter.GetComponentInChildren<Selectable>();
            }

            if (sel != null)
            {
                pointerEventData.selectedObject = sel.gameObject;
            }
        }
    }

    public void OnPointExit(BaseEventData eventData)
    {
        if (eventData is PointerEventData pointerEventData)
        {
            pointerEventData.selectedObject = null;
        }
    }

    protected virtual void OnNavigate(InputAction.CallbackContext context)
    {
        if (EventSystem.current.currentSelectedGameObject == null & lastSelected != null)
        {
            EventSystem.current.SetSelectedGameObject(lastSelected.gameObject);
        }
    }
}
