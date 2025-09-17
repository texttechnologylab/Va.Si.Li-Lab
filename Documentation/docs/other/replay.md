# Simulations & Experiments Playback

Simulations and Experiments performed using this project can be replayed using the following steps:


## Add Scenes to Build Setting

Scenes to be replayed and the Simulaton loader scene should be added to build setting.

1. Open `SmulatonLoader` scene located in `Assets/Simulation_dev/Scenes`.  
2. From `File` tab choose `Build Settings...`.  
3. Add `SimulatoinLoader` scene to build by clickng `Add Open Scenes`.  
4. Make sure `SimulatioinLoader` scene is enabled using the toggle to its left side.  
5. Add and enable all scenes of the Simulations/Experiments to be replayed.  


## Start Playback

To start playback open and play `SimulatonLoader` scene.  

### Local Playback

Scene will search for previous Playbacks, if any has been saved.  

Choose the scene, date and players to be replayed then click on `Next`.

### Online Playback

If no local data found, or if online data is preferred, using the `Load Online Data` toggle **(Disabled by default)** will connect to the server and retreive information about Simulations and its data.

By online Playback the followng options are available:

- Keeping the chosen playbacks data if local instances of it are found, or Saving the chosen playbacks if no local data of it found using the `Keep Json Copy` toggle. Loacl data will be yellow colored by the list of players to choose from. **(Enabled by default)**
- Use online data instead of local data of the chosen playbacks, if any local data found, using the `Updated Info` toggle. This will Replace local data if `Keep Json Copy` toggle is enabled. **(Disabled by default)**
- **Note**: 
    - Disabling `Keep Json Copy` will remove any local data, **of the chosen playbacks only**, saved previously.
    - Having `Updated Info` toggle enabled or disabled when no local data is found will have no different action and will load online data anyway.
    - If any local playbacks are found, it will be colored yellow and will say `Json Available` by the list of players to choose from.

After choosing players click on `Next`.

### Choosing Avatars

After choosng players and clicking `Next`, a new page is loaded with the name of chosen Scene, *[Instructions](#keyboard)* on how to control playback with keyboard and the ability to choose different avatars for each player.  

Default avatars will be set according to the info retreived online or from local data. If none found, Avatar Nr. 0 will be set as default.

Click on `Load Scene` to Start playback after all required data have been loaded.


## Playback Control

There are several ways to control the playback:

### Keyboard

#### 3. Person Camera

- `Arrow Keys`: Horizontal & Vertical Rotation.  
- `T,G`: Zoom in/Out.  
- `F,H`: Next/Previous Player.  
- `P`: Pause Playbacks (Audio & Movement).  
- `O`: Change Audio Mode.  
    - `Global`: Audio from all players will be heard. **(Default)**
    - `Single`: Audio only from the current chosen player.
- `Space`: Take a Screenshot. Will be saved in `screenshots` folder.

#### 1. Person Camera

- `C`: Activate/Disable 1. Person Camera.  

**Note:** While in 1. Person Camera, no camera movements controls are available. This will imitate being in the prespective of the current player.

#### Free Camera

- `Q`: Activate/Disable Free Camera.
- `Arrow Keys`: Movement.
- `T,G`: Vertical Rotation.
- `F,H`: Horizontal Rotation.
- `Z,X`: Height Control.
- `V,B`: Jump to nexy player's position.  

**Note:** Once activated, Camera will be positioned at a default location. Depending on the scene, this location might be far from players.
using `V,B` will move Camera to the position of the next/previous player.

### Custom Inspector

Another Option to Control Playback is through a custom inspector. When Playback starts, inside `Hierarchy` tab,
click on `Simulation(Clone)` object under `DontDestroyOnLoad`. Custom inspector will be shown inside `Inspector` tab.

#### Player Info

- Choosing players.
- Loop Variable slider.
    - This will show in which frame, in regards to the current player's playback frame count, the current player is.
    - Changing the value by sliding or giving a valid number in the input field will pause the playback. Click `Play` or `Apply`
    to continue.
- Audio Mode Switcher.
- `Back` button to change chosen playbacks without the need to stop and replay the scene.
- `Play/Pause` Playback.
- `Screenshot`|`.` Take a screenshot | Open the folder, where all screenshots are saved.
- `Show/Hide Instructions` Show *[Instructions](#keyboard)* of the keyboard controls.

#### Weights Multiplier

- `Face/Eye Weights`: With a value between 0 and 2 to control the intensity of the Face/Eye pose for **all** playbacks.
- `Hand Pos. Senset.`: With a value between 0.4 and 1.5. When the difference in vertical hand position between the current frame and the previous one is bigger than this value, 
the old value from the previos frame will be taken instead and ignore the current one, to avoid having hands in extrem positions. This can happen mostly when switching
between Finger tracking and Controller tracking.
- `Voice Ind. Senset.`: With a value between 0.0001 and 1. Controls the sensetivity of the voice indicator above each player's avatar.
Higher values will requier higher sounds to activate the indicator. By value 1 the voice indicator will mostly be disabled.

#### Camera Control

- `1. Person view` toggle to activate 1. Person Camera.
- `Free Camera` toggle to activate Free Camera. This will show additional buttons underneath for movement control.
- `Rotation & Zoom` Controls when Free Camera is disabled / `Movament, Rotation & Height` Controls When Free Camera
is activated.

#### Debug Mode

- Log chosen data to console.

### Time & Speed Slider

- `Speed Slider`: With a value between 0.1 and 2, will control the speed of all playbacks' movements and sounds.
- `Time Slider`: Once selected or changed value, playbacks' movements and sounds will be paused. Unpause through inspector or keypoard `P` to continue. 


## Record Playback

All playbacks can be recorded and exported as a video:

- Add `Recorder` Window by navigating to `Window` tab then clicking `General>Recorder>Recorder Window`.
- In `Recorder` Window choose desired settings then click `START RECORDING` and when finished `STOP RECORDING`.

**Note:** While Recording, Sounds from playbacks may not be heard through Unity, but it's all being recorded in the video.