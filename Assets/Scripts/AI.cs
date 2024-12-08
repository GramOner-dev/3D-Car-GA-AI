using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AI : MonoBehaviour
{
    public int[] layerSizes = new int[] { 6, 15, 10, 4 };

    public int currentIteration = 0;
    public float maxTimePerEpisode = 50f;
    private float timeSpentInEpisode;

    public float weightAdjustmentMultiplier = 0.05f;
    public int numberOfAgents = 20;
    public int numberOfAgentsToKeepPerGeneration = 5;
    private Network[] agents;
    public GameObject carPrefab;
    public BoxCollider[] checkPoints;

    private GameObject[] cars;
    private WallManager[] wallManagers;
    private CarController[] carControllers;
    private CheckPointManager[] checkpointManagers;
    public int incrementBetweenTrainingUpdates = 5;
    private int currentFrame;

    public int[] actionIndexes;

    public float normalizedCarSpeed;
    public float normalizedDistanceToNextCheckpoint;
    public float normalizedDistanceToWallForwards;
    public float normalizedDistanceToWallRight;
    public float normalizedDistanceToWallLeft;
    public float normalizedRotationRelativeToCheckpoint;
    public float score;


    void Start()
    {
        InitNetworks();
        InitCars();
        actionIndexes = new int[numberOfAgents];
    }
    private void Update() {
        timeSpentInEpisode += Time.deltaTime;
    }

    void FixedUpdate()
    {
        normalizedCarSpeed = agents[0].getState().NormalizedCarSpeed;
        normalizedDistanceToNextCheckpoint = agents[0].getState().NormalizedDistanceToNextCheckpoint;
        normalizedDistanceToWallForwards = agents[0].getState().NormalizedDistanceToWallForwards;
        normalizedDistanceToWallRight = agents[0].getState().NormalizedDistanceToWallRight;
        normalizedDistanceToWallLeft = agents[0].getState().NormalizedDistanceToWallLeft;
        normalizedRotationRelativeToCheckpoint = agents[0].getState().NormalizedRotationRelativeToCheckpoint;
        score = agents[0].getScore(checkpointManagers[0].getTotalCheckpoints());

        
        if (shouldUpdateOnThisFrame())
        {
            SetEnvironmentsValues();
            FindActionsForAllAgents();
            if (didEpisodeEnd())
            {
                NewEpisode();
            }
        }
    }

    private bool shouldUpdateOnThisFrame()
    {
        currentFrame++;
        if (currentFrame > incrementBetweenTrainingUpdates) currentFrame = 0;
        return currentFrame == incrementBetweenTrainingUpdates;
    }

    public void InitCars()
    {
        cars = new GameObject[numberOfAgents];
        wallManagers = new WallManager[numberOfAgents];
        carControllers = new CarController[numberOfAgents];
        checkpointManagers = new CheckPointManager[numberOfAgents];
        for(int i = 0; i < numberOfAgents; i++)
        {
            cars[i] = Instantiate(carPrefab);
            cars[i].GetComponent<CheckPointManager>().setCheckPoints(checkPoints);
            wallManagers[i] = cars[i].GetComponent<WallManager>();
            carControllers[i] = cars[i].GetComponent<CarController>();
            checkpointManagers[i] = cars[i].GetComponent<CheckPointManager>();

        }
    }

    public void InitNetworks() {
        agents = new Network[numberOfAgents];
        for (int i = 0; i < agents.Length; i++)
        {
            agents[i] = new Network(layerSizes);
        }
    }

    public void NewEpisode(){
        ModifyAllAgents();
        for(int i = 0; i < numberOfAgents; i++)
        {
            Destroy(cars[i]);
        }
        InitCars();
    }

    public void FindActionsForAllAgents()
    {
        for (int i = 0; i < numberOfAgents; i++)
        {
            carControllers[i].resetActionSpace();

            if (!wallManagers[i].WasWallHit())
            {
                actionIndexes[i] = agents[i].GetChosenActionIndex();
                carControllers[i].setActionSpace(actionIndexes[i]);
            }
            
        }
    }

    public void SetEnvironmentsValues()
    {
        for(int i = 0; i < numberOfAgents; i++)
        {
            agents[i].setValues(
                carControllers[i].getMaxCarSpeed(),
                carControllers[i].getCurrentCarSpeed(),
                checkpointManagers[i].getDistanceBetweenCheckPoints(),
                checkpointManagers[i].getCarsDistanceToNextCheckPoint(),
                checkpointManagers[i].getRotationRelativeToNextCheckpoint(),
                wallManagers[i].getCarViewDistanceToWall(),
                wallManagers[i].getDistanceToLeftWall(),
                wallManagers[i].getDistanceToRightWall(),
                wallManagers[i].getDistanceToForwardWall());
        }
    }
    public void ModifyAllAgents()
    {
        MultiplyAgents(getBestAgentIndexes());
        DoGeneticModifications();
    }

    public void DoGeneticModifications()
    { 
        for(int i = numberOfAgentsToKeepPerGeneration; i < agents.Length; i++)
        {
            agents[i].RandomlyAdjustAllWeightsAndBiases(weightAdjustmentMultiplier);
        }

    }

    public void MultiplyAgents(int[] bestAgentIndexes)
    {
        Network[] bestAgents = new Network[bestAgentIndexes.Length];
        for (int i = 0; i < bestAgents.Length; i++)
        {
            bestAgents[i] = agents[bestAgentIndexes[i]];
        }
        for (int i = 0; i < agents.Length; i++)
        {
            agents[i] = bestAgents[i % 5];
        }
    }

    public int[] getBestAgentIndexes()
    {
        int[] bestAgentIndexes = new int[numberOfAgentsToKeepPerGeneration];
        float[] agentScores = new float[numberOfAgents];
        for (int i = 0; i < agents.Length; i++)
        {
            agentScores[i] = agents[i].getScore(checkpointManagers[i].getTotalCheckpoints());
        }
        float[] sortedAgentScores = Sort.MergeSort(agentScores, 0, agentScores.Length - 1);
        float[] bestScores = new float[numberOfAgentsToKeepPerGeneration];
        for (int i = 0; i < numberOfAgentsToKeepPerGeneration; i++)
        {
            bestScores[i] = sortedAgentScores[sortedAgentScores.Length - 1 - i];
        }

        for (int i = 0; i < bestAgentIndexes.Length; i++)
        {
            for (int j = 0; j < agentScores.Length; j++)
            {
                if (bestScores[i] == agentScores[j])
                {
                    bestAgentIndexes[i] = j;
                    break;
                }
            }
        }
        return bestAgentIndexes;
    }


    private bool didEpisodeEnd()
    {
        if(timeSpentInEpisode > maxTimePerEpisode){
            timeSpentInEpisode = 0;
            return true;
        }
        int numberOfCarsThatHitWall = 0;
        foreach (WallManager wallManager in wallManagers)
        {
            if (wallManager.WasWallHit()) numberOfCarsThatHitWall++;
        }
        return numberOfCarsThatHitWall == numberOfAgents;
    }

}

