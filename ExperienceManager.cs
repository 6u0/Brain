using UnityEngine;
using UnityEngine.Events;
using System.Collections;

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
    [Tooltip("テストを開始したいフェーズを選んでください")]
    public Phase debugStartPhase = Phase.Setup;

    [Space(10)]
    [Header("▼ 疑似・後ろ倒れギミック（フェーズ3用）")]
    [Tooltip("OVRCameraRigの親オブジェクト（XR Originなど）を指定")]
    public Transform xrOrigin; 
    [Tooltip("マイナスの値を入れると上を向く（後ろに倒れる）動きになります")]
    public float tiltAngle = -25f;
    [Tooltip("この秒数かけて倒れます。短いほど衝撃的です")]
    public float tiltDuration = 0.15f;

    [Space(10)]
    [Header("▼ 各フェーズ開始時に実行する演出リスト（＋ボタンで追加）")]
    [Header("0. 待機・座標合わせ完了")]
    public UnityEvent onSetupStart;
    [Header("1. 導入：正木教授の語り")]
    public UnityEvent onIntroStart;
    [Header("2. 接触：脳を掴む")]
    public UnityEvent onContactStart;
    [Header("3. 破棄：脳を投げる・ブラックアウト")]
    public UnityEvent onThrowStart;
    [Header("4. 夢中遊行：SR映像")]
    public UnityEvent onSleepwalkStart;
    [Header("5. 終焉：液体に溶ける")]
    public UnityEvent onEndingStart;

    void Start()
    {
        currentPhase = useDebugStart ? debugStartPhase : Phase.Setup;
        ExecutePhaseLogic();
    }

    // 外部（OSCや当たり判定）から呼ばれる「次へ進む」処理
    public void GoToNextPhase()
    {
        if (currentPhase == Phase.Ending) return; 
        currentPhase++;
        ExecutePhaseLogic();
    }

    // インスペクターを右クリックしてテスト実行するためのボタン
    [ContextMenu("★ 強制的に『現在のフェーズ(currentPhase)』を実行")]
    public void ForceExecuteCurrentPhase()
    {
        ExecutePhaseLogic();
    }

    void ExecutePhaseLogic()
    {
        Debug.Log($"<color=cyan>[ExperienceManager]</color> フェーズ移行: {currentPhase} が開始されました");

        switch (currentPhase)
        {
            case Phase.Setup:
                onSetupStart.Invoke();
                break;

            case Phase.Intro:
                onIntroStart.Invoke();
                break;

            case Phase.Contact:
                onContactStart.Invoke();
                break;

            case Phase.Throw:
                onThrowStart.Invoke();
                StartCoroutine(SimulateFallingBackwards());
                StartCoroutine(WaitAndNext(10f)); // 10秒後に自動で次へ
                break;

            case Phase.Sleepwalk:
                onSleepwalkStart.Invoke();
                StartCoroutine(WaitAndNext(45f)); // 45秒後に自動で次へ
                break;

            case Phase.Ending:
                onEndingStart.Invoke();
                break;
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