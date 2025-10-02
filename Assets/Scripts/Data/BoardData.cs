using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "BoardData", menuName = "GamePlay/BoardData", order = 0)]
    public class BoardData : ScriptableObject
    {
        [Range(3,15)] public int edgeSize;
        
                
    }
}