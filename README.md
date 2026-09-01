# The Crossroads

The Crossroads is a Unity addon which you can download to your project.

With it you can attach a script to a GameObject, which you can then run a function that takes some parameters and a list of names of functions it can output, and outputs the most logical function.

Ex.\
**Input:**
  - **Parameters:** 
    - **Object:** lamp
    - **Description:** a bedside lamp
    - **Action:** take out the filament
  - **Possible functions(format: name - description):** 
    - **LampBroken()** - the lamp is now broken
    - **LampDisappear()** - the lamp disappeared
    - **LampOn()** - the lamp is now on
    - **LampOff()** - the lamp is now off
    - **LampOnFire()** - the lamp is now on fire
    - **Nothing()** - nothing happens

**Output:** LampBroken()

## How to use

1. Download
    - TheCrossroads.cs
    - tokenizer.json(inside the StreamingAssets folder)
    - modelreal.sentis(inside the StreamingAssets folder - this will take a while)

2. Move TheCrossroads.cs to the Assets folder in your Unity project.\
          If you don’t have a StreamingAssets folder then create one, and then move tokenizer.json and modelreal.sentis inside.

3. To call the most logical function just do

        string function = crossroads.BestOption(optionnames, optiondescriptions, name, description, action);
        MethodInfo method = functions.GetType().GetMethod(function);

Where
  - optionnames is the array of your options’ names
  - optiondescriptions is the array of your options’ descriptions(what they will do)
  - name is the name of your object
  - description is the description of your object
  - action is the action that is being done to the object
  - functions is your script containing the possible functions


Thanks for reading!






                        ------____       
                       |          ***---    This is Murphy. Say hi to Murphy.
                       ͏͏▏               |   
                      |    O           ▏    
                 ‾‾¯¯¯▏      ,   O    |     
                     |                ▏‾‾¯¯⁻⁻
                     ***---___       | 
                        /     ***----      
                      _/        |        
                               _|          
