<div align="center">
  <img src="https://github.com/DavidFernandezChaves/RobotAtVirtualHome/blob/main/Assets/Resources/RobotAtVirtualHome/Images/RobotAtVirtualHome.gif?raw=true" alt="ViMantic" width="400" height="200"/>
</div>

# Introduction
Robot@VirtualHome consists of a set of tools that can instantiate, change the appearance of, and provide knowledge from up to 30 virtual houses. Each of these has been designed out of real houses' resources (plans, images, point clouds, etc.) hosted on <a href="www.idealista.com">Idealista</a>, a popular real estate website in Spain. Objects appearing in real houses have also been replicated using virtual objects. These are 3D models of the same type (\eg chair, table, microwave, etc.) as the objects found in the real environment and which are placed in the equivalent location to the position where the objects they recreate are. All these objects, as well as all the rooms, have been manually labeled in order to also provide ground truth information about the types of the elements they imitate, hence augmenting the geometric and appearance information with semantic knowledge.

![Head Image](./Assets/Resources/RobotAtVirtualHome/Images/ImgExample.png)
  
## How to use?
  To start using this ecosystem, you just need to have Unity installed, download the project and open the scene located in: "Assets/RobotAtVirtualHome/Scenes/RobotAtVirtualHome".
  
  
## Features
1. **Customize your environment**:
In the gameobject "map" you can find the "GeneralManager" script which will help you configure many of the appearance aspects of the environment.

>**House Selected:** You can choose which house to instantiate between 1 and 30. (0 to choose a random house).
>
>**Record Hierarchy:** Check if you want to save a log file with information about the instantiated map.
>
>**Transparent Roof:** Check if you want the roof of the house to be transparent to make it easier to see the execution. Note: this will not cause ambient light problems.
>
>**Initial State Door:** Choose the state of the doors, which modifies the navigation area of the agents. It can be set to None - will be kept as default, on - all doors open, off - all doors closed, or random. 
>
>**Initial State General Light:** Modifies the status of the ceiling lights in each room. It can be set to None - will be kept as default, on - all doors open, off - all doors closed, or random.
>
>**Initial State Lights:** Modify the state of the lights in the lamps of each room. It can be set to None - will be kept as default, on - all doors open, off - all doors closed, or random.
>
>**Wall Painting:** Modify the color of the walls. It can be set to None - will be kept as default, on - all doors open, off - all doors closed, or random. It is possible to add new colors and textures by inserting it in the folder: "Assets/Resources/RobotAtVirtualHome/Materials/Walls"
>
>**Floor Painting:** Modify the color of the floors. It can be set to None - will be kept as default, on - all doors open, off - all doors closed, or random. It is possible to add new colors and textures by inserting it in the folder: "Assets/Resources/RobotAtVirtualHome/Materials/Floors"
>
>**Force Speed:**
>
>**Speed Foced:**
>
>**Path:**
        
  you can change the default options for lights, doors, colors, etc., by modifying each House prefabs in: "Assets/Resources/RobotAtVirtualHome/Houses".
  
  2. Ambient Light  

  
  3. Robotic Behavior
  
  4. Virtual Sensors
  
  
## Use Case

## Dataset

## Reference