public class Network
{
    public float maxCarSpeed;
    public float carSpeed;
    public float distanceFromLastCheckPointToNext;
    public float distanceToNextCheckpoint;
    public float rotationRelativeToNextCheckpoint;
    public float carViewDistanceToWall;
    public float distanceToWallForwards;
    public float distanceToWallRight;
    public float distanceToWallLeft;
    public int currentCheckpoint;

    private State currentState;


    public Layer[] layers;
    public Network(int[] layerSizes)
    {
        int numberOfLayers = layerSizes.Length - 1;
        layers = new Layer[numberOfLayers];
        for (int i = 0; i < numberOfLayers; i++)
        {
            int numberOfNeuronsForLayer = layerSizes[i + 1];
            int numberOfInputsForLayer = layerSizes[i];
            layers[i] = new Layer(numberOfNeuronsForLayer, numberOfInputsForLayer);
        }
    }

    public float[] GetQValues(float[] inputs)
    {
        float[] currentOutput = inputs;
        for (int i = 0; i < layers.Length; i++)
        {
            bool isOutputLayer = i == layers.Length - 1;
            currentOutput = layers[i].GetOutputs(currentOutput, isOutputLayer);
        }
        return currentOutput;
    }

    public int FindBestActionIndex(float[] QValues)
    {
        int currentBestActionIndex = 0;
        for (int i = 0; i < QValues.Length; i++)
        {
            if (QValues[i] > QValues[currentBestActionIndex]) currentBestActionIndex = i;
        }
        return currentBestActionIndex;
    }
    public void RandomlyAdjustAllWeightsAndBiases(float weightAdjustmentMultiplier)
    {
        foreach(Layer layer in layers)
        {
            layer.RandomlyAdjustNeuronWeightsAndBiases(weightAdjustmentMultiplier);
        }
    }
    public float getScore(int currentCheckpoint)
    {
        float score = currentCheckpoint;
        score += (1 - currentState.NormalizedDistanceToNextCheckpoint);
        return score;
    }

