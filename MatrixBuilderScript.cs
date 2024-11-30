using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class MatrixBuilderScript : MonoBehaviour
{
    // fields
    private double[,] edgeVelocities = new double[5, 2];
    private char[] edgeTypes = new char[5];

    private bool useTimeOfDayForCalculationUser;    
    private bool useTimeOfDayForCalculationDB;
    public bool useTimeOfDayForCalculation;

    public bool stepFree {get; private set;}

    public int[] nodesForMatrix {get; private set;}

    public double[,] distanceMatrixDefault {get; private set;}
    public char[,] infoMatrixDefault {get; private set;}

    public double[,] distanceMatrixOneWay {get; private set;}
    public char[,] infoMatrixOneWay {get; private set;}

    public double[,] timeMatrixDefault;

    public double[,] timeMatrixStairsLifts {get; private set;}

    public int numberOfNodes {get; private set;}

    [SerializeField] private DatabaseHelperScript databaseHelper;

    Stopwatch stopwatch = new Stopwatch();
    

    // constructors
    public MatrixBuilderScript() {
        
        // write edge types for array
        edgeTypes = new char[5] {'O', 'I', 'C', 'S', 'L'};

        // get velocities
        // go through all 5 x 2 values and place in edge Velocities from DB
        edgeVelocities[0, 0] = databaseHelper.GetVelocityValue(edgeTypes[0], false); // outside normal
        edgeVelocities[0, 1] = databaseHelper.GetVelocityValue(edgeTypes[0], true); // outside slow
        edgeVelocities[1, 0] = databaseHelper.GetVelocityValue(edgeTypes[1], false); // inside normal
        edgeVelocities[1, 1] = databaseHelper.GetVelocityValue(edgeTypes[1], true); // inside slow
        edgeVelocities[2, 0] = databaseHelper.GetVelocityValue(edgeTypes[2], false); // commonly congested normal
        edgeVelocities[2, 1] = databaseHelper.GetVelocityValue(edgeTypes[2], true); // commonly congested slow
        edgeVelocities[3, 0] = databaseHelper.GetVelocityValue(edgeTypes[3], false); // stairs normal
        edgeVelocities[3, 1] = databaseHelper.GetVelocityValue(edgeTypes[3], true); // stairs slow
        edgeVelocities[4, 0] = databaseHelper.GetVelocityValue(edgeTypes[4], false); // lift normal
        edgeVelocities[4, 1] = databaseHelper.GetVelocityValue(edgeTypes[4], true); // lift slow

        // now check settings
        useTimeOfDayForCalculationUser = true; // sourced from user settings
        useTimeOfDayForCalculationDB = databaseHelper.GetTimeOfDayDB(); // sourced from DB settings

        // if DB sets it as false, then it is false, otherwise, follow user's settings
        // this is just 'and' gate
        useTimeOfDayForCalculation = useTimeOfDayForCalculationUser && useTimeOfDayForCalculationDB;

        // get user settings for step free access
        stepFree = false; // false means using stairs

        // set non null values for array/matrices
        numberOfNodes = databaseHelper.GetNumberOfNodes();
        nodesForMatrix = new int[numberOfNodes];

        distanceMatrixDefault = new double[numberOfNodes, numberOfNodes];
        infoMatrixDefault = new char[numberOfNodes, numberOfNodes];

        distanceMatrixOneWay = new double[numberOfNodes, numberOfNodes];
        infoMatrixOneWay = new char[numberOfNodes, numberOfNodes];

        timeMatrixDefault = new double[numberOfNodes, numberOfNodes];

        timeMatrixStairsLifts = new double[numberOfNodes, numberOfNodes];
    }

    // methods

    /**
    This function calls functions that queries the database to create the non directional
    dist dist matrix and info matrix used by the program to create a time dist matrix.
    */
    public (int[], double[,], char[,]) BuildNormalMatrices() {

        // get number of nodes so a n x n matrix array can be defined
        int numberOfNodes = databaseHelper.GetNumberOfNodes();

        // make node array
        int[] nodeArray = new int[numberOfNodes];
        var (nodeFields, nodeValues) = databaseHelper.GetNodes();

        // just in case the order has been changed, get index manually
        int nodeIndex = nodeFields.IndexOf("node_id");
        
        // now loop through nodeValues and put value in nodeArray
        for (int i = 0; i < numberOfNodes; i++) {
            
            nodeArray[i] = Convert.ToInt32(nodeValues[i][nodeIndex]);
        }

        // initialise distance matrix
        double[,] distanceMatrix = new double[numberOfNodes, numberOfNodes];

        // initialise info matrix
        char[,] infoMatrix = new char[numberOfNodes, numberOfNodes];

        // query db for all edges
        var (edgeFields, edgeValues) = databaseHelper.GetEdges();

        //get number of times need to loop from the edgeValues
        int numberOfEdges = edgeValues.Count;

        // just in case the order has been changed, get indexes manually
        int node1FieldIndex = edgeFields.IndexOf("node_1_id");
        int node2FieldIndex = edgeFields.IndexOf("node_2_id");
        int weightFieldIndex = edgeFields.IndexOf("weight");
        int infoFieldIndex = edgeFields.IndexOf("edge_type_id");

        // loop through edges and update matrix
        for (int i = 0; i < numberOfEdges; i++) {

            // get node id from edge values
            int node1ID = Convert.ToInt32(edgeValues[i][node1FieldIndex]); // returns a node id
            int node2ID = Convert.ToInt32(edgeValues[i][node2FieldIndex]); // returns a node id

            // get node index from node array
            int node1Index = Array.IndexOf(nodeArray, node1ID);
            int node2Index = Array.IndexOf(nodeArray, node2ID);

            // get weight value from edge values
            double weightVal = Math.Round(Convert.ToDouble(edgeValues[i][weightFieldIndex]), 1);

            // get edge type / info value from edge values
            char infoVal = Convert.ToChar(edgeValues[i][infoFieldIndex]);
                                 
            // now update matrices

            // put in both 1, 2 and 2, 1 because non directional            
            distanceMatrix[node1Index, node2Index] = weightVal;
            distanceMatrix[node2Index, node1Index] = weightVal;

            // put in both 1, 2 and 2, 1 because non directional            
            infoMatrix[node1Index, node2Index] = infoVal;
            infoMatrix[node2Index, node1Index] = infoVal;
        }

        // go through info matrix and change values to '0'
        for (int i = 0; i < numberOfNodes; i++) {
            for (int j = 0; j < numberOfNodes; j++) {
                if (infoMatrix[i, j] == '\0') {
                    infoMatrix[i, j] = '0';
                }
            }
        }     

        return (nodeArray, distanceMatrix, infoMatrix);
    }

    /**
    This function takes the results of the above function (node array, dist matrix, info amatrix)
    and sets all values to zero where the one way system applies.
    */
    public (double[,], char[,]) BuildOWSMatrices(int[] nodeArray, double[,] distanceMatrix, char[,] infoMatrix) {

        // clone matrices as they got passed by ref not by val
        var distanceMatrixOneWay = (double[,])distanceMatrix.Clone();
        var infoMatrixOneWay = (char[,])infoMatrix.Clone();

        // get all one-way edges from db
        var (edgeFields, edgeValues) = databaseHelper.GetOneWayEdges();

        //get number of times need to loop from the edgeValues
        int numberOfEdges = edgeValues.Count;

        // just in case the order has been changed, get indexes manually
        int node1FieldIndex = edgeFields.IndexOf("node_1_id");
        int node2FieldIndex = edgeFields.IndexOf("node_2_id");

        for (int i = 0; i < numberOfEdges; i++) {

            // get node id from edge values
            int node1ID = Convert.ToInt32(edgeValues[i][node1FieldIndex]); // returns a node id
            int node2ID = Convert.ToInt32(edgeValues[i][node2FieldIndex]); // returns a node id

            // get node index from node array
            int node1Index = Array.IndexOf(nodeArray, node1ID);
            int node2Index = Array.IndexOf(nodeArray, node2ID);

            // set the edge from node 2 to node 1 to 0 in matrices
            distanceMatrixOneWay[node2Index, node1Index] = 0;
            infoMatrixOneWay[node2Index, node1Index] = '0';
        } 

        return (distanceMatrixOneWay, infoMatrixOneWay);
    }

    /**
    This functions configures a time matrix so that pathfinding can be carried out.
    It takes two matrices - the first is a distance distance matrix representing the connections between
    nodes on a graph in metres, and the second represnting what type of path each edge is.
    It then estimates time for each non 0 edge in the matrix, also considering the time of day if this is enabled
    in the user settings and database settings.
    */
    public double[,] ConfigureTimeMatrix(double[,] distanceMatrix, char[,] infoMatrix) {

        // figure out if slow (congested) values for velocity should be used
        // calculated once so not repeating each loop
        bool useSlowVal = NearCongestionTime() && useTimeOfDayForCalculation;

        // initialise temp variables within loop
        double distance; // in metres
        double time; // in seconds
        char info;

        // initialise returned matrix
        double[,] timeMatrix = new double[numberOfNodes, numberOfNodes];

        for (int rowNum = 0; rowNum < numberOfNodes; rowNum++) {
            for (int colNum = 0; colNum < numberOfNodes; colNum++) {

                distance = distanceMatrix[rowNum, colNum];
                if (distance > 0) {
                    info = infoMatrix[rowNum, colNum];
                    time = EstimateTimeFromDistance(distance, info, useSlowVal);
                    timeMatrix[rowNum, colNum] = time;
                }
                
            }
        }

        return timeMatrix;
    }

    /**
    This function does the individual time estimation part of configuring the time matrix for a single
    distance and info instance.
    It takes both these values, considers what type of path it is, so it can then assign a velocity, then uses the
    time = distance / speed formula to return a value for time.
    */
    public double EstimateTimeFromDistance(double distance, char info, bool useSlowVal) {
        
        double realVelocity;
        double time;
        
        // get index of the edge type (info)
        int edgeTypeIndex = Array.IndexOf(edgeTypes, info);
        
        // get velocity from edge velocities matrix
        realVelocity = edgeVelocities[edgeTypeIndex, Convert.ToInt32(useSlowVal)];

        // use t = s / v formula
        time = distance/realVelocity;

        return Math.Round(time, 1);
    }

    /**
    This function is called to check how near the current time is to a list of congestion times.
    Makes use of the TimeSpan datatypes to represent times in a day
    */
    public bool NearCongestionTime() {

        bool isNearCongestionTime = false;

        // using timespans as times of the day
        // source from database

        // get congestion times and duration
        List<TimeSpan> congestionTimes = databaseHelper.GetCongestionTimes(); // these are the busy corridor times
        TimeSpan congestionDuration = databaseHelper.GetCongestionDuration(); // 3 mins

        // get current time of day
        TimeSpan currentTime = DateTime.Now.TimeOfDay;

        for (int i = 0; i < congestionTimes.Count; i++) {
            // if current time is within allowed duration of congestionTimes[i]
            // current time has to be greater than the start of the congestion time
            // and less than congestion time + congestion duration
            if (currentTime >= congestionTimes[i] && currentTime <= congestionTimes[i] + congestionDuration) {
                isNearCongestionTime = true;
                break;
            }
        }

        //return isNearCongestionTime;
        return isNearCongestionTime;
    }

    /**
    This function is used to adjust the time matrix so that if step-free access is selected
    then stairs are not considered when pathfinding. The opposite is true so that people who can use
    stairs get directed through them.
    It iteratively looks through the info matrix for either S or L and adjusts the matrix correctly.
    */
    public double[,] AdjustStairsLifts(double[,] timeMatrix, char[,] infoMatrix) {

        //saves computation time only checking this once instead of for each iteration
        if (stepFree) { // so get rid of edges for stairs
            for (int rowNum = 0; rowNum < numberOfNodes; rowNum++) {
                for (int colNum = 0; colNum < numberOfNodes; colNum++) {
                    if (infoMatrix[rowNum, colNum] == 'S') {
                        if (rowNum != 13 && colNum != 13) {
                            timeMatrix[rowNum, colNum] = 0;
                        }
                        
                    }
                }
            }
        }
        
        else { // so get rid of edges for lifts
            for (int rowNum = 0; rowNum < numberOfNodes; rowNum++) {
                for (int colNum = 0; colNum < numberOfNodes; colNum++) {
                    if (infoMatrix[rowNum, colNum] == 'L') {
                        timeMatrix[rowNum, colNum] = 0;
                    }
                }
            }
        }
        
        return timeMatrix;
    }

    /**
    This procedure links together all of the necessary functions for when the matrices
    are built for typical use by a regular user.
    */
    public void BuildMatricesForPathfinding() {

        // start stopwatch
        stopwatch.Start();

        var(nfm, dmn, imn) = BuildNormalMatrices();
        nodesForMatrix = nfm;
        distanceMatrixDefault = dmn;
        infoMatrixDefault = imn;
        var(dmows, imows) = BuildOWSMatrices(nodesForMatrix, distanceMatrixDefault, infoMatrixDefault);
        distanceMatrixOneWay = dmows;
        infoMatrixOneWay = imows;
        timeMatrixDefault = ConfigureTimeMatrix(distanceMatrixDefault, infoMatrixDefault);
        if (!stepFree) {
            timeMatrixStairsLifts = AdjustStairsLifts(ConfigureTimeMatrix(distanceMatrixOneWay, infoMatrixOneWay), infoMatrixOneWay);
        }
        else {
            timeMatrixStairsLifts = AdjustStairsLifts(timeMatrixDefault, infoMatrixDefault);
        }

        // stop stopwatch
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Elapsed Matrix Time: {stopwatch.ElapsedMilliseconds} ms\n");
    }
}
