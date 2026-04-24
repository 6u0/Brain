using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using TMPro; // 文字を綺麗に表示する機能を追加！

public class ExperienceManager : MonoBehaviour
{
    public enum Phase
    {
        Setup = 0,
        Intro = 1,
        Contact = 2,
        Throw = 3,
        Sleepwalk = 4,
        Ending = 5
    }

    [Header("▼ 現在のステータス（プレイ中に確認・変更可能）")]
    public Phase currentPhase = Phase.Setup;

    [Space(10)]
    [Header("▼ 開発用デバッグ機能")]
    [Tooltip("チェックを入れると、下のフェーズからゲームが開始されます")]
    public bool useDebugStart = false;
    public Phase debugStartPhase = Phase.Setup;

    // ＝＝＝ 追加：VR内デバッグモニター ＝＝＝
    [Space(10)]
    [Header("▼ VR内デバッグモニター（任意）")]
    [Tooltip("VR空間に文字を出すテキストUI（TextMeshPro）を登録します")]
    public TextMeshProUGUI debugMonitorText;
    // ＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

    [Space(10)]
    [Header("▼ 疑似・後ろ倒れギミック（フェーズ3用）")]
    public Transform xrOrigin; 
    public float tiltAngle = -25f;
    public float tiltDuration = 0.15f;

    [Space(10)]
    [Header("▼ 各フェーズ開始時に実行する演出リスト（＋ボタンで追加）")]
    [Header("0. 待機・座標合わせ完了")] public UnityEvent onSetupStart;
    [Header("1. 導入：正木教授の語り")] public UnityEvent onIntroStart;
    [Header("2. 接触：脳を掴む")] public UnityEvent onContactStart;
    [Header("3. 破棄：脳を投げる")] public UnityEvent onThrowStart;
    [Header("4. 夢中遊行：SR映像")] public UnityEvent onSleepwalkStart;
    [Header("5. 終焉：液体に溶ける")] public UnityEvent onEndingStart;

    void Start()
    {
        currentPhase = useDebugStart ? debugStartPhase : Phase.Setup;
        ExecutePhaseLogic();
    }

    public void GoToNextPhase()
    {
        if (currentPhase == Phase.Ending) return; 
        currentPhase++;
        ExecutePhaseLogic();
    }

    [ContextMenu("★ 強制的に『現在のフェーズ(currentPhase)』を実行")]
    public void ForceExecuteCurrentPhase()
    {
        ExecutePhaseLogic();
    }

    void ExecutePhaseLogic()
    {
        Debug.Log($"<color=cyan>[ExperienceManager]</color> フェーズ移行: {currentPhase} が開始されました");

        // ＝＝＝ 追加：VR内モニターの文字を更新する ＝＝＝
        if (debugMonitorText != null)
        {
            debugMonitorText.text = $"<color=yellow>[DEBUG]</color>\n現在: {currentPhase}";
        }
        // ＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝＝

        switch (currentPhase)
        {
            case Phase.Setup: onSetupStart.Invoke(); break;
            case Phase.Intro: onIntroStart.Invoke(); break;
            case Phase.Contact: onContactStart.Invoke(); break;
            case Phase.Throw:
                onThrowStart.Invoke();
                StartCoroutine(SimulateFallingBackwards());
                StartCoroutine(WaitAndNext(10f)); 
                break;
            case Phase.Sleepwalk:
                onSleepwalkStart.Invoke();
                StartCoroutine(WaitAndNext(45f)); 
                break;
            case Phase.Ending: onEndingStart.Invoke(); break;
        }
    }

    IEnumerator WaitAndNext(float waitTime)
    {
        yield return new WaitForSeconds(waitTime);
        GoToNextPhase();
    }

    IEnumerator SimulateFallingBackwards()
    {
        if (xrOrigin == null) yield break;

        Quaternion startRotation = xrOrigin.rotation;
        Vector3 startPosition = xrOrigin.position;
        Quaternion targetRotation = startRotation * Quaternion.Euler(tiltAngle, 0, 0);
        Vector3 targetPosition = startPosition + new Vector3(0, -0.1f, -0.1f);
        float elapsedTime = 0f;

        while (elapsedTime < tiltDuration)
        {
            xrOrigin.rotation = Quaternion.Lerp(startRotation, targetRotation, elapsedTime / tiltDuration);
            xrOrigin.position = Vector3.Lerp(startPosition, targetPosition, elapsedTime / tiltDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        xrOrigin.rotation = targetRotation;
        xrOrigin.position = targetPosition;
    }
}
