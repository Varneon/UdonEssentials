﻿#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using UdonSharp;
using UnityEngine;
using VRC.Udon;
using System.Linq;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.Udon.Common;
using System.Reflection;
using JetBrains.Annotations;

#if !COMPILER_UDONSHARP && UNITY_EDITOR
using UdonSharpEditor;
using UnityEditor;
using System.Collections.Generic;
#endif

namespace Varneon.UdonPrefabs.Essentials
{
    /// <summary>
    /// Simple storage behaviour for adding users into groups that can be fetched during runtime
    /// </summary>
    [DefaultExecutionOrder(-2146483647)]
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class Groups : UdonSharpBehaviour
    {
        /// <summary>
        /// Names of the groups
        /// </summary>
        [SerializeField, HideInInspector]
        private string[] groupNames = new string[0];

        /// <summary>
        /// Icons of the groups
        /// </summary>
        [SerializeField, HideInInspector]
        private Sprite[] groupIcons = new Sprite[0];

        /// <summary>
        /// Arguments of the groups
        /// </summary>
        /// <remarks>
        /// e.g. "-playerlistFrameColor#ABCDEF -noPlayerlistIcon"
        /// </remarks>
        [SerializeField, HideInInspector]
        private string[] groupArguments = new string[0];

        /// <summary>
        /// Line-separated string for storing the list of names that are part of the system, acts as the name lookup
        /// </summary>
        [SerializeField, HideInInspector]
        private string memberList = string.Empty;

        /// <summary>
        /// Matching group indices array for all members declared in memberList
        /// </summary>
        [SerializeField, HideInInspector]
        private int[][] memberGroupIndices = new int[0][];

        /// <summary>
        /// Possible newline characters
        /// </summary>
        private readonly char[] NewlineChars = new char[] { '\n', '\r' };

        /// <summary>
        /// Template for the padding of each display name in line-separated strings
        /// </summary>
        private const string NamePaddingTemplate = "\n{0}\n";

        /// <summary>
        /// Text assets containing the display names for each player in the groups
        /// </summary>
        [SerializeField, HideInInspector]
        private TextAsset[] groupUsernames = new TextAsset[0];

        /// <summary>
        /// Get indices of the groups that the player with the provided name is part of
        /// </summary>
        /// <param name="displayName">Display name of the player</param>
        /// <returns>Int32 array containing all of the indices for the groups</returns>
        [PublicAPI]
        public int[] _GetGroupIndicesOfPlayer(string displayName)
        {
            int lookupPos = GetPlayerLookupIndex(displayName);

            if (lookupPos < 0) { return new int[0]; }

            return memberGroupIndices[memberList.Substring(0, lookupPos).Split(NewlineChars).Length - 1];
        }

        /// <summary>
        /// Get the icon of a group
        /// </summary>
        /// <remarks>
        /// Returns null if index was outside of range or group doesn't have an icon
        /// </remarks>
        /// <param name="groupIndex">Index of the group</param>
        /// <returns>Icon of the group as Sprite</returns>
        [PublicAPI]
        public Sprite _GetGroupIcon(int groupIndex)
        {
            if (!IsIndexWithinArrayRange(groupIndex, groupIcons)) { return null; }

            return groupIcons[groupIndex];
        }

        /// <summary>
        /// Get the name of a group
        /// </summary>
        /// <remarks>
        /// Returns string.Empty if index was outside of range
        /// </remarks>
        /// <param name="groupIndex">Index of the group</param>
        /// <returns>Name of the group</returns>
        [PublicAPI]
        public string _GetGroupName(int groupIndex)
        {
            if (!IsIndexWithinArrayRange(groupIndex, groupNames)) { return string.Empty; }

            return groupNames[groupIndex];
        }

        /// <summary>
        /// Get the arguments of a group
        /// </summary>
        /// <remarks>
        /// Returns string.Empty if index was outside of range
        /// </remarks>
        /// <param name="groupIndex">Index of the group</param>
        /// <returns>Arguments of a group</returns>
        [PublicAPI]
        public string _GetGroupArguments(int groupIndex)
        {
            if (!IsIndexWithinArrayRange(groupIndex, groupArguments)) { return string.Empty; }

            return groupArguments[groupIndex];
        }

        /// <summary>
        /// Get the names of players in a group. Doesn't return players added via _AddPlayersToGroup()
        /// </summary>
        /// <remarks>
        /// Returns empty array if index was outside of range
        /// </remarks>
        /// <param name="groupIndex"></param>
        /// <returns>String array containing the names of all of the players in a group</returns>
        [PublicAPI]
        public string[] _GetPlayerNamesInGroup(int groupIndex)
        {
            if (!IsIndexWithinArrayRange(groupIndex, groupUsernames)) { return new string[0]; }

            return groupUsernames[groupIndex].text.Split(NewlineChars, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Runtime method for adding players manually to a group
        /// </summary>
        /// <param name="groupName">Name of the group</param>
        /// <param name="displayNames">Display names of the players</param>
        [PublicAPI]
        public void _AddPlayersToGroup(string groupName, string[] displayNames)
        {
            int groupIndex = 0;

            for (int i = 0; i < groupNames.Length; i++)
            {
                if (groupNames[i].Equals(groupName))
                {
                    groupIndex = i;

                    break;
                }
            }

            foreach (string displayName in displayNames)
            {
                AddPlayerToGroup(groupIndex, displayName);
            }
        }

        /// <summary>
        /// Add a player to a group
        /// </summary>
        /// <param name="groupIndex">Index of the group</param>
        /// <param name="displayName">Display name of the player</param>
        private void AddPlayerToGroup(int groupIndex, string displayName)
        {
            int playerLookupIndex = GetPlayerLookupIndex(displayName);

            if (playerLookupIndex < 0)
            {
                memberList += $"{displayName}\n";

                int memberCount = memberGroupIndices.Length;
                int[][] tempMemberGroupIndices = new int[memberCount + 1][];
                memberGroupIndices.CopyTo(tempMemberGroupIndices, 0);
                memberGroupIndices = tempMemberGroupIndices;
                memberGroupIndices[memberCount] = new int[] { groupIndex };
            }
            else
            {
                int[] indices = memberGroupIndices[playerLookupIndex];
                foreach (int index in indices)
                {
                    if (index.Equals(groupIndex)) { return; }
                }
                int groupCount = indices.Length;
                int[] tempIndices = new int[groupCount + 1];
                indices.CopyTo(tempIndices, 0);
                indices = tempIndices;
                indices[groupCount] = groupIndex;
                memberGroupIndices[playerLookupIndex] = indices;
            }
        }

        /// <summary>
        /// Get the lookup index of a player
        /// </summary>
        /// <param name="displayName">Display name of the player</param>
        /// <returns>Lookup index for finding player's group indices via _GetGroupIndicesOfPlayer()</returns>
        private int GetPlayerLookupIndex(string displayName)
        {
            return memberList.IndexOf(string.Format(NamePaddingTemplate, displayName));
        }

        /// <summary>
        /// Checks whether the index is within the array's range or not
        /// </summary>
        /// <param name="index">Index</param>
        /// <param name="array">Array</param>
        /// <returns>Is the index within the range of the array</returns>
        private bool IsIndexWithinArrayRange(int index, object[] array)
        {
            return index >= 0 && index < array.Length;
        }
    }

    #region Custom Editor
#if !COMPILER_UDONSHARP && UNITY_EDITOR
    [CustomEditor(typeof(Groups))]
    internal class GroupsEditor : Editor
    {
        private static float ThreeLines = EditorGUIUtility.singleLineHeight * 3f + EditorGUIUtility.standardVerticalSpacing * 2f;

        private Groups groupsBehaviour;

        private UdonBehaviour groupsUdonBehaviour;

        private List<Group> groups;

        private bool isDirty;

        private bool isUdonSharpOne;

        private bool isPlayModeActive;

        private bool isPrefabInspector;

        private string groupsPreviewText;

        private bool waitingForPrefabUnpack;

        private bool invalidBehaviour;

        private bool previewGroupsData;

        private const string NamelistPaddingTemplate = "\n{0}\n";

        private struct Group
        {
            internal string Name;
            internal TextAsset Usernames;
            internal Sprite Icon;
            internal string Arguments;
            internal bool Expanded;
        }

        private void OnEnable()
        {
            if (isPlayModeActive = EditorApplication.isPlaying) { return; }

            groupsBehaviour = (Groups)target;

            groupsUdonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(groupsBehaviour);

            if(invalidBehaviour = groupsUdonBehaviour == null)
            {
                return;
            }

            IUdonVariableTable variables = groupsUdonBehaviour.publicVariables;

            isUdonSharpOne = variables.TryGetVariableValue("___UdonSharpBehaviourVersion___", out _);

            if(UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() != null || PrefabUtility.IsPartOfPrefabAsset(groupsBehaviour))
            {
                isPrefabInspector = true;
            }
            else if (PrefabUtility.IsPartOfPrefabInstance(groupsUdonBehaviour))
            {
                waitingForPrefabUnpack = true;
            }

            groups = new List<Group>();

            string[] groupNames;
            Sprite[] groupIcons;
            string[] groupArguments;
            TextAsset[] groupUsernames;

            if (isUdonSharpOne)
            {
                FieldInfo[] fieldInfos = typeof(Groups).GetRuntimeFields().ToArray();

                groupNames = GetVariable(fieldInfos, "groupNames") as string[];
                groupIcons = GetVariable(fieldInfos, "groupIcons") as Sprite[];
                groupArguments = GetVariable(fieldInfos, "groupArguments") as string[];
                groupUsernames = GetVariable(fieldInfos, "groupUsernames") as TextAsset[];
            }
            else
            {
                variables.TryGetVariableValue("groupNames", out groupNames);
                variables.TryGetVariableValue("groupIcons", out groupIcons);
                variables.TryGetVariableValue("groupArguments", out groupArguments);
                variables.TryGetVariableValue("groupUsernames", out groupUsernames);
            }

            for (int i = 0; i < groupNames?.Length; i++)
            {
                groups.Add(new Group() { Name = groupNames[i], Icon = groupIcons[i], Usernames = groupUsernames[i], Arguments = groupArguments[i] });
            }

            groupsPreviewText = string.Join("\n\n", groups.Select(c => $"{c.Name}:\n{string.Join("\n", c.Usernames.text)}").ToArray());
        }

        private void SaveGroupsToUdonBehaviour()
        {
            if (groupsBehaviour && groupsUdonBehaviour)
            {
                Dictionary<string, List<int>> groupData = new Dictionary<string, List<int>>();

                for (int i = 0; i < groups.Count; i++)
                {
                    foreach (string name in groups[i].Usernames?.text.Split(new char[] { '\n', '\r' }) ?? new string[0])
                    {
                        if (string.IsNullOrEmpty(name)) { continue; }

                        if (groupData.ContainsKey(name))
                        {
                            groupData[name].Add(i);
                        }
                        else
                        {
                            groupData.Add(name, new List<int>() { i });
                        }
                    }
                }

                if (isUdonSharpOne)
                {
                    Undo.RecordObject(groupsBehaviour, "Apply group data");

                    FieldInfo[] fieldInfos = typeof(Groups).GetRuntimeFields().ToArray();

                    SetVariable(fieldInfos, "groupNames", groups.Select(c => c.Name).ToArray());
                    SetVariable(fieldInfos, "groupIcons", groups.Select(c => c.Icon).ToArray());
                    SetVariable(fieldInfos, "groupArguments", groups.Select(c => c.Arguments ?? string.Empty).ToArray());
                    SetVariable(fieldInfos, "groupUsernames", groups.Select(c => c.Usernames).ToArray());
                    SetVariable(fieldInfos, "memberList", string.Format(NamelistPaddingTemplate, string.Join("\n", groupData.Keys.ToArray())));
                    SetVariable(fieldInfos, "memberGroupIndices", groupData.Select(c => c.Value.ToArray()).ToArray());

                    EditorUtility.SetDirty(groupsBehaviour);
                }
                else
                {
                    Undo.RecordObject(groupsUdonBehaviour, "Apply group data");

                    IUdonVariableTable variables = groupsUdonBehaviour.publicVariables;

                    SetProgramVariable(variables, "groupNames", groups.Select(c => c.Name).ToArray());
                    SetProgramVariable(variables, "groupIcons", groups.Select(c => c.Icon).ToArray());
                    SetProgramVariable(variables, "groupArguments", groups.Select(c => c.Arguments ?? string.Empty).ToArray());
                    SetProgramVariable(variables, "groupUsernames", groups.Select(c => c.Usernames).ToArray());
                    SetProgramVariable(variables, "memberList", string.Format(NamelistPaddingTemplate, string.Join("\n", groupData.Keys.ToArray())));
                    SetProgramVariable(variables, "memberGroupIndices", groupData.Select(c => (object)c.Value.ToArray()).ToArray());

                    UdonSharpEditorUtility.CopyUdonToProxy(groupsBehaviour);

                    EditorUtility.SetDirty(groupsUdonBehaviour);
                }
            }
        }

        private object GetVariable(FieldInfo[] fields, string variableName)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.Name.Equals(variableName))
                {
                    return field.GetValue(groupsBehaviour);
                }
            }

            return null;
        }

