
#pragma warning disable IDE0044 // Making serialized fields readonly hides them from the inspector
#pragma warning disable IDE1006 // VRChat public method network execution prevention using underscore
#pragma warning disable 649

using UdonSharp;
using UnityEngine;
using VRC.Udon;
using System.Linq;
using VRC.Udon.Common.Interfaces;
using System;
using VRC.Udon.Common;

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
        [SerializeField, HideInInspector]
        private string[] groupNames = new string[0];

        [SerializeField, HideInInspector]
        private Sprite[] groupIcons = new Sprite[0];

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

        private readonly char[] NewlineChars = new char[] { '\n', '\r' };

        private const string NamePaddingTemplate = "\n{0}\n";

#pragma warning disable IDE0052 // Variable is only used for the custom inspector

        [SerializeField, HideInInspector]
        private TextAsset[] groupUsernames = new TextAsset[0];

#pragma warning restore IDE0052

        private void Start()
        {
            name = "Varneon.UdonPrefabs.Essentials.Groups";
        }

        /// <summary>
        /// Gets indices of the groups that the player with the provided name is part of
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public int[] _GetGroupIndicesOfPlayer(string displayName)
        {
            int lookupPos = GetPlayerLookupIndex(displayName);

            if(lookupPos < 0) { return new int[0]; }

            return memberGroupIndices[memberList.Substring(0, lookupPos).Split(NewlineChars).Length - 1];
        }

        /// <summary>
        /// Gets the icon sprite of the group with the index of groupIndex
        /// </summary>
        /// <param name="groupIndex"></param>
        /// <returns></returns>
        public Sprite _GetGroupIcon(int groupIndex)
        {
            return groupIcons[groupIndex];
        }

        /// <summary>
        /// Gets the name of the group with the index of groupIndex
        /// </summary>
        /// <param name="groupIndex"></param>
        /// <returns></returns>
        public string _GetGroupName(int groupIndex)
        {
            return groupNames[groupIndex];
        }

        /// <summary>
        /// Gets the tags of the group with the index of groupIndex
        /// </summary>
        /// <param name="groupIndex"></param>
        /// <returns></returns>
        public string _GetGroupArguments(int groupIndex)
        {
            return groupArguments[groupIndex];
        }

        /// <summary>
        /// Runtime method for adding players into groups
        /// </summary>
        /// <param name="groupName"></param>
        /// <param name="displayNames"></param>
        public void _AddPlayersToGroup(string groupName, string[] displayNames)
        {
            int groupIndex = 0;

            for(int i = 0; i < groupNames.Length; i++)
            {
                if (groupNames[i].Equals(groupName))
                {
                    groupIndex = i;

                    break;
                }
            }

            foreach(string displayName in displayNames)
            {
                AddPlayerToGroup(groupIndex, displayName);
            }
        }

        private void AddPlayerToGroup(int groupIndex, string displayName)
        {
            int playerLookupIndex = GetPlayerLookupIndex(displayName);

            if(playerLookupIndex < 0)
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
                foreach(int index in indices)
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

        private int GetPlayerLookupIndex(string displayName)
        {
            return memberList.IndexOf(string.Format(NamePaddingTemplate, displayName));
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
            groupsBehaviour = (Groups)target;

            groupsUdonBehaviour = UdonSharpEditorUtility.GetBackingUdonBehaviour(groupsBehaviour);

            groups = new List<Group>();

            IUdonVariableTable variables = groupsUdonBehaviour.publicVariables;

            variables.TryGetVariableValue("groupNames", out string[] groupNames);
            variables.TryGetVariableValue("groupIcons", out Sprite[] groupIcons);
            variables.TryGetVariableValue("groupArguments", out string[] groupArguments);
            variables.TryGetVariableValue("groupUsernames", out TextAsset[] groupUsernames);

            for(int i = 0; i < groupNames?.Length; i++)
            {
                groups.Add(new Group() { Name = groupNames[i], Icon = groupIcons[i], Usernames = groupUsernames[i], Arguments = groupArguments[i] });
            }
        }

        private void OnDestroy()
        {
            SaveGroupsToUdonBehaviour();
        }

        private void SaveGroupsToUdonBehaviour()
        {
            if (groupsUdonBehaviour)
            {
                Undo.RecordObject(groupsUdonBehaviour, "Apply group data");

                IUdonVariableTable variables = groupsUdonBehaviour.publicVariables;

                SetProgramVariable(variables, "groupNames", groups.Select(c => c.Name).ToArray());
                SetProgramVariable(variables, "groupIcons", groups.Select(c => c.Icon).ToArray());
                SetProgramVariable(variables, "groupArguments", groups.Select(c => c.Arguments ?? string.Empty).ToArray());
                SetProgramVariable(variables, "groupUsernames", groups.Select(c => c.Usernames).ToArray());

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

                SetProgramVariable(variables, "memberList", string.Format(NamelistPaddingTemplate, string.Join("\n", groupData.Keys.ToArray())));
                SetProgramVariable(variables, "memberGroupIndices", groupData.Select(c => (object)c.Value.ToArray()).ToArray());

                groupsBehaviour.UpdateProxy();
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

        public override void OnInspectorGUI()
        {
            using (new GUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                GUILayout.Label("Varneon's UdonEssentials - Groups", EditorStyles.largeLabel);
            }

            EditorGUILayout.HelpBox("Define names of the groups and text assets containing the usernames of the members in that group below", MessageType.Info);

            for (int i = 0; i < groups.Count; i++)
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

                        if (GUILayout.Button("X", GUILayout.Width(25))) { if (EditorUtility.DisplayDialog("Remove Group?", $"Are you sure you want to remove the following group:\n\n{group.Name}", "Yes", "No")) { groups.RemoveAt(i); break; } }
                    }

                    if (group.Expanded)
                    {
                        using (new GUILayout.HorizontalScope())
                        {
                            using (new GUILayout.VerticalScope())
                            {
                                group.Name = EditorGUILayout.TextField("Name:", groups[i].Name);

                                group.Usernames = (TextAsset)EditorGUILayout.ObjectField("Username List:", group.Usernames, typeof(TextAsset), false);

                                group.Arguments = EditorGUILayout.TextField("Arguments (WIP):", groups[i].Arguments);

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
                    groups.Add(new Group() { Expanded = true, Name = $"Group #{groups.Count + 1}" });
                }
                else if (GUILayout.Button("Save Groups"))
                {
                    SaveGroupsToUdonBehaviour();
                }
            }
        }
    }
#endif
    #endregion
}
