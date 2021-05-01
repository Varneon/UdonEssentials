Varneon's UdonEssentials - Playerlist


This prefab is a playerlist that lists all players in the instance and shows their playmode, id, name and time when they joined the instance if the user has joined the instance after the local player has joined

The list also shows following information about the instance: players online and total playercount in the instances lifetime, master of the instance, your time in the instance and total lifetime of the instance

The list has an option for allowing the creator of the world to assign users into 2 different groups, list will show a little customizable icon next to their name if they are part of the group

The creator also has an option to highlight their panel for everyone when they are in the world


Instructions for setting up the list:

1) Open PlayerlistExampleScene.scene or drop the Playerlist prefab into your scene

2) Place the root GameObject where you would like the list to be located

3) If you would like to scale the list to fit certain environment, you may select the first child GameObject in the hierarchy under the root that has the Canvas component on it and adjust the "Width" and "Height" of the RectTransform on the top of the inspector

4) Modify any of the parameters in the Playerlist root UdonBehaviour under "Settings". Please do not modify the References unless you know what you are doing

5) Everything is ready for use in VRChat!