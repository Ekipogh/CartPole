# CartPole with Unity NEAT

This project is a Unity implementation of the CartPole problem using NEAT (NeuroEvolution of Augmenting Topologies). The goal is to train a neural network to balance a pole on a moving cart through evolutionary algorithms.

## Features

- **Unity Integration**: Built entirely in Unity for easy visualization and simulation.
- **NEAT Algorithm**: Implements NEAT for evolving neural networks.
- **Customizable Parameters**: Adjust simulation and NEAT settings to experiment with different configurations.
- **Real-Time Visualization**: Watch the training process and see how the AI improves over generations.

## Requirements

- Unity 2021.3 or later
- .NET Framework 4.7.1 or later

## Getting Started

1. Clone the repository:
    ```bash
    git clone https://github.com/Ekipogh/CartPole.git
    ```
2. Open the project in Unity.
3. Configure NEAT parameters in the `NEATConfig` script.
4. Press Play in the Unity Editor to start the simulation.

## Project Structure

- **Assets/Scripts**: Contains all the scripts for NEAT and simulation logic.
- **Assets/Prefabs**: Prefabs for the cart and pole.
- **Assets/Scenes**: Main scene for running the simulation.

## How It Works

1. The cart and pole system is simulated in Unity.
2. A population of neural networks is evolved using NEAT.
3. Each neural network controls the cart and is evaluated based on how long it can balance the pole.
4. The best-performing networks are selected and mutated to create the next generation.

## Customization

- Customize fields in NeatController.cs Unity monoscript to modify population size, generations number, number of champions and anti-champions

## References

- [NEAT Paper by Kenneth O. Stanley](http://nn.cs.utexas.edu/downloads/papers/stanley.ec02.pdf)
- [Unity Documentation](https://docs.unity3d.com/)

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Inspired by the classic CartPole problem in reinforcement learning.
- Thanks to the NEAT algorithm community for their contributions.
