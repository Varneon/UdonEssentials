#if !UDONSHARP_COMPILER && UNITY_EDITOR

using UdonSharp;
using UnityEngine;
using VRC.SDKBase;

public class GroupsUsageExample : UdonSharpBehaviour
{
    [SerializeField]
    private Varneon.UdonPrefabs.Essentials.Groups groups;

    public override void OnPlayerJoined(VRCPlayerApi player)
    {
        string displayName = player.displayName;

        // Get the group indices of the player based on the displayName of the player
        int[] groupIndices = groups._GetGroupIndicesOfPlayer(displayName);

        // Iterate through every group that the player is part of
        foreach (int index in groupIndices)
        {
            // The following information can be fetched from groups based on the index of the group
            string groupName = groups._GetGroupName(index);
            Sprite groupIcon = groups._GetGroupIcon(index);
            string groupArgs = groups._GetGroupArguments(index);

            Debug.Log($"{displayName} <color=silver>Is part of group:</color> {groupName}\n<color=silver>Group icon:</color> {(groupIcon != null ? groupIcon.name : "null")}\n<color=silver>Group arguments:</color> {groupArgs}");
        }
    }
}

#endif
