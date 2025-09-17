# Experiment Visualisation Trackingdata

## Overview 

###  Description
This Streamlit Web Application uses Data stored in a MongoDB from Experiments and displays multiple plots visualising the behavior of the test subjects.

## Setup
Python version 3.12.0  
login.json in directory ../db/login/login.json (Login data for MongoDB)

### Requirements
The Requirements.txt is in the Project

- matplotlib==3.9.0
- numpy==1.26.4
- numpy_quaternion==2023.0.4
- pandas==2.2.2
- Pillow==10.3.0
- plotly==5.22.0
- pydub==0.25.1
- pymongo==4.7.2
- Requests==2.31.0
- scipy==1.13.1
- seaborn==0.13.2
- streamlit==1.34.0
- textblob==0.18.0.post0
- whisper==1.1.10
  

## How to use

### starting the Application
In your Terminal type streamlit run streamlit run Live_Experiment_Analysis.py  
In the Sidebar navigate to Trackingdata.

### Usage

In the Sidebar first select the experiment and the date. This will display all playerIds that where logged in that day.  

![image](https://github.com/user-attachments/assets/c6c39c4e-d952-4686-95cd-74217f3d7785)

**Optional** You can now filter the players to only show one role  
*Note** some experiments have no role or role is not stored correctly   

![image](https://github.com/user-attachments/assets/8bd4ec7c-7e9c-4433-a0bb-14e25197131e)  

Now select the playerIds. This will first load the level selector. After Loading finished you can click the "Load Plots" button.  
When a player is first loaded data from the mongodb has to be fetched, which will take some time. After the first time no data has to be fetched again (only session wise).   

At the top you can switch between the Plots and Multimodal chat Tabs  

### Options
In the options Menu you have the ability to select different settings. 
**You have to click submit to save the changes** 
Starting from left to right.  
- Select filtered Objects which will not be displayed in the Interaction Heatmap
- Select which Eye should be used to calclualte the Eygazes Plot. Default is 0 = left Eye
- Select smoothing factor for the Linegraphs. default is 250 lower number means less smoothing
- Select the Radius for the Head and Body Sphere (Hitbox) (When calculating if a person looked at another person)
- Checkbox display contious plots. Displays a continous motion of the facial expression polygons on the avatarface (Default is False because it does take a lot of time especially for the longer experiments to go through all players)
- checkbox show teleportations. Shows in the linegraphs when a player is teleportated (skews the graph)
- checkbox less expensive plots. Doesn't show facial expressions and the heatmap. The heatmap is what really takes a long time to calculate   

![image](https://github.com/user-attachments/assets/30ad7c08-8d08-403f-8d2e-da754c56fa46)

### Levelselector 
**You have to click submit to save the changes**  
**Note you also have to tick the checkbox to filter out the time**
In the selectboxes you can see each level when it started for each player.  
Then in the slider choose the time you want to see  
The Interactionheatmap, Facial expressions and the multimodal chat is nopt influenced by the levelfilter

![image](https://github.com/user-attachments/assets/49099bca-31ca-47f9-9026-99b343dd00f5)

## Functions

### What are the plots representing
- Eyegaze: The orange dot is the position of the Eye and the orange line is the straight forward looking direction.
The blue points represent where the person looked originating from [0, 0, 0] (orange dot)

![image](https://github.com/user-attachments/assets/29fc171d-a5a4-45b2-b2ec-aa6c69230a33)  

- Handheatmap: The orange dot is the Position of the Head. The "Volumeheatmap" shows where relatively to the head the hand is.
  **doesn't work as intended** The handposition is rotated to the direction which the **Body** faces, which causes the heatmap to be displayed "around" the player.
  extra eklärung
  
- Facial Expressions: Displays the intensity of movement for 63 key points on the face, indicating how much each part has moved. see project files for Legend
  
   Using this scale purple being the lowest and yellow the highest
  
![image](https://github.com/user-attachments/assets/db7c8a18-82a9-43b5-b711-e1eef246c1e9)  

- Interaction heatmap: Shows where and how the player has interacted with an object on the map. (for some experiments are the sizes of the icons skewed because some experiments have different dimensions)

- Heatmap: Shows a heatmap for positional points
  
![image](https://github.com/user-attachments/assets/df35e730-e159-45f5-ada7-a60ac662e35d)

If there is no no backroundmap the heatmap will be displayed on a white backround
![image](https://github.com/user-attachments/assets/3d9710be-74ea-44c5-9a86-5629a6639c26)


- Multimodal chat: Displays a timeline from top (start) to bottom (end), whether a player was looking at another player's head or body and when a player interacted with an object.

### structure of data

For all plots (except facial expressions) was first the data fetched and calculated upon and then with this data where the plots made. Some data is used multiple times for different plots.  

When the data is fetched every player is given a list where his data is stored [ [], [], [] ]  

See input output for how _fetch_data works  

Please see in project file /datastructure/body for List_of_every_player_body explanation diagramm  

list_of_every_player_head and list_of_every_player_hand follows a similiar formula



## input Ouput

Not all function are here, because some are very specific.  

### linegraph.py

#### _fetch_data: 
takes in( a list of playerids, the collection to search in, what to search for ) # playerId is always returned 

 ["324Fg", "jshfu"],    main.collection_body,      {serverTime:1, position:1} 
 
 Output: returns a list of lists [ [], [] ] where each list is for one player, in which the data is stored

### Eye_tracking.py   
#### create_quaternion(orientation): 

takes in a "quaternion" in a "list format" [0, 0, 0, 1] with w being the last element. 

output: a quaternion in matrix format

### facial_expression.py
#### avatarId_finder(playerId): 

takes in **one** playerid as a string 

outputs: an int 0 to 31 representing the avatarId of the playerId 


#### get_intensitity_for_expression(percent):  
Input: Float number 0 to 1  (a percent)  
Output: 4 tuple RGBA in normalized format (r,g,b,opacity)  

### interactions.py

#### interactionShapeNetObjectAdder(interactionsObjects):    
**Only for Shapenet objects if no shapenet object return the same dict**  

Input: a dictionary of interactions done {'objectName': '343434335353_center', 'hand': 'Right', 'interaction': 'ungrasped', 'playerId': '520708ed-59e733b7', 'server-timestamp': datetime.datetime(2024, 5, 29, 9, 34, 30), 'position': {'x': -821.1394, 'y': 51.47187, 'z': -770.7883}}  
Output: return the dict with the objectName changed to the real Objectname + adds two new key values pairs. the old Id name and categories     

### levelSelecter.py  

####  getMinMaxTime(playerIds):  
Input : list of playerIds   
Output: **Across all playerIds** that were given outputs: a list of  a dictionary with the min and max of the server time [ { maxServerTime: } ]  
minServerTime first Servertime and "maxServerTime" last ServerTime (Login, Logoff)   

### multimodalChat.py  

#### collisionChecker(personPosition, personViewPosition, secondPersonSphere, radius=0.15):  
Input: Position first point [ 0, 4, 3 ] , position second Point, PositionSphere, **optional radius=float**  
Output: weather or not the line beetween the first two points, intersects the third position (Sphere) with the radius= 


