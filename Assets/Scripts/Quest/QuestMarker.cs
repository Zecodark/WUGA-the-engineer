using UnityEngine;

public class QuestMarker : MonoBehaviour
{
    [SerializeField] private GameObject markerPrefab;
    [SerializeField] private Transform[] itemPosition;
    
    private GameObject currentMarker;
    
    public void ShowMarker (int itemIndex)
    {
        if(currentMarker != null)
            Destroy(currentMarker);

        if (itemIndex >= 0 && itemIndex < itemPosition.Length)
        {
            currentMarker = Instantiate(markerPrefab, 
            itemPosition[itemIndex].position + Vector3.up * 2f, Quaternion.identity);
        }
    }


    public void HideMarker()
    {
        if (currentMarker != null)
        Destroy(currentMarker);
    }
}
