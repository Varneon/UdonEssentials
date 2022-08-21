Varneon's UdonEssentials - Playerlist


This prefab is a playerlist that lists all players in the instance and shows their playmode, id, name and time when they joined the instance if the user has joined the instance after the local player has joined

The list also shows following information about the instance: players online and total playercount in the instances lifetime, master of the instance, your time in the instance and total lifetime of the instance

The list has an option for allowing the creator of the world to assign users into 2 different groups, list will show a little customizable icon next to their name if they are part of the group

The playerlist has an option for linking it to Groups - an UdonBehaviour which allows the creator to establish in-game groups which are accessible during runtime. By assigning an icon to a group in Groups prefab and linking it to the playerlist, the icons of the 2 first groups that the player is part of can be displayed on the playerlist.

Additionally the playerlist supports advanced arguments declared in Groups, (e.g. -playerlistFrameColor=#8000FF will set the color of the frame of the group member purple and -noPlayerlistIcon will disable the icon showcase of that group and attempt to show the next group instead)


Instructions for setting up the list:

1) Open PlayerlistExampleScene.scene or drop the Playerlist prefab into your scene

2) Place the root GameObject where you would like the list to be located

3) If you would like to scale the list to fit certain environment, you may select the first child GameObject in the hierarchy under the root that has the Canvas component on it and adjust the "Width" and "Height" of the RectTransform on the top of the inspector

4) (Optional) Link Groups prefab to the playerlist on the settings tab to enable group showcase

5) Everything is ready for use in VRChat!