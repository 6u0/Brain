using UnityEngine;
using System.Collections;

[RequireComponent(typeof(OVRSpatialAnchor))]
public class SpatialAnchorManager : MonoBehaviour
{
    [Header("▼ ここにGameManagerオブジェクトを登録")]
    public ExperienceManager gameManager;

    private OVRSpatialAnchor spatialAnchor;
    private const string PREF_KEY_UUID = "ChairAnchorUUID";

    void Start()
    {
        spatialAnchor = GetComponent<OVRSpatialAnchor>();
        
        // 起動時に自動で読み込みを試みる
        StartCoroutine(WaitAndLoadAnchor());
    }

    IEnumerator WaitAndLoadAnchor()
    {
        yield return new WaitForSeconds(1.0f);
        LoadAndAlignAnchor();
    }

    // 設営時用：Aボタンで保存
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.Button.One)) 
        {
            SaveCurrentAnchor();
        }
    }

    [ContextMenu("★ 現在の位置を『椅子の位置』として保存")]
    public void SaveCurrentAnchor()
    {
        if (spatialAnchor == null) spatialAnchor = GetComponent<OVRSpatialAnchor>();

        spatialAnchor.Save((anchor, success) => {
            if (success) {
                PlayerPrefs.SetString(PREF_KEY_UUID, anchor.Uuid.ToString());
                Debug.Log("<color=green>【成功】椅子の位置を現実空間に保存しました！</color>");
            } else {
                Debug.LogError("アンカーの保存に失敗。明るい場所でやり直してください。");
            }
        });
    }

    public void LoadAndAlignAnchor()
    {
        string savedUuidStr = PlayerPrefs.GetString(PREF_KEY_UUID);
        
        if (string.IsNullOrEmpty(savedUuidStr)) 
        {
            Debug.LogWarning("保存された座標がありません。設営時にAボタンで保存してください。");
            return;
        }

        var uuid = new System.Guid(savedUuidStr);
        OVRSpatialAnchor.LoadUnboundAnchors(new OVRSpatialAnchor.LoadOptions {
            Uuids = new[] { uuid }
        }, (anchors) => {
            if (anchors != null && anchors.Length > 0) {
                anchors[0].BindTo(spatialAnchor);
                Debug.Log("<color=green>【成功】現実の椅子とVRの座標が合致しました！</color>");
                
                // 座標合わせが終わったら、司令塔に「次（フェーズ1）へ進め」と命令
                if (gameManager.currentPhase == ExperienceManager.Phase.Setup)
                {
                    gameManager.GoToNextPhase();
                }
            }
        });
    }
}