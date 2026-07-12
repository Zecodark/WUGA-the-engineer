using UnityEngine;

public class PortalLevel1 : MonoBehaviour
{
    [Header("Pengaturan Portal")]
    [Tooltip("Pilih quest yang harus diselesaikan untuk membuka portal. Kosongkan jika quest apa pun boleh.")]
    [SerializeField] private QuestData requiredQuest;
    
    [Tooltip("Masukkan Game Object portal (visual dan collider-nya) ke sini. Jangan masukkan object ini sendiri agar script tetap berjalan.")]
    [SerializeField] private GameObject portalObject;

    [Tooltip("Trigger pada PortalEffect yang menampilkan panel hasil.")]
    [SerializeField] private PortalFinishTrigger finishTrigger;

    void Start()
    {
        ResolveFinishTrigger();

        // Menyembunyikan portal saat game baru mulai
        if (portalObject != null)
        {
            portalObject.SetActive(false);
            finishTrigger?.SetPortalUnlocked(false);
        }
        else
        {
            Debug.LogWarning("[PortalLevel1] Portal Object belum dimasukkan di Inspector!");
        }

        // Mendaftarkan event ketika quest selesai
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted += CheckPortalUnlock;
        }
    }

    private void CheckPortalUnlock(QuestData completedQuest)
    {
        // Jika requiredQuest diisi, pastikan yang selesai adalah quest tersebut.
        // Jika tidak diisi, portal akan terbuka pada quest apa saja.
        if (requiredQuest == null || completedQuest == requiredQuest)
            UnlockPortal();
    }

    public void UnlockPortal()
    {
        if (portalObject == null)
        {
            Debug.LogWarning(
                "[PortalLevel1] Portal Object belum dimasukkan.",
                this
            );
            return;
        }

        portalObject.SetActive(true);
        ResolveFinishTrigger();
        finishTrigger?.SetPortalUnlocked(true);
        Debug.Log("[PortalLevel1] Portal terbuka!", this);
    }

    private void ResolveFinishTrigger()
    {
        if (finishTrigger != null || portalObject == null)
            return;

        finishTrigger =
            portalObject.GetComponent<PortalFinishTrigger>();
    }

    private void OnDestroy()
    {
        // Jangan lupa menghapus event saat object hancur/ganti scene agar tidak memori bocor
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestCompleted -= CheckPortalUnlock;
        }
    }
}
