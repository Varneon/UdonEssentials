Varneon's UdonEssentials - Music Player

This prefab is a music player that allows you to listen to playlists that have been added to the player before uploading the world.

For now the player only works for the local player and it is still missing few quality of life features like single song repeat, favourites, etc...


Instructions for setting up the player:

1) Open MusicPlayerExampleScene.scene or drop the Tunify prefab into your scene

2) Place the root GameObject where you would like the player to be located

3) If you would like to scale the player to fit certain environment, you may select the first child GameObject in the hierarchy under the root that has the Canvas component on it and adjust the "Width" and "Height" of the RectTransform on the top of the inspector

4) Open the Music Player Manager by opening "Varneon" > "Udon Prefab Editors" > "Music Player Manager" on your Unity Editor toolbar

5) Make sure that "Active Music Player" field is populated by the player in your scene, this will be indicated by the green background on the object field

6) Make sure that the active music library file can be found on the next object field

7) Create any kind of playlists and songs you would like to have on the music player and click "Save Library" on the bottom of the window to save the music library file for later use.

8) When you are ready to export the music library to the player, click "Apply Library To Player" and make sure that the song statistics under the Active Music Player represent your intended changes

9) Close the Music Player Manager

10) Everything is ready for use in VRChat!


Optional advanced steps:

1) You may change any of the parameters on the Tunify UdonBehaviour under the "Settings" tab. Please do not modify the References unless you know what you are doing

2) If you would like to debug the player in game, the Tunify UdonBehaviour has a field for my UdonConsole which is demonstrated in the example scene