    public int GetChosenActionIndex()
    {
        setCurrentState();
        float[] inputs = currentState.createInputVector();
        float[] QValues = GetQValues(inputs);
        return FindBestActionIndex(QValues);
    }

    public void setValues(float maxCarSpeed, float carSpeed, float distanceFromLastCheckPointToNext, float distanceToNextCheckpoint, float rotationRelativeToNextCheckpoint, float carViewDistanceToWall, float distanceToLeftWall, float distanceToRightWall, float distanceToForwardWall)
    {
        this.maxCarSpeed = maxCarSpeed;
        this.carSpeed = carSpeed;
        this.distanceFromLastCheckPointToNext = distanceFromLastCheckPointToNext;
        this.distanceToNextCheckpoint = distanceToNextCheckpoint;
        this.rotationRelativeToNextCheckpoint = rotationRelativeToNextCheckpoint;
        this.carViewDistanceToWall = carViewDistanceToWall;
        this.distanceToWallLeft = distanceToLeftWall;
        this.distanceToWallRight = distanceToRightWall;
        this.distanceToWallForwards = distanceToForwardWall;
    }

    private void setCurrentState()
    {
        currentState = new State(carSpeed, maxCarSpeed, distanceFromLastCheckPointToNext, distanceToNextCheckpoint, rotationRelativeToNextCheckpoint, carViewDistanceToWall, distanceToWallForwards, distanceToWallRight, distanceToWallLeft);
    }
    public State getState() => currentState;
}

public class Layer
{
    private Neuron[] neurons;

    public Layer(int numOfNeurons, int numberOfInputs)
    {
        neurons = new Neuron[numOfNeurons];
        for (int i = 0; i < numOfNeurons; i++)
        {
            neurons[i] = new Neuron(numberOfInputs);
        }
    }

    public float[] GetOutputs(float[] inputs, bool isOutputLayer)
    {
        float[] outputs = new float[neurons.Length];
        for (int i = 0; i < outputs.Length; i++)
        {
            outputs[i] = neurons[i].ComputeOutput(inputs, isOutputLayer);
        }
        return outputs;
    }

    public void RandomlyAdjustNeuronWeightsAndBiases(float weightAdjustmentMultiplier)
    {
        foreach(Neuron neuron in neurons)
        {
            neuron.RandomlyAdjustWeightsAndBias(weightAdjustmentMultiplier);
        }
    }
}

public class Neuron
{

    private float[] weights;
    private float bias;
    public Neuron(int numberOfInputs)
    {
        weights = WeightInitializer.GetRandomWeights(numberOfInputs);
        bias = RandomFloatInRange(0f, 1f);
    }
    public float RandomFloatInRange(float min, float max)
    {
        System.Random rand = new System.Random();
        return (float)(min + rand.NextDouble() * (max - min));
    }

    public float ComputeOutput(float[] inputs, bool isOutputLayer)
    {
        float sum = 0;
        for (int i = 0; i < inputs.Length; i++)
        {
            sum += inputs[i] * weights[i];
        }
        sum += bias;
        return isOutputLayer ? sum : ActivationFunction(sum);
    }


    private float ActivationFunction(float value)
    {
        //LeakyReLU
        float negativeGradient = 0.01f;
        return value > 0 ? value : value * negativeGradient;
    }

    public void RandomlyAdjustWeightsAndBias(float weightAdjustmentMultiplier)
    {
        for(int i = 0; i < weights.Length; i++)
        {
            weights[i] += RandomFloatInRange(-1, 1) * weightAdjustmentMultiplier  * RandomFloatInRange(0.5f, 2f);;
        }
    }
}

