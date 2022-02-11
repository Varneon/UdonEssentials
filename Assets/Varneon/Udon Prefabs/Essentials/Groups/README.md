Varneon's UdonEssentials - Groups

Groups is an UdonBehaviour that allows creators to set up their own groups for the world based on lists of names and add information to the groups like icon and arguments


Instructions for setting up groups:

1) Add Groups UdonBehaviour into your scene by dragging "Groups" prefab from "Assets/Varneon/Udon Prefabs/Essentials/Groups" into the scene

2) Proceed to configure the groups as you wish:
    Name: Name of the group
    Username List: TextAsset (.txt file) containing the list of display names for the group, separated by lines
    Icon: On the right side of the panel you can specify icon Sprite for the group
    Arguments: WIP feature allowing specific parameters to be fed to other UdonBehaviours (e.g. -playerlistFrameColor=#8000FF will set the color of the UdonEssentials playerlist frame of the group member purple)

3) Press "Save Groups" or select another object to save the group information to the UdonBehaviour

4) Done! All of the information can now be read from other UdonBehaviours! Check out Examples folder to find out how to access the groups data from UdonSharpBehaviours