        private void SetVariable(FieldInfo[] fields, string variableName, object value)
        {
            foreach (FieldInfo field in fields)
            {
                if (field.Name.Equals(variableName))
                {
                    field.SetValue(groupsBehaviour, value);

                    return;
                }
            }
        }

        private void SetProgramVariable(IUdonVariableTable variableTable, string symbol, object value)
        {
            if (!variableTable.TryGetVariableType(symbol, out _) || !variableTable.TryGetVariableValue(symbol, out _))
            {
                variableTable.TryAddVariable((IUdonVariable)Activator.CreateInstance(typeof(UdonVariable<>).MakeGenericType(value.GetType()), symbol, value));
            }
            else
            {
                variableTable.TrySetVariableValue(symbol, value);
            }
        }

        private void SetVariablesDirty()
        {
            isDirty = true;
        }

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Varneon's UdonEssentials - Groups", EditorStyles.largeLabel);
            }

            if (invalidBehaviour)
            {
                EditorGUILayout.HelpBox("Use the Groups prefab provided in Assets/Varneon/Udon Prefabs/Essentials/Groups", MessageType.Error);

                return;
            }

            if (isPlayModeActive)
            {
                EditorGUILayout.HelpBox("Exit play mode to edit the groups", MessageType.Info);

                return;
            }

            if (isPrefabInspector)
            {
                EditorGUILayout.HelpBox("Place this prefab in the scene to edit the groups", MessageType.Info);

                DisplayGroupPreview();

                return;
            }

            if (waitingForPrefabUnpack)
            {
                EditorGUILayout.HelpBox("Unpack this prefab to edit the groups", MessageType.Info);

                if (GUILayout.Button("Unpack Prefab", GUILayout.Height(24)))
                {
                    PrefabUtility.UnpackPrefabInstance(groupsBehaviour.gameObject, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                }

                DisplayGroupPreview();

                return;
            }

            EditorGUILayout.HelpBox("Define names of the groups and text assets containing the usernames of the members in that group below", MessageType.Info);

            for (int i = 0; i < groups?.Count; i++)
            {
                Group group = groups[i];

                using (new GUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        using (new EditorGUI.IndentLevelScope(1))
                        {
                            group.Expanded = EditorGUILayout.Foldout(group.Expanded, group.Name, true);
                        }

                        if (GUILayout.Button("X", GUILayout.Width(25))) { if (EditorUtility.DisplayDialog("Remove Group?", $"Are you sure you want to remove the following group:\n\n{group.Name}", "Yes", "No")) { SetVariablesDirty(); groups.RemoveAt(i); break; } }
                    }

                    if (group.Expanded)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.VerticalScope())
                            {
                                using (var scope = new EditorGUI.ChangeCheckScope())
                                {
                                    group.Name = EditorGUILayout.TextField("Name:", groups[i].Name);

                                    group.Usernames = (TextAsset)EditorGUILayout.ObjectField("Username List:", group.Usernames, typeof(TextAsset), false);

                                    group.Arguments = EditorGUILayout.TextField("Arguments (WIP):", groups[i].Arguments);

                                    if (scope.changed)
                                    {
                                        SetVariablesDirty();
                                    }
                                }

                                using (new GUILayout.HorizontalScope())
                                {
                                    GUILayout.FlexibleSpace();
                                }
                            }

                            group.Icon = (Sprite)EditorGUILayout.ObjectField(group.Icon, typeof(Sprite), false, new GUILayoutOption[] { GUILayout.Width(ThreeLines), GUILayout.Height(ThreeLines) });

                            using (new GUILayout.VerticalScope())
                            {
                                using (new EditorGUI.DisabledGroupScope(i == 0))
                                {
                                    if (GUILayout.Button("▲", GUILayout.Width(25)))
                                    {
                                        SetVariablesDirty();
                                        groups.RemoveAt(i);
                                        groups.Insert(i - 1, group);
                                        break;
                                    }
                                }

                                using (var scope = new EditorGUI.ChangeCheckScope())
                                {
                                    int newIndex = EditorGUILayout.IntField(i, GUILayout.Width(25));

                                    if (scope.changed)
                                    {
                                        GUI.FocusControl(null);

                                        if (newIndex >= 0 && newIndex < groups.Count)
                                        {
                                            SetVariablesDirty();
                                            groups.RemoveAt(i);
                                            groups.Insert(newIndex, group);
                                            break;
                                        }
                                    }
                                }

                                using (new EditorGUI.DisabledGroupScope(i == groups.Count - 1))
                                {
                                    if (GUILayout.Button("▼", GUILayout.Width(25)))
                                    {
                                        SetVariablesDirty();
                                        groups.RemoveAt(i);
                                        groups.Insert(i + 1, group);
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    groups[i] = group;
                }
            }

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New Group"))
                {
                    SetVariablesDirty();
                    groups.Add(new Group() { Expanded = true, Name = $"Group #{groups.Count + 1}" });
                }

                using (new EditorGUI.DisabledGroupScope(!isDirty))
                {
                    if (GUILayout.Button("Save Groups"))
                    {
                        isDirty = false;
                        SaveGroupsToUdonBehaviour();
                    }
                }
            }

            GUILayout.Space(20);

            using (new EditorGUI.IndentLevelScope(1))
            {
                if (previewGroupsData = EditorGUILayout.Foldout(previewGroupsData, "Groups Data Preview", true))
                {
                    DisplayGroupPreview();
                }
            }
        }

        private void DisplayGroupPreview()
        {
            using (new GUILayout.VerticalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Groups:", EditorStyles.largeLabel);

                GUILayout.Label(groupsPreviewText, EditorStyles.wordWrappedLabel);
            }
        }
    }
#endif
    #endregion
}
