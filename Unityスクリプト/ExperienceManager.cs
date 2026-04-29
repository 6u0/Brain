using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class ExperienceManager : MonoBehaviour
{
    public enum Phase
    {
        Wait,
        Intro,
        Contact,
        Throw,
        Erosion,
        Ending,
        Complete
    }

    [Header("Phase")]
    [InspectorName("Current Phase")]
    public Phase currentPhase = Phase.Wait;

    public event Action<Phase> PhaseChanged;

    [Header("Settings")]
    [SerializeField, InspectorName("Start On Play")]
    private bool startOnPlay = false;

    [SerializeField, InspectorName("Brain Throw Velocity Threshold")]
    private float brainThrowVelocityThreshold = 5.0f;

    [Header("Durations")]
    [SerializeField, InspectorName("Intro Duration Seconds")]
    private float introDurationSeconds = 20.0f;

    [SerializeField, InspectorName("Contact Timeout Seconds")]
    private float contactTimeoutSeconds = 70.0f;

    [SerializeField, InspectorName("Throw Blackout Seconds")]
    private float throwBlackoutSeconds = 15.0f;

    [SerializeField, InspectorName("Erosion Duration Seconds")]
    private float erosionDurationSeconds = 45.0f;

    [SerializeField, InspectorName("Ending Fallback Seconds")]
    private float endingFallbackSeconds = 20.0f;

    [Header("Phase Events")]
    [SerializeField] private UnityEvent onWaitEnter;
    [SerializeField] private UnityEvent onIntroEnter;
    [SerializeField] private UnityEvent onContactEnter;
    [SerializeField] private UnityEvent onThrowEnter;
    [SerializeField] private UnityEvent onErosionEnter;
    [SerializeField] private UnityEvent onEndingEnter;
    [SerializeField] private UnityEvent onCompleteEnter;

    [Header("Debug")]
    [SerializeField, InspectorName("Allow Keyboard Debug")]
    private bool allowKeyboardDebug = true;

    [SerializeField, InspectorName("Start Key")]
    private KeyCode startKey = KeyCode.Return;

    [SerializeField, InspectorName("Next Phase Key")]
    private KeyCode nextPhaseKey = KeyCode.Space;

    [SerializeField, InspectorName("Previous Phase Key")]
    private KeyCode prevPhaseKey = KeyCode.Backspace;

    [Header("References")]
    [SerializeField, InspectorName("Operation Panel OSC")]
    private OperationPanelOSC operationPanelOsc;

    [SerializeField, InspectorName("Phase Debug TMP")]
    private TMP_Text phaseDebugText;

    private bool hasRequestedOperationPanelPreparation;

    private void Awake()
    {
        EnsureOperationPanelReference();
        UpdatePhaseDebugText();
    }

    private void Start()
    {
        ChangePhase(startOnPlay ? Phase.Intro : Phase.Wait);
    }

    private void Update()
    {
        switch (currentPhase)
        {
            case Phase.Contact:
                UpdateContactPhase();
                break;
            case Phase.Throw:
                UpdateThrowPhase();
                break;
            case Phase.Ending:
                UpdateEndingPhase();
                break;
        }

        if (!allowKeyboardDebug)
        {
            return;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[(Key)startKey].wasPressedThisFrame)
        {
            OnStartButtonPressed();
        }

        if (keyboard[(Key)nextPhaseKey].wasPressedThisFrame)
        {
            ForceNextPhase();
        }

        if (keyboard[(Key)prevPhaseKey].wasPressedThisFrame)
        {
            ForcePrevPhase();
        }
    }

    public void ChangePhase(Phase newPhase)
    {
        if (currentPhase == newPhase)
        {
            UpdatePhaseDebugText();
            return;
        }

        if (currentPhase == Phase.Wait && newPhase != Phase.Wait)
        {
            EnsureOperationPanelPrepared();
        }

        currentPhase = newPhase;
        Debug.Log($"[ExperienceManager] Phase changed: {currentPhase}");

        StopAllCoroutines();

        UpdatePhaseDebugText();
        PhaseChanged?.Invoke(currentPhase);
        InvokeEnterHook(currentPhase);

        switch (currentPhase)
        {
            case Phase.Wait:
                StartCoroutine(Routine_Wait());
                break;
            case Phase.Intro:
                StartCoroutine(Routine_Intro());
                break;
            case Phase.Contact:
                StartCoroutine(Routine_Contact());
                break;
            case Phase.Throw:
                StartCoroutine(Routine_Throw());
                break;
            case Phase.Erosion:
                StartCoroutine(Routine_Erosion());
                break;
            case Phase.Ending:
                StartCoroutine(Routine_Ending());
                break;
            case Phase.Complete:
                StartCoroutine(Routine_Complete());
                break;
        }
    }

    public void OnStartButtonPressed()
    {
        EnsureOperationPanelPrepared();

        if (currentPhase == Phase.Wait)
        {
            ChangePhase(Phase.Intro);
            return;
        }

        ForceNextPhase();
    }

    public void ForceNextPhase()
    {
        int next = Mathf.Min((int)currentPhase + 1, Enum.GetValues(typeof(Phase)).Length - 1);
        ChangePhase((Phase)next);
    }

    public void ForcePrevPhase()
    {
        int prev = Mathf.Max((int)currentPhase - 1, 0);
        ChangePhase((Phase)prev);
    }

    public string GetPhaseLabel(Phase phase)
    {
        switch (phase)
        {
            case Phase.Wait:
                return "Wait";
            case Phase.Intro:
                return "Intro";
            case Phase.Contact:
                return "Contact";
            case Phase.Throw:
                return "Throw";
            case Phase.Erosion:
                return "Erosion";
            case Phase.Ending:
                return "Ending";
            case Phase.Complete:
                return "Complete";
            default:
                return "Unknown";
        }
    }

    private IEnumerator Routine_Wait()
    {
        Debug.Log("[ExperienceManager] Wait phase.");
        yield break;
    }

    private IEnumerator Routine_Intro()
    {
        Debug.Log("[ExperienceManager] Intro phase.");
        yield return new WaitForSeconds(introDurationSeconds);
        ChangePhase(Phase.Contact);
    }

    private IEnumerator Routine_Contact()
    {
        Debug.Log("[ExperienceManager] Contact phase.");
        yield return new WaitForSeconds(contactTimeoutSeconds);

        if (currentPhase == Phase.Contact)
        {
            ChangePhase(Phase.Throw);
        }
    }

    private IEnumerator Routine_Throw()
    {
        Debug.Log("[ExperienceManager] Throw phase.");
        yield return new WaitForSeconds(throwBlackoutSeconds);
        ChangePhase(Phase.Erosion);
    }

    private IEnumerator Routine_Erosion()
    {
        Debug.Log("[ExperienceManager] Erosion phase.");
        yield return new WaitForSeconds(erosionDurationSeconds);
        ChangePhase(Phase.Ending);
    }

    private IEnumerator Routine_Ending()
    {
        Debug.Log("[ExperienceManager] Ending phase.");

        float elapsed = 0.0f;
        while (elapsed < endingFallbackSeconds)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (currentPhase == Phase.Ending)
        {
            ChangePhase(Phase.Complete);
        }
    }

    private IEnumerator Routine_Complete()
    {
        Debug.Log("[ExperienceManager] Complete phase.");
        yield break;
    }

    private void UpdateContactPhase()
    {
        _ = brainThrowVelocityThreshold;
    }

    private void UpdateThrowPhase()
    {
    }

    private void UpdateEndingPhase()
    {
    }

    public void NotifyBrainThrown()
    {
        if (currentPhase == Phase.Contact)
        {
            ChangePhase(Phase.Throw);
        }
    }

    public void NotifyHmdRemoved()
    {
        if (currentPhase == Phase.Ending)
        {
            ChangePhase(Phase.Complete);
        }
    }

    private void InvokeEnterHook(Phase phase)
    {
        switch (phase)
        {
            case Phase.Wait:
                onWaitEnter?.Invoke();
                break;
            case Phase.Intro:
                onIntroEnter?.Invoke();
                break;
            case Phase.Contact:
                onContactEnter?.Invoke();
                break;
            case Phase.Throw:
                onThrowEnter?.Invoke();
                break;
            case Phase.Erosion:
                onErosionEnter?.Invoke();
                break;
            case Phase.Ending:
                onEndingEnter?.Invoke();
                break;
            case Phase.Complete:
                onCompleteEnter?.Invoke();
                break;
        }
    }

    private void EnsureOperationPanelPrepared()
    {
        EnsureOperationPanelReference();

        if (operationPanelOsc == null)
        {
            Debug.LogWarning("[ExperienceManager] OperationPanelOSC was not found.");
            return;
        }

        operationPanelOsc.PrepareOperationPanel();
        if (!hasRequestedOperationPanelPreparation)
        {
            hasRequestedOperationPanelPreparation = true;
            Debug.Log("[ExperienceManager] Operation panel preparation requested.");
        }
    }

    private void EnsureOperationPanelReference()
    {
        if (operationPanelOsc != null)
        {
            return;
        }

        operationPanelOsc = FindFirstObjectByType<OperationPanelOSC>();
        if (operationPanelOsc != null)
        {
            Debug.Log("[ExperienceManager] OperationPanelOSC was assigned automatically.");
        }
    }

    private void UpdatePhaseDebugText()
    {
        if (phaseDebugText == null)
        {
            return;
        }

        phaseDebugText.text = $"Phase: {GetPhaseLabel(currentPhase)}";
    }
}
