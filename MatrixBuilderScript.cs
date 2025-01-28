using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class MatrixBuilderScript : MonoBehaviour
{
    // fields
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private UserSettingsScript userSettings;

    private double[,] edgeVelocities = new double[5, 2];
    private char[] edgeTypes = new char[5];

    private bool useTimeOfDayForCalculationUser;    
    private bool useTimeOfDayForCalculationDB;
    public bool useTimeOfDayForCalculation;
    public bool matricesUseCongestion;

    public bool stepFree {get; private set;}

    public int[] nodesForMatrix {get; private set;}

    public double[,] distanceMatrixDefault {get; private set;}
    public char[,] infoMatrixDefault {get; private set;}

    public double[,] distanceMatrixOneWay {get; private set;}
    public char[,] infoMatrixOneWay {get; private set;}

    public double[,] timeMatrixDefault;

    public double[,] timeMatrixStairs {get; private set;}
    public double[,] timeMatrixLifts {get; private set;}

    public int numberOfNodes {get; private set;}

    Stopwatch stopwatch = new Stopwatch();
    

    // constructor
    void Start() {
        ResetFields();        
    }

    void Update() {
        if (NearCongestionTime() && useTimeOfDayForCalculation != matricesUseCongestion) {
            ResetFields();
        }
    }


    // methods

    public void ResetFields() {

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

        CheckTimeOfDayEstimation();

        // get user settings for step free access
        stepFree = userSettings.stepFree; // false means using stairs

        // set non null values for array/matrices
        numberOfNodes = databaseHelper.GetNumberOfNodes();
        nodesForMatrix = new int[numberOfNodes];

        distanceMatrixDefault = new double[numberOfNodes, numberOfNodes];
        infoMatrixDefault = new char[numberOfNodes, numberOfNodes];

        distanceMatrixOneWay = new double[numberOfNodes, numberOfNodes];
        infoMatrixOneWay = new char[numberOfNodes, numberOfNodes];

        timeMatrixDefault = new double[numberOfNodes, numberOfNodes];

        timeMatrixStairs = new double[numberOfNodes, numberOfNodes];
        timeMatrixLifts = new double[numberOfNodes, numberOfNodes];
    }

    /**
    This function calls functions that queries the database to create the non directional
    dist dist matrix and info matrix used by the program to create a time dist matrix.
    */
    public void BuildNormalMatrices() {

        // get number of nodes so a n x n matrix array can be defined
        int numberOfNodes = databaseHelper.GetNumberOfNodes();

        // make node array
        nodesForMatrix = databaseHelper.GetNodeIDsInDatabase();

        // initialise distance matrix
        distanceMatrixDefault = new double[numberOfNodes, numberOfNodes];

        // initialise info matrix
        infoMatrixDefault = new char[numberOfNodes, numberOfNodes];

        string[,] edgeInfo = databaseHelper.GetEdgesToBuildMatrices();
        int numberOfEdges = edgeInfo.GetLength(0);

        // loop through edges and update matrix
        for (int i = 0; i < numberOfEdges; i++) {

            // get node id from edge values
            int node1ID = Convert.ToInt32(edgeInfo[i, 0]); // returns a node id
            int node2ID = Convert.ToInt32(edgeInfo[i, 1]); // returns a node id

            // get node index from node array
            int node1Index = Array.IndexOf(nodesForMatrix, node1ID);
            int node2Index = Array.IndexOf(nodesForMatrix, node2ID);

            // get weight value from edge values
            double weightVal = Math.Round(Convert.ToDouble(edgeInfo[i, 2]), 1);

            // get edge type / info value from edge values
            char infoVal = Convert.ToChar(edgeInfo[i, 3]);
                                 
            // now update matrices

            // put in both 1, 2 and 2, 1 because non directional            
            distanceMatrixDefault[node1Index, node2Index] = weightVal;
            distanceMatrixDefault[node2Index, node1Index] = weightVal;

            // put in both 1, 2 and 2, 1 because non directional            
            infoMatrixDefault[node1Index, node2Index] = infoVal;
            infoMatrixDefault[node2Index, node1Index] = infoVal;
        }

        // go through info matrix and change values to '0'
        for (int i = 0; i < numberOfNodes; i++) {
            for (int j = 0; j < numberOfNodes; j++) {
                if (infoMatrixDefault[i, j] == '\0') {
                    infoMatrixDefault[i, j] = '0';
                }
            }
        }
    }

    /**
    This function takes the results of the above function (node array, dist matrix, info amatrix)
    and sets all values to zero where the one way system applies.
    */
    public void BuildOWSMatrices() {

        // clone matrices as they got passed by ref not by val
        distanceMatrixOneWay = (double[,])distanceMatrixDefault.Clone();
        infoMatrixOneWay = (char[,])infoMatrixDefault.Clone();

        // get all one-way edges from db
        string[,] edgeInfo = databaseHelper.GetOneWayEdgesToBuildMatrix();
        int numberOfEdges = edgeInfo.GetLength(0);

        for (int i = 0; i < numberOfEdges; i++) {

            // get node id from edge values
            int node1ID = Convert.ToInt32(edgeInfo[i, 0]); // returns a node id
            int node2ID = Convert.ToInt32(edgeInfo[i, 1]); // returns a node id

            // get node index from node array
            int node1Index = Array.IndexOf(nodesForMatrix, node1ID);
            int node2Index = Array.IndexOf(nodesForMatrix, node2ID);

            // set the edge from node 2 to node 1 to 0 in matrices
            // because in db, one-way is defined as one-way from 1 to 2
            distanceMatrixOneWay[node2Index, node1Index] = 0;
            infoMatrixOneWay[node2Index, node1Index] = '0';
        }
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
                    time = EstimateTimeFromDistance(distance, info, matricesUseCongestion);
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

        return isNearCongestionTime;
    }

    /**
    This function is used to adjust the time matrix so that if step-free access is selected
    then stairs are not considered when pathfinding. The opposite is true so that people who can use
    stairs get directed through them.
    It iteratively looks through the info matrix for either S or L and adjusts the matrix correctly.
    */
    public double[,] AdjustStairsLifts(double[,] timeMatrix, char[,] infoMatrix, bool stepFree) {

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

        BuildNormalMatrices();
        BuildOWSMatrices();
        timeMatrixDefault = ConfigureTimeMatrix(distanceMatrixDefault, infoMatrixDefault);
        // fill time matrix stairs and time matrix lifts

        // not step free, so one way system
        if (userSettings.oneWaySystem) {
        timeMatrixStairs = AdjustStairsLifts(ConfigureTimeMatrix(distanceMatrixOneWay, infoMatrixOneWay), infoMatrixOneWay, false);
        }
        else {
        timeMatrixStairs = AdjustStairsLifts(ConfigureTimeMatrix(distanceMatrixDefault, infoMatrixDefault), infoMatrixDefault, false);
        }

        // step free so no one way system
        timeMatrixLifts = AdjustStairsLifts(timeMatrixDefault, infoMatrixDefault, true);

        // stop stopwatch
        stopwatch.Stop();

        UnityEngine.Debug.Log($"Elapsed Matrix Time: {stopwatch.ElapsedMilliseconds} ms\n");
        stopwatch.Reset();
    }

    /**
    This function chekcs if time estimation should be used in calculations.
    */
    public void CheckTimeOfDayEstimation() {
        // now check settings
        useTimeOfDayForCalculationUser = userSettings.useTimeOfDayForCalculation; // sourced from user settings
        useTimeOfDayForCalculationDB = databaseHelper.GetUseTimeOfDayDB(); // sourced from DB settings

        // if DB sets it as false, then it is false, otherwise, follow user's settings
        // this is just 'and' gate
        useTimeOfDayForCalculation = useTimeOfDayForCalculationUser && useTimeOfDayForCalculationDB;
    }
}
