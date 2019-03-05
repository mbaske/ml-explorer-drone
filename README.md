## Explorer Drone - [Video](https://www.youtube.com/watch?v=3-4_-FBDr8Q)

<img src="images/banner.png" align="middle" width="1920"/>

This is a basic zero gravity drone demo, made with Unity Machine Learning Agents. The idea was to train a drone which has just a single raycast per timestep for scanning its environment. Each ray intersects an octree and creates leaf nodes along its way. The number of intersections per node and the accumulated leaf node volume near the drone's position are observed for reinforcement learning. As well as the drone's velocity and a buffer containing the 10 most recent scan points. Rewards are set in proportion to the number of newly created leafs. The drone's action space consists of values for propulsion and raycast direction.

The project utilizes the now [outdated version 0.6 of ml-agents](https://github.com/Unity-Technologies/ml-agents/releases/tag/0.6.0a).
In order to run the trained models, you need to install the Unity TensorFlowSharp Plugin and add ENABLE_TENSORFLOW to Edit > Project Settings > Player > Configuration > Scripting Define Symbols in the Unity Editor.
An archived copy of the plugin is available [here](https://www.icloud.com/iclouddrive/0hz4Gx3Knz6D6iuU8fqcasIaw#TFSharpPlugin).

The project uses [Popcron's Gizmos library](https://github.com/popcron/gizmos) for data visualization.