class WeightInitializer
{
    private static System.Random random = new System.Random();
    private static float RandomGaussian()
    {
        double r1 = random.NextDouble();
        double r2 = random.NextDouble();

        //box muller transform
        double randomGuassianVarience = Math.Sqrt(-2.0 * Math.Log(r1)) * Math.Cos(2.0 * Math.PI * r2);

        return (float)randomGuassianVarience;
    }

    private static float HeInitialization(int numberOfInputs)
    {
        //standard deviation for the normal distribution
        float stdDev = (float)Math.Sqrt(2.0 / numberOfInputs);

        return RandomGaussian() * stdDev;
    }

    public static float RandomFloatInRange(float min, float max)
    {
        System.Random rand = new System.Random();
        return (float)(min + rand.NextDouble() * (max - min));
    }

    public static float[] GetRandomWeights(int numberOfInputs)
    {
        float[] initializedWeights = new float[numberOfInputs];
        for (int i = 0; i < numberOfInputs; i++)
        {
            initializedWeights[i] = HeInitialization(numberOfInputs);
        }
        return initializedWeights;
    }
}

public struct State
{
    private float normalizedCarSpeed;
    private float normalizedDistanceToNextCheckpoint;
    private float normalizedDistanceToWallForwards;
    private float normalizedDistanceToWallRight;
    private float normalizedDistanceToWallLeft;
    private float normalizedRotationRelativeToCheckpoint;
    public State(float carSpeed, float maxCarSpeed, float distanceFromLastCheckPointToNext, float distanceToNextCheckpoint, float rotationRelativeToNextCheckpoint, float carViewDistanceToWall, float distanceToWallForwards, float distanceToWallRight, float distanceToWallLeft)
    {
        normalizedCarSpeed = carSpeed / maxCarSpeed;
        normalizedDistanceToNextCheckpoint = 1-(distanceToNextCheckpoint / distanceFromLastCheckPointToNext);
        normalizedDistanceToWallForwards = distanceToWallForwards / carViewDistanceToWall;

        normalizedDistanceToWallRight = distanceToWallRight / carViewDistanceToWall;
        normalizedDistanceToWallLeft = distanceToWallLeft / carViewDistanceToWall;
        normalizedRotationRelativeToCheckpoint = rotationRelativeToNextCheckpoint / 360f;

    }

    public float[] createInputVector()
    {
        float[] inputVector = new float[] { normalizedCarSpeed, normalizedDistanceToNextCheckpoint, normalizedDistanceToWallForwards, normalizedDistanceToWallRight, normalizedDistanceToWallLeft, normalizedRotationRelativeToCheckpoint };
        // Debug.Log($"Input Vector: {string.Join(", ", inputVector)}");
        return inputVector;
    }
    public float NormalizedCarSpeed { get { return normalizedCarSpeed; } }
    public float NormalizedDistanceToNextCheckpoint { get { return normalizedDistanceToNextCheckpoint; } }
    public float NormalizedDistanceToWallForwards { get { return normalizedDistanceToWallForwards; } }
    public float NormalizedDistanceToWallRight { get { return normalizedDistanceToWallRight; } }
    public float NormalizedDistanceToWallLeft { get { return normalizedDistanceToWallLeft; } }
    public float NormalizedRotationRelativeToCheckpoint {get {return normalizedRotationRelativeToCheckpoint; }}

}

public static class Sort
{
    public static float[] MergeSort(float[] array, int start, int end)
    {
        if (start >= end) return new float[] { array[start] };

        int mid = (start + end) / 2;

        var leftSorted = MergeSort(array, start, mid);
        var rightSorted = MergeSort(array, mid + 1, end);

        return Merge(leftSorted, rightSorted);
    }

    public static float[] Merge(float[] left, float[] right)
    {
        float[] merged = new float[left.Length + right.Length];
        int leftIndex = 0, rightIndex = 0, mergeIndex = 0;

        while (leftIndex < left.Length && rightIndex < right.Length)
        {
            if (left[leftIndex] <= right[rightIndex]) merged[mergeIndex++] = left[leftIndex++];
            else merged[mergeIndex++] = right[rightIndex++];
        }

        while (leftIndex < left.Length) merged[mergeIndex++] = left[leftIndex++];

        while (rightIndex < right.Length) merged[mergeIndex++] = right[rightIndex++];

        return merged;
    }
}
