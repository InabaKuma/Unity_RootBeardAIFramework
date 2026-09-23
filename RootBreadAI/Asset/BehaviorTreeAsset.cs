using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBehaviorTree", menuName = "AI/Behavior Tree Asset")]
public class BehaviorTreeAsset : ScriptableObject
{
    public string RootNodeGuid;
    public List<BTNodeData> AllNodes = new List<BTNodeData>();
}