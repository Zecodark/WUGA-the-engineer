using UnityEngine;

public static class Level2StaticColliderSetup
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AddMissingLevel2Colliders()
    {
        if (GameObject.Find("Level2_System") == null)
            return;

        GameObject environmentRoot = GameObject.Find("Ruang2");
        if (environmentRoot == null)
        {
            Debug.LogWarning("[Level2Collider] Root Ruang2 tidak ditemukan.");
            return;
        }

        MeshFilter[] meshes = environmentRoot.GetComponentsInChildren<MeshFilter>(true);
        int addedCount = 0;

        foreach (MeshFilter meshFilter in meshes)
        {
            if (meshFilter == null || meshFilter.sharedMesh == null)
                continue;

            GameObject target = meshFilter.gameObject;
            if (target.GetComponent<Collider>() != null)
                continue;

            MeshCollider collider = target.AddComponent<MeshCollider>();
            collider.sharedMesh = meshFilter.sharedMesh;
            collider.convex = false;
            addedCount++;
        }

        Debug.Log($"[Level2Collider] Menambahkan {addedCount} collider lingkungan di Ruang2.");
    }
}
