using UnityEngine;

public class PortalLevel1 : MonoBehaviour
{
    [Header("Pengaturan Portal")]
    [Tooltip("Pilih quest yang harus diselesaikan untuk membuka portal. Kosongkan jika quest apa pun boleh.")]
    [SerializeField] private QuestData requiredQuest;
    
    [Tooltip("Masukkan Game Object portal (visual dan collider-nya) ke sini. Jangan masukkan object ini sendiri agar script tetap berjalan.")]
    [SerializeField] private GameObject portalObject;

    void Start()
    {
        // Menyembunyikan portal saat game baru mulai
        if (portalObject != null)
        {
            portalObject.SetActive(false);
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
        {
            if (portalObject != null)
            {
                portalObject.SetActive(true); // Memunculkan portal
                Debug.Log("[PortalLevel1] Portal terbuka!");
            }
        }
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
