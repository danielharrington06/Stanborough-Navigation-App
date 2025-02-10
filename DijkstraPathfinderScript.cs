using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using TMPro;

public class DijkstraPathfinderScript : MonoBehaviour
{
    // fields
    [SerializeField] private MatrixBuilderScript matrixBuilder;
    [SerializeField] private DatabaseHelperScript databaseHelper;
    [SerializeField] private PathRendererScript pathRenderer;
    [SerializeField] private UserSettingsScript userSettings;

    [Header ("UI Elements")]
    [SerializeField] private TMP_InputField startLocationInput;
    [SerializeField] private TMP_InputField targetLocationInput;
    [SerializeField] private TMP_Text timeOutput;
    [SerializeField] private TMP_Text ETAOutput;
    [SerializeField] private TMP_Text distanceOutput;
    [SerializeField] public TMP_Text errorMessage;

    private double timeSecsModifier; // used to consider time getting in and out of classrooms for example
    
    int numberOfNodes;

    private int[] nodesForMatrix;
    private double[,] timeMatrix;
    private double[,] distanceMatrix;
    private char[,] infoMatrix;

    public Location startLocation {get; private set;}
    public Location targetLocation {get; private set;}

    /* public string startLocationText;
    public string targetLocationText; */

    /* public int startNode; // node id from nodesForMatrix
    public int targetNode; */ // node id from nodesForMatrix

    /* public string startLocation; // node or room
    public string targetLocationText; */ // node or room

    /* private string startType; // "" if undefined, "N" if node, "RN" if room that is a node, "RNC" if room thats connected to a node, "REU" if undirectional edge, "RED" if directional edge"
    private string targetType; */ // "" if undefined, "N" if node, "RN" if room that is a node, "RNC" if room thats connected to a node, "REU" if undirectional edge, "RED" if directional edge"
    [Header ("Pathfinding Variables")]
    public double[] dijkstraDistances;
    public double estimatedTimeInSecs;
    public TimeSpan estimatedTime;
    public TimeSpan estimatedTimeOfArrival;
    public double estimatedDistance;
    public bool showResults;
    public bool startDijkstra = false;
    private Stopwatch stopwatch = new Stopwatch();

    public List<int> dijkstraPath; // path of node id's

    public List<double[]> floor0Path {get; private set;} // path of coordinates
    public List<double[]> floor1Path {get; private set;} // path of coordinates

    public List<int> floor0BreakIndex {get; private set;} // path of coordinates
    public List<int> floor1BreakIndex {get; private set;} // path of coordinates

    // constructor
    void Start(){
        startLocation = new Location(databaseHelper);
        targetLocation = new Location(databaseHelper);

        ResetFields();

    }

    // methods
    
    void Update() {
        if (startDijkstra == true) {
            showResults = false;
            if (startLocation.id != "" && targetLocation.id != "") {
                CarryOutAndInterpretPathfinding();
                OutputTextResults();
                UnityEngine.Debug.Log($"Elapsed Dijkstra Time: {stopwatch.ElapsedMilliseconds} ms\n");
            }
            else if (startLocation.id == "" && startLocation.userText == "" && targetLocation.id == "" && targetLocation.userText == "") {
                // both empty
                errorMessage.text = "Please enter a start and target location.";
            }
            else if (startLocation.id == "" && startLocation.userText != "" && targetLocation.id == "" && targetLocation.userText == "") {
                // start invalid and target empty
                errorMessage.text = "Please enter a valid start location and a target location.";
            }
            else if (startLocation.id == "" && startLocation.userText == "" && targetLocation.id == "" && targetLocation.userText != "") {
                // start valid and target invalid
                errorMessage.text = "Please enter a start location and a valid target location.";
            }
            else if (startLocation.id == "" && startLocation.userText != "" && targetLocation.id == "" && targetLocation.userText != "") {
                // start and target invalid
                errorMessage.text = "Please enter valid start and target locations.";
            }
            // by now all possibilitise have been covered for both not good
            else if (startLocation.id == "" && startLocation.userText == "") {
                // start empty
                errorMessage.text = "Please enter a start location.";
            }
            else if (startLocation.id == "" && startLocation.userText != "") {
                // start invalid
                errorMessage.text = "Please enter a valid start location.";
            }
            else if (targetLocation.id == "" && targetLocation.userText == "") {
                // target empty
                errorMessage.text = "Please enter a target location.";
            }
            else if (targetLocation.id == "" && targetLocation.userText != "") {
                // target invalid
                errorMessage.text = "Please enter a valid target location.";
            }
            startDijkstra = false;
        }

        // check if no longer showing results but they are shown
        if (showResults == false && TextResultsVisible()) {
            ResetOutput();
        }

        // eta check
        if (estimatedTimeOfArrival != EstimateTimeOfArrival(estimatedTime) && showResults) {
            estimatedTimeOfArrival = EstimateTimeOfArrival(estimatedTime);
            OutputTextResults();
        }
    }

    /**
    This function will reset all the variables so can be used in Start and before pathfinding.
    */
    public void ResetFields() {

        // need to get from db
        timeSecsModifier = 0;

        // set non-null values for array and matrices
        numberOfNodes = matrixBuilder.numberOfNodes;

        nodesForMatrix = matrixBuilder.nodesForMatrix;
        //UnityEngine.Debug.Log("tST" + userSettings.stepFree);
        if (!userSettings.stepFree) { // not step free, so have to follow one-way
            timeMatrix = matrixBuilder.timeMatrixStairs;
            if (userSettings.oneWaySystem) {
                distanceMatrix = matrixBuilder.distanceMatrixOneWay;
                infoMatrix = matrixBuilder.infoMatrixOneWay;
            }
            else {
                distanceMatrix = matrixBuilder.distanceMatrixDefault;
                infoMatrix = matrixBuilder.infoMatrixDefault;
            }
        }
        else {
            timeMatrix = matrixBuilder.timeMatrixLifts;
            distanceMatrix = matrixBuilder.distanceMatrixDefault;
            infoMatrix = matrixBuilder.infoMatrixDefault;
        }
        
        dijkstraDistances = new double[numberOfNodes];
        estimatedTime = new TimeSpan(0, 0, 0);
        estimatedTimeOfArrival = new TimeSpan(0, 0, 0);

        dijkstraPath = new List<int>();

        floor0Path = new List<double[]>();
        floor1Path = new List<double[]>();

        floor0BreakIndex = new List<int>();
        floor1BreakIndex = new List<int>();

        estimatedDistance = 0;

        showResults = false;

        stopwatch.Reset();

    }

    /**
    This function carries out Dijkstra's Algorithm to finds the shortest path between a start node and every other node.
    It takes the matrix and a node to start the pathfinding from.
    A distance of 0 in the matrix either means that the nodes are not directly connected or that the edge would be from 
    one node to the same, and an edge of this type does not exist in the database.
     */
    public double[] DijkstrasAlgorithm(double[,] matrix, int startNode) {
        
        // convert from node id to index in array
        int startNodeIndex = Array.IndexOf(nodesForMatrix, startNode);

        // to keep track of visited locations to not go to a previously visited node and to stay efficient
        bool[] visitedNodes = new bool[numberOfNodes];
        // this holds the working (then finalised) distances from the start node to each other node
        double[] dijkstraDistances = new double[numberOfNodes];

        // initial setup
        for (int i = 0; i < numberOfNodes; i++) {
            visitedNodes[i] = false;
            dijkstraDistances[i] = matrix[startNodeIndex, i];
        }
        
        int currentNodeIndex = startNodeIndex;

        // setup variables before while loop
        double distanceToNode;
        double outgoingDist;
        double lowestVal;
        int lowestValIndex;

        // repeats as long as there is at least one unvisited node
        while (visitedNodes.Contains(false)) {

            visitedNodes[currentNodeIndex] = true;
            distanceToNode = dijkstraDistances[currentNodeIndex];

            // iterate over other nodes
            for (int j = 0; j < numberOfNodes; j++) {
                // if already visited node, skip
                if (visitedNodes[j] || currentNodeIndex == j) {}

                //if unvisited, consider
                else {
                    //distance from currentNode to the other node
                    outgoingDist = matrix[currentNodeIndex, j];
                    // this distance may be 0, so no direct connection or it is itself
                    if (outgoingDist == 0) {}

                    // if directly connected, evaluate
                    else {
                        // is a shorter path
                        // second condition is where no path to node has been found yet, so current value is 0
                        if (distanceToNode + outgoingDist < dijkstraDistances[j] || dijkstraDistances[j] == 0) {
                            // so update dijkstraDistances
                            dijkstraDistances[j] = Math.Round(distanceToNode + outgoingDist, 1);
                        }
                    }
                }
            }
            // now select next node
            lowestVal = double.MaxValue;
            lowestValIndex = -1;
            for (int k = 0; k < numberOfNodes; k++) {
                if (visitedNodes[k] == false) {
                    if (dijkstraDistances[k] < lowestVal && dijkstraDistances[k] != 0) {
                        lowestVal = dijkstraDistances[k];
                        lowestValIndex = k;
                    }
                }
            }
            if (lowestValIndex == -1) {
                for (int i = 0; i < dijkstraDistances.Length; i++) {
                    if (dijkstraDistances[i] == 0 && nodesForMatrix[i] != startNode) {
                        dijkstraDistances[i] = double.MaxValue;
                        //UnityEngine.Debug.Log($"{nodesForMatrix[i]} set to max value.");
                    }
                }
                return dijkstraDistances;
            }
            currentNodeIndex = lowestValIndex;
            
        }

        return dijkstraDistances;
    }

    /**
    This function takes a node index and a list (the resulting dijkstra distances)
    and returns the value at the index specified. Most of the hard work has already
    been done for the estimation of time.
    */
    public double EstimateTime(double[] dijkstraDistances, int targetNode) {
        
        // convert from node id target node to index in array
        int targetNodeIndex = Array.IndexOf(nodesForMatrix, targetNode);

        // deal with it being out of range
        if (targetNodeIndex >= dijkstraDistances.Length) {
            throw new FormatException($"List index '{targetNodeIndex}' out of range.");
        }

        double timeInSecs = dijkstraDistances[targetNodeIndex];
        timeInSecs += timeSecsModifier; // at 0 for testing, get from database later
        
        return timeInSecs;
    }

    /**
    This function takes seconds and returns that in hours minutes and seconds.
    */
    public TimeSpan ConvertSecsToTimeFormat(double timeInSecs) {

        // time as timespan format
        TimeSpan time;
        
        // throw error if time in secs is negative
        if (timeInSecs < 0) {
            throw new ArgumentOutOfRangeException(nameof(timeInSecs), "The value cannot be negative.");
        }

        int intTimeInSecs = Convert.ToInt32(timeInSecs); // can only be integer seconds
        // now manually use mod function to find hours, mins and secs
        // hours
        int hours = intTimeInSecs / (60*60);
        if (hours > 0) {
            intTimeInSecs -= hours*60*60;
        }
        // minutes
        int minutes = intTimeInSecs / 60;
        if (minutes > 0) {
            intTimeInSecs -= minutes*60;
        }
        // seconds are just intTimeInSecs
        time = new TimeSpan(hours, minutes, intTimeInSecs);

        return time;
    }
    /**
    This function takes a time duration and adds it to the current time to produce the ETA.
    */
    public TimeSpan EstimateTimeOfArrival(TimeSpan timeDuration) {
        // get current time of day
        TimeSpan currentTime = DateTime.Now.TimeOfDay;
        // add time duration
        TimeSpan estimatedTimeOfArrival = currentTime + timeDuration;
        // now round seconds
        estimatedTimeOfArrival = new TimeSpan(estimatedTimeOfArrival.Hours, estimatedTimeOfArrival.Minutes, 0);
        return estimatedTimeOfArrival;
    }

    /**
    This function uses the time matrix, dijkstra distances, start and target nodes to
    back track and find the optimal path.
    */
    public List<int> FindDijkstrasPath(double[,] matrix, double[] dijkstraDistances, int startNode, int targetNode) {
        
        // convert from node id to index in array
        int startNodeIndex = Array.IndexOf(nodesForMatrix, startNode);
        int targetNodeIndex = Array.IndexOf(nodesForMatrix, targetNode);

        // use a list to store path as it will change in size
        List<int> dijkstraPath = new List<int> {targetNode};

        int currentNodeIndex = targetNodeIndex;
        int currentNode = targetNode;

        List<double> attachedEdges = new List<double>();
        List<int> possibleNodes = new List<int>();

        while (currentNodeIndex != startNodeIndex) {
            // get row currentNodeIndex from matrix
            attachedEdges.Clear();
            for (int i = 0; i < numberOfNodes; i++) {
                // seems wrong to have [i, currentNode]
                // however we are backtracking so its all edges going into current node
                attachedEdges.Add(matrix[i, currentNodeIndex]);
            }

            // indexes in list are part of optimal path
            possibleNodes.Clear();
            for (int i = 0; i < numberOfNodes; i++) {
                // add optimal edges to list

                if(currentNode == 29 && attachedEdges[i] != 0) {
                    UnityEngine.Debug.Log($"Node {currentNode}, to node {nodesForMatrix[i]}: {attachedEdges[i]}");
                    UnityEngine.Debug.Log($"{currentNodeIndex}: {Math.Round(dijkstraDistances[currentNodeIndex] - attachedEdges[i], 1)}");
                    UnityEngine.Debug.Log(dijkstraDistances[i]);
                }
                
                // current dijkstra time - time to node =? dijkstra time to node
                if (Math.Round(dijkstraDistances[currentNodeIndex] - attachedEdges[i], 1) == dijkstraDistances[i]) {
                    // if dijkstra path doesnt contain the node and check that the node is connected by an edge..
                    if (!dijkstraPath.Contains(nodesForMatrix[i]) && attachedEdges[i] != 0){ 
                        possibleNodes.Add(i);
                    }
                }
            }

            // there is a possibility that there are multiple edges part of the dijkstra path
            // any of them should work
            // if there is only one, choose it
            // if mutliple, choose a random one

            if (possibleNodes.Count == 1) {
                // consider this node as the current node
                currentNodeIndex = possibleNodes[0];
                // get node id of current node
                currentNode = nodesForMatrix[currentNodeIndex];
                // add it to the backtracked path
                dijkstraPath.Add(currentNode);
            }
            else if (possibleNodes.Count > 1) {
                // take first index for simplicity
                //UnityEngine.Debug.Log($"In pathfinding, there were multiple possible nodes to choose from when backtracking Dijkstra's path.");
                string possibleNodesText = "";
                for (int i = 0; i < possibleNodes.Count; i++) {
                    possibleNodesText = possibleNodesText + Convert.ToString(nodesForMatrix[possibleNodes[i]]) + ", ";
                }
                UnityEngine.Debug.Log($"\nAt node {currentNode} the options were {possibleNodesText}.");
                currentNodeIndex = possibleNodes[0];
                // get node id of current node
                currentNode = nodesForMatrix[currentNodeIndex];
                // add it to the backtracked path
                dijkstraPath.Add(currentNode);
            }
            else {
                // no possible nodes
                // check havent accidentally reached the start node
                if (currentNode == startNode) {
                    break;
                }
                else {
                    throw new ApplicationException($"Program has no options for a node past {currentNode}");
                }
            }
        }
        // now convert the list into an array and reverse it
        // save time by storing length of list
        int dijkstraPathLength = dijkstraPath.Count;
        List<int> dijkstraPathReturn = new List<int>();
        for (int i = 0; i < dijkstraPathLength; i++) {
            // take last value and place it first in list
            // - 1 - i works
            dijkstraPathReturn.Add(dijkstraPath[dijkstraPathLength-1-i]);
        }

        return dijkstraPathReturn;
    }

    /**
    This function takes the dijkstraPath, distance matrix and info matrix to sum the edges 
    from a distance matrix in order to generate a value for the total distance travelled.
    */
    public double CalculateDistance(List<int> dijkstraPath, double[,] distanceMatrix, char[,] infoMatrix) {

        // provided that it is n x n
        int numPathNodes = dijkstraPath.Count;

        //double to hold totalDistance
        double totalDistance = 0;

        int fromNode = dijkstraPath[0]; // represents the node coming from for each edge
        int toNode = dijkstraPath[0]; // represents node going to

        //now convert into node indexes
        int fromNodeIndex = Array.IndexOf(nodesForMatrix, fromNode);
        int toNodeIndex = Array.IndexOf(nodesForMatrix,toNode);
        double edgeDistance;

        for (int i = 0; i < numPathNodes-1; i++) {
            fromNodeIndex = toNodeIndex; // previous to node is now from node
            fromNode = toNode;

            toNode= dijkstraPath[i+1]; // to node is the next in array
            // find toNode index
            toNodeIndex = Array.IndexOf(nodesForMatrix, toNode);

            edgeDistance = distanceMatrix[fromNodeIndex, toNodeIndex];
            if (edgeDistance == 0) {
                throw new ApplicationException($"Program has found Edge '{fromNode}', '{toNode}' with a distance of 0");
            }
            else if (infoMatrix[fromNodeIndex, toNodeIndex] == 'L') {
                //do nothing as this shouldnt be updated
            }
            else {
                // handles every other edge by adding to distance
                totalDistance += edgeDistance;
            }
        }
        
        return Math.Round(totalDistance, 1);
    }

    /**
    This function works out the possible nodes for the start room and end room by interfacing with the database.
    A list of lists is returned where the first row is the possible nodes for the startRoom and the second row is the 
    possible nodes for the targetRoom.
    */
    public List<List<int>> EvaluatePossibleNodes() {

        // first deal with the startLocation.id

        // start list of possible nodes
        List<int> startLocationPossNodes = new List<int>();

        // if node, just do the int of the node
        if (startLocation.type == "N  ") {
            startLocationPossNodes.Add(Convert.ToInt32(startLocation.id));
        }
        else {
            // query the db for room_id, edge_id, node_id from startRoom record
            string[] startRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
            
            // figure out if connected to node or edge
            if (startLocation.type.Substring(0, 2) == "RN") {
                // connected to node or is a node
                startLocationPossNodes.Add(Convert.ToInt32(startRoomRecord[3]));
                // this is the only possible node
            }
            else if (startLocation.type.Substring(0, 2) == "RE") {
                // connected to edge
                // query db for edge info to figure out if it is directional and the nodes
                string[] startEdgeInfo = databaseHelper.GetEdgeRecord(Convert.ToInt32(startRoomRecord[2]));
                
                if (startEdgeInfo[5] == "True" && !userSettings.stepFree && userSettings.oneWaySystem) { // directional edge 
                    // so use node 2 as only node as user can at first only walk from room to this edge
                    startLocationPossNodes.Add(Convert.ToInt32(startEdgeInfo[2]));
                }
                else { // non directional
                    // so user can leave room and go to either node
                    startLocationPossNodes.Add(Convert.ToInt32(startEdgeInfo[1]));
                    startLocationPossNodes.Add(Convert.ToInt32(startEdgeInfo[2]));
                }
            }
        }
            

        // first deal with the targetLocation.id

        // target list of possible nodes
        List<int> targetLocationPossNodes = new List<int>();

        // if node, just do the int of the node
        if (targetLocation.type == "N  ") {
            targetLocationPossNodes.Add(Convert.ToInt32(targetLocation.id));
        }
        else {
            // query the db for room_id, edge_id, node_id from targetRoom record
            string[] targetRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);
            
            // figure out if connected to node or edge
            if (targetLocation.type.Substring(0, 2) == "RN") {
                // connected to node or is a node
                targetLocationPossNodes.Add(Convert.ToInt32(targetRoomRecord[3]));
                // this is the only possible node
            }
            else if (targetLocation.type.Substring(0, 2) == "RE") {
                // connected to edge
                // query db for edge info to figure out if it is directional and the nodes
                string[] targetEdgeInfo = databaseHelper.GetEdgeRecord(Convert.ToInt32(targetRoomRecord[2]));
                
                
                if (targetEdgeInfo[5] == "True" && !userSettings.stepFree && userSettings.oneWaySystem) { // directional edge 
                    // so use node 1 as user has to follow one one way system on entrance to this edge
                    targetLocationPossNodes.Add(Convert.ToInt32(targetEdgeInfo[1]));
                }
                else { // non directional
                    // so user can leave room and go to either node
                    targetLocationPossNodes.Add(Convert.ToInt32(targetEdgeInfo[1]));
                    targetLocationPossNodes.Add(Convert.ToInt32(targetEdgeInfo[2]));
                }
            }
        }

        List<List<int>> possibleNodes = new List<List<int>>

        {
            new List<int>(),
            new List<int>()
        };
        for (int i = 0; i < startLocationPossNodes.Count; i++) {
            possibleNodes[0].Add(startLocationPossNodes[i]);
        }
        for (int i = 0; i < targetLocationPossNodes.Count; i++) {
            possibleNodes[1].Add(targetLocationPossNodes[i]);
        }

        return possibleNodes;
    }

    /**
    This function figures out which nodes should be set as the start node and target node.
    It takes a list of lists representing the possible nodes for each room and sets the startNode 
    and targetNodefields in the class to the correct nodes.
    */
    public void DetermineStartAndTargetNodes(List<List<int>> possibleNodes, string startRoom, string targetRoom) {

        // store num nodes for each so decision can be made on what to do
        int startRoomNumNodes = possibleNodes[0].Count;
        int targetRoomNumNodes = possibleNodes[1].Count;

        // variables that will be returned
        int startNode = -1;
        int targetNode = -1;

        if (startRoomNumNodes == 1 && targetRoomNumNodes == 1) {
            // if just one possible node for startRoom and targetRoom, choose them
            startNode = possibleNodes[0][0];
            targetNode = possibleNodes[1][0];
        }
        else if (startRoomNumNodes == 1 && targetRoomNumNodes == 2) {
            // if one node for start and two for target
            // carry out dijkstras and estimate time from each possible final node to targetroom
            // and add this to the dijkstra time for each node
            // 1 dijkstra, two options to choose from

            // define t(arget) node id 1 and 2
            int TnodeID1 = possibleNodes[1][0];
            int TnodeID2 = possibleNodes[1][1];

            double[] dijkDists = DijkstrasAlgorithm(timeMatrix, possibleNodes[0][0]);

            // calc possible times for each node
            double possTimeTNode1 = EstimateTime(dijkDists, TnodeID1) + EstimateNodeRoomTime(TnodeID1, targetRoom);
            double possTimeTNode2 = EstimateTime(dijkDists, TnodeID2) + EstimateNodeRoomTime(TnodeID2, targetRoom);

            // only one option for start node
            startNode = possibleNodes[0][0];

            // choose target node with minimum time
            if (possTimeTNode1 <= possTimeTNode2) {
                // choose target node1
                targetNode = TnodeID1;
            }
            else {
                // choose target node2
                targetNode = TnodeID2;
            }
        }
        else if (startRoomNumNodes == 2 && targetRoomNumNodes == 1) {
            // if two nodes for start and one for target
            // carry out dijkstras from each start node and estimate time from target room to each possible start node
            // and add this to dijkstra time for each node
            // 2 dijkstra, two options to choose from

            // define s(tart) node id 1 and 2
            int SnodeID1 = possibleNodes[0][0];
            int SnodeID2 = possibleNodes[0][1];

            int Tnode = possibleNodes[1][0];

            //do dijkstra one then dijkstra two
            double[] dijkDists1 = DijkstrasAlgorithm(timeMatrix, SnodeID1);
            double[] dijkDists2 = DijkstrasAlgorithm(timeMatrix, SnodeID2);

            // calc possible times for each node
            double possTimeSNode1 = EstimateTime(dijkDists1, Tnode) + EstimateNodeRoomTime(SnodeID1, startRoom);
            double possTimeSNode2 = EstimateTime(dijkDists2, Tnode) + EstimateNodeRoomTime(SnodeID2, startRoom);

            // choose start node with minimum time
            if (possTimeSNode1 <= possTimeSNode2) {
                // choose start node1
                startNode = SnodeID1;
            }
            else {
                // choose start node2
                startNode = SnodeID2;
            }

            // only one option for target node
            targetNode = Tnode;
        }
        else if (startRoomNumNodes == 2 && targetRoomNumNodes == 2) {
            // if two nodes for start and two for target
            // carry out dijkstras from each start node and estimate time from target room to each possible start node
            // and time from each possible final node to target room
            // and add this to the dijkstra time for each node
            // 2 dijkstra, four options to choose from

            // define s(tart) node id 1 and 2
            int SnodeID1 = possibleNodes[0][0];
            int SnodeID2 = possibleNodes[0][1];
            // define t(arget) node id 1 and 2
            int TnodeID1 = possibleNodes[1][0];
            int TnodeID2 = possibleNodes[1][1];

            //do dijkstra one then dijkstra two
            double[] dijkDists1 = DijkstrasAlgorithm(timeMatrix, SnodeID1);
            double[] dijkDists2 = DijkstrasAlgorithm(timeMatrix, SnodeID2);

            // calc possible times for each node to each node
            double possTimeSNode1TNode1 = EstimateTime(dijkDists1, TnodeID1) + EstimateNodeRoomTime(SnodeID1, startRoom) + EstimateNodeRoomTime(TnodeID1, targetRoom);
            double possTimeSNode2TNode1 = EstimateTime(dijkDists2, TnodeID1) + EstimateNodeRoomTime(SnodeID2, startRoom) + EstimateNodeRoomTime(TnodeID1, targetRoom);
            double possTimeSNode1TNode2 = EstimateTime(dijkDists1, TnodeID2) + EstimateNodeRoomTime(SnodeID1, startRoom) + EstimateNodeRoomTime(TnodeID2, targetRoom);
            double possTimeSNode2TNode2 = EstimateTime(dijkDists2, TnodeID2) + EstimateNodeRoomTime(SnodeID2, startRoom) + EstimateNodeRoomTime(TnodeID2, targetRoom);

            // find the minimum time of possible times
            double[] possTimes = new double[4] {possTimeSNode1TNode1, possTimeSNode2TNode1, possTimeSNode1TNode2, possTimeSNode2TNode2};

            int minIndex = 0;
            for (int i = 1; i < possTimes.Length; i++) {
                if (possTimes[i] < possTimes[minIndex]) {
                    minIndex = i;
                }
            }

            // assign start and target node values as appropriate
            switch (minIndex) {
                case 0:
                    startNode = SnodeID1;
                    targetNode = TnodeID1;
                    break;
                case 1:
                    startNode = SnodeID2;
                    targetNode = TnodeID1;
                    break;
                case 2:
                    startNode = SnodeID1;
                    targetNode = TnodeID2;
                    break;
                case 3:
                    startNode = SnodeID2;
                    targetNode = TnodeID2;
                    break;
            }
        }
        else {
            // alert if an incorrect number of possible nodes were passed in
            // problem is likely with EvaluatePossibleNodes function
            Console.WriteLine("Invalid number of possible nodes returned.");
            Console.WriteLine($"{startRoomNumNodes} nodes for startRoom and {targetRoomNumNodes} nodes for targetRoom.");
            errorMessage.text = "An error occurred when determining the start and target nodes.";
        }
        // set the start and target nodes
        startLocation.node = startNode;
        targetLocation.node = targetNode;
    }

    /**
    This function returns the distance in metres to travel from a given node to a room along the edge the room is on.
    */
    public double EstimateNodeRoomDistance(int node_id, string room_id) {

        // query db for given node record
        string[] nodeRecord = databaseHelper.GetNodeRecord(node_id);
        // query db for room record
        string[] roomRecord = databaseHelper.GetRoomRecord(room_id);

        // from room record query for edge, so can get other node
        string[] edgeRecord = databaseHelper.GetEdgeRecord(Convert.ToInt32(roomRecord[2]));
        List<int> edgeNodes = new List<int> {Convert.ToInt32(edgeRecord[1]), Convert.ToInt32(edgeRecord[2])};

        // remove the node given to find the other node
        edgeNodes.Remove(node_id);
        int otherNode = edgeNodes[0];

        // query for other node record to get x and y coordinates
        string[] otherNodeRecord = databaseHelper.GetNodeRecord(otherNode);

        // define edge coordinates
        double nodeX = Convert.ToDouble(nodeRecord[1]);
        double nodeY = Convert.ToDouble(nodeRecord[2]);
        double otherNodeX = Convert.ToDouble(otherNodeRecord[1]);
        double otherNodeY = Convert.ToDouble(otherNodeRecord[2]);

        // define room coordinates and angle
        double roomX = Convert.ToDouble(roomRecord[4]);
        double roomY = Convert.ToDouble(roomRecord[5]);
        double angle = Convert.ToDouble(roomRecord[6]);
        
        var (xIntercept, yIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(nodeX, nodeY, otherNodeX, otherNodeY, roomX, roomY, angle);

        // calculate distance from node to other node
        double wholeEdgeDistance = Math.Sqrt(Math.Pow(nodeX-otherNodeX, 2) + Math.Pow(nodeY-otherNodeY, 2));

        // calculate distance from node to intersection
        double partialEdgeDistance = Math.Sqrt(Math.Pow(nodeX-xIntercept, 2) + Math.Pow(nodeY-yIntercept, 2));

        // find partial real life distance of edge
        double distance = Convert.ToDouble(edgeRecord[3]) * (partialEdgeDistance / wholeEdgeDistance); // from 0 to 1

        return distance;
    }

    /**
    This function calls the above one, then converts to time
    */
    public double EstimateNodeRoomTime(int node_id, string room_id) {

        double distance = EstimateNodeRoomDistance(node_id, room_id);

        //use exact same code from above to get edgeRecord[4]
        // query db for room record
        string[] roomRecord = databaseHelper.GetRoomRecord(room_id);
        // from room record query for edge, so can get other node
        string[] edgeRecord = databaseHelper.GetEdgeRecord(Convert.ToInt32(roomRecord[2]));
        // estimate time from distance
        double time = matrixBuilder.EstimateTimeFromDistance(distance, Convert.ToChar(edgeRecord[4]), matrixBuilder.matricesUseCongestionEstimation);

        return time;
    }

    /**
    This procedure goes through an array of boundary nodes and purges them from the time matrix
    if the target/start is none of them nor any nodes which are beyond the school gates.
    */
    public void PurgeEdges() {
        int[] gateNodes = {71, 56, 67, 2};
        int[] beyondBoundaryNodes = {1, 68, 57, 69, 70, 88};

        bool needToUseBoundaryNodes = false;

        // no one should be directed outside of gates even if teacher
        if (startLocation.type == "N  ") {
            needToUseBoundaryNodes = needToUseBoundaryNodes || gateNodes.Contains(Convert.ToInt32(startLocation.id));
            needToUseBoundaryNodes = needToUseBoundaryNodes || beyondBoundaryNodes.Contains(Convert.ToInt32(startLocation.id));
        }
        if (targetLocation.type == "N  ") {
            needToUseBoundaryNodes = needToUseBoundaryNodes || gateNodes.Contains(Convert.ToInt32(targetLocation.id));
            needToUseBoundaryNodes = needToUseBoundaryNodes || beyondBoundaryNodes.Contains(Convert.ToInt32(targetLocation.id));
        }
        
        // purge nodes if needed
        if (!needToUseBoundaryNodes) {
            for (int i = 0; i < gateNodes.Length; i++) {
                RemoveNodeInMatrices(gateNodes[i]);
            }
        }

        // now sixth form (currently)
        /* if (true) {//(userSettings.userType == "student") {
            for (int i = 0; i < studentNoNodes.Length; i++) {
                RemoveNodeInMatrices(studentNoNodes[i]);
            }
        } */
    }

    /**
    This procedure removes all edges attached to a node in the time matrix.
    */
    public void RemoveNodeInMatrices(int node_id) {
        int nodeIndex = Array.IndexOf(nodesForMatrix, node_id);

        // remove entire row
        for (int i = 0; i < numberOfNodes; i++) {
            timeMatrix[nodeIndex, i] = 0;
            infoMatrix[nodeIndex, i] = '0';
            distanceMatrix[nodeIndex, i] = 0;
        }

        // remove entire column
        for (int i = 0; i < numberOfNodes; i++) {
            timeMatrix[i, nodeIndex] = 0;
            infoMatrix[i, nodeIndex] = '0';
            distanceMatrix[i, nodeIndex] = 0;
        }
    }

    /**
    This procedure links together all of the necessary functions for carrying out Dijkstra's
    Algorithm for a typical user, including returning the time, eta, distance.
    */
    public void CarryOutAndInterpretPathfinding() {

        //matrixBuilder.BuildMatricesForPathfinding;
        ResetFields();

        startLocation.SetupLocation();
        targetLocation.SetupLocation();

        // time using stopwatch
        stopwatch.Start();

        // dea with "" being returned
        if (startLocation.type == "   " && targetLocation.type == "   ") {
            errorMessage.text = "Start and target locations were invalid.";
            return;
        }
        else if (startLocation.type == "   ") {  
            errorMessage.text = $"Start location {startLocation.id} was invalid.";
            return;
        }
        else if (targetLocation.type == "   ") {
            errorMessage.text = $"Target location {targetLocation.id} was invalid.";
            return;
        }
        else {} // otherwise fine so pass
        

        // first sort out possibility that start location may be the target location
        if (startLocation.id == targetLocation.id) {
            estimatedTimeInSecs = 0;
            estimatedTime = ConvertSecsToTimeFormat(estimatedTimeInSecs);
            estimatedTimeOfArrival = EstimateTimeOfArrival(estimatedTime);
            estimatedDistance = 0;
            errorMessage.text = $"The start location \"{startLocation.id}\" was the same as the target location \"{targetLocation.id}\".";
            // output a message like above to the screen
            // exit procedure very early
            return;

        }

        // figure out which node
        List<List<int>> possibleNodes = EvaluatePossibleNodes();
        DetermineStartAndTargetNodes(possibleNodes, startLocation.id, targetLocation.id);

        // determine if method 0 or 1
        int method = DetermineMethod();

        // now do finding distance/time        
        // normal dijkstra method
        if (method == 0) {

            // get values to 0 on the time matrix on the nodes that cant be used unless they are the target/start
            PurgeEdges();

            // carry out dijkstras from start node
            dijkstraDistances = DijkstrasAlgorithm(timeMatrix, startLocation.node);
            //UnityEngine.Debug.Break();

            // now do time, path, distance for nodes stuff only

            // find estimated time in secs, but not in time format as have not added possible travel 
            // time from start room to first node and last node to target room
            estimatedTimeInSecs = EstimateTime(dijkstraDistances, targetLocation.node);

            // retrace steps to find dijkstra path
            dijkstraPath = FindDijkstrasPath(timeMatrix, dijkstraDistances, startLocation.node, targetLocation.node);

            // find estimated distance for nodes
            estimatedDistance = CalculateDistance(dijkstraPath, distanceMatrix, infoMatrix);

            // for time and distance, the distinguishment is whether the rooms are attached to an edge or a node

            // init variables
            double sRoomDistance;
            double tRoomDistance;
            double sRoomTime;
            double tRoomTime;
        
            // figure out start location first  
            if (startLocation.type.Substring(0, 2) == "RN") {
                // room is node or is connected to node so no need to add anything to time or distance
                sRoomDistance = 0;
                sRoomTime = 0;

            }
            else if (startLocation.type.Substring(0, 2) == "RE") {
                // room is connected to edge so add distance from room to startnode
                string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
                string[] sEdgeRecord = databaseHelper.GetEdgeRecord(Convert.ToInt32(sRoomRecord[2]));
                sRoomDistance = EstimateNodeRoomDistance(startLocation.node, startLocation.id);
                sRoomTime = matrixBuilder.EstimateTimeFromDistance(sRoomDistance, Convert.ToChar(sEdgeRecord[4]), matrixBuilder.matricesUseCongestionEstimation);
            }
            else{
                // node
                sRoomDistance = 0;
                sRoomTime = 0;
            }

            // figure out target location now 
            if (targetLocation.type.Substring(0, 2) == "RN") {
                // room is node or is connected to node so no need to add anything to time or distance
                tRoomDistance = 0;
                tRoomTime = 0;

            }
            else if (targetLocation.type.Substring(0, 2) == "RE") {
                // room is connected to edge so add distance from room to startnode
                string[] tRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);
                string[] tEdgeRecord = databaseHelper.GetEdgeRecord(Convert.ToInt32(tRoomRecord[2]));
                tRoomDistance = EstimateNodeRoomDistance(targetLocation.node, targetLocation.id);
                tRoomTime = matrixBuilder.EstimateTimeFromDistance(tRoomDistance, Convert.ToChar(tEdgeRecord[4]), matrixBuilder.matricesUseCongestionEstimation);
            }
            else{
                // node
                tRoomDistance = 0;
                tRoomTime = 0;
            }

            // and update time and distance
            estimatedTimeInSecs = Math.Round(estimatedTimeInSecs + sRoomTime + tRoomTime, 1);
            estimatedDistance = Math.Round(estimatedDistance + sRoomDistance + tRoomDistance, 1);

            estimatedTime = ConvertSecsToTimeFormat(estimatedTimeInSecs);
            estimatedTimeOfArrival = EstimateTimeOfArrival(estimatedTime); //eta 

            // get path coordinates
            (floor0Path, floor1Path) = GetDijkstraPathCoordinates();

        }

        // along edge only method
        else if (method == 1) {
            // work out distance from each room to the node that the room is closer to than the other
            // eg: N1   R1   R2                  N2
            // even though both rooms are closer to N1, R1 would get the distance to N1 and R2 to N2
            // this is so that the distance between R1 and R2 can be found by subtracting both distances from the length of the edge

            // get edge record
            string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
            int edge = Convert.ToInt32(sRoomRecord[2]);
            string[] edgeRecord = databaseHelper.GetEdgeRecord(edge);

            // figure out which is closer to the first node in the edge record
            double distNodeSRoom = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[1]), startLocation.id);
            double distNodeTRoom = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[1]), targetLocation.id);

            // now calculate distance from rooms to nodes
            double dist1, dist2;
            if (distNodeSRoom < distNodeTRoom) {
                // Sroom and node 1, Troom and node2
                dist1 = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[1]), startLocation.id);
                dist2 = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[2]), targetLocation.id);
            }
            else {
                // Sroom and node2, Troom and node1
                dist1 = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[2]), startLocation.id);
                dist2 = EstimateNodeRoomDistance(Convert.ToInt32(edgeRecord[1]), targetLocation.id);
            }

            // and then estimated distance
            estimatedDistance = Math.Round(Convert.ToDouble(edgeRecord[3]) - (dist1 + dist2), 1);

            // find estimated time in secs
            estimatedTimeInSecs = matrixBuilder.EstimateTimeFromDistance(estimatedDistance, Convert.ToChar(edgeRecord[4]), matrixBuilder.matricesUseCongestionEstimation);

            // and then other time things
            estimatedTime = ConvertSecsToTimeFormat(estimatedTimeInSecs);
            estimatedTimeOfArrival = EstimateTimeOfArrival(estimatedTime);

            // no dijkstra path or dijkstra distances

            // get path coordinates
            (floor0Path, floor1Path) = GetDijkstraPathCoordinates();        
        }

        // should NEVER execute
        else {
            Console.WriteLine("This should never execute. If it does, there is a pathway in the nested if's above that leads to method not being assigned.");
            errorMessage.text = "An error has occured when setting the method for pathfinding.";
        }

        showResults = true;

        //end stopwatch
        stopwatch.Stop();
    }

    /**
    This function determines if method 0 or 1 should be used.
    */
    public int DetermineMethod() {
        // define variable that represents the method that will be caried out
        int method = 0; // -1 is undefined, 0 is normal from node to node, 1 is along an edge only

        // only method 1 if room that is connected to edge, on the same edge and...
        // (edge is not one way or edge is one way and distance to the startroom from the edge startnode is less than to targetroom)
        // only can check both are rooms (not nodes)
        if (startLocation.type != "N  " && targetLocation.type != "N  ") {

            // check if locations are rooms attached to edges
            if (startLocation.type.Substring(0, 2) != "RN" && targetLocation.type.Substring(0, 2) != "RN") {
                
                // connected to edge, so check if same edge
                string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
                string[] tRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);

                // check if the same edge
                if (sRoomRecord[2] == tRoomRecord[2]) {
                    // same edge so check if edge is one-way

                    if (startLocation.type == "RED") {
                        // one-way so check if target is downstream

                        // get edge record to find top of edge (most upstream part)
                        string[] edgeRecord = databaseHelper.GetEdgeRecord(Convert.ToInt32(sRoomRecord[2]));
                        int edgeTopNode = Convert.ToInt32(edgeRecord[1]); // start of one-way edge

                        // find distance from edge start node to each
                        double distSNode = EstimateNodeRoomDistance(edgeTopNode, startLocation.id);
                        double distTNode = EstimateNodeRoomDistance(edgeTopNode, targetLocation.id);

                        // check if distSNode < distTNode
                        if (distSNode < distTNode) {
                            // t node is downstream from s node, so can go along edge
                            method = 1;
                        }
                    }
                    else {
                        // same edge and undirected, so go along edge
                        method = 1;
                    }
                }
            }
        }
        return method;
    }
    
    /**
    This function finds the paths for the ground and first floor and returns them.
    This works for method 0 (normal) pathfinding.
    */
    public (List<double[]>, List<double[]>) GetDijkstraPathCoordinates() {

        // now make list of arrays that holds coordinates for floor 0 and 1 paths
        floor0Path = new List<double[]>();
        floor1Path = new List<double[]>();

        // make list of indexes that represent the node after which there is an unconnected line
        floor0BreakIndex = new List<int>();
        floor1BreakIndex = new List<int>();
        // while going through and adding coordinates if the previous node was on a different floor, 
        // the current length of that floor path gets added as an index to break up the line

        // deal with start location first

        if (startLocation.type == "N  ") {
            // is a node
            // so coordinates covered with dijstra path
        }
        else if (startLocation.type == "RN ") {
            // is a room node
            // so coordinates covered with dijstra path
        }
        else if (startLocation.type == "RNC") {
            // room is connected to node
            // add coordinates of room
            // get record to get coordinates
            string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
            
            // get start node record to check which floor it is on
            if (databaseHelper.GetRoomFloor(startLocation.id) == 0) {
                // ground floor
                // add room door
                floor0Path.Add(new double[2] {Math.Round(Convert.ToDouble(sRoomRecord[4]), 3), Math.Round(Convert.ToDouble(sRoomRecord[5]), 3)}); // room door
            }
            else if (databaseHelper.GetRoomFloor(startLocation.id) == 1) {
                // first floor
                // add room door
                floor1Path.Add(new double[2] {Math.Round(Convert.ToDouble(sRoomRecord[4]), 3), Math.Round(Convert.ToDouble(sRoomRecord[5]), 3)}); // room door
            }
        }
        else if (startLocation.type.Substring(0, 2) == "RE") {
            // attached to an edge
            // so add room coordinates and intersection coordinates
            // get record to get coordinates
            string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);

            // have to do this to get 
            if (databaseHelper.GetRoomFloor(startLocation.id) == 0) {
                // ground floor
                // add room door and edge intersection
                double[] coordinates = databaseHelper.GetRoomEdgeInfoForIntersection(startLocation.id);
                var (xIntercept, yIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5], coordinates[6]);
                floor0Path.Add(new double[2] {Math.Round(Convert.ToDouble(sRoomRecord[4]), 3), Math.Round(Convert.ToDouble(sRoomRecord[5]), 3)}); // room door
                floor0Path.Add(new double[2] {Math.Round(xIntercept, 3), Math.Round(yIntercept, 3)}); // intersection
            }
            else if (databaseHelper.GetRoomFloor(startLocation.id) == 1) {
                // first floor
                // add room door and edge intersection
                double[] coordinates = databaseHelper.GetRoomEdgeInfoForIntersection(startLocation.id);
                var (xIntercept, yIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5], coordinates[6]);
                floor1Path.Add(new double[2] {Math.Round(Convert.ToDouble(sRoomRecord[4]), 3), Math.Round(Convert.ToDouble(sRoomRecord[5]), 3)}); // room door
                floor1Path.Add(new double[2] {Math.Round(xIntercept, 3), Math.Round(yIntercept, 3)}); // intersection
            }
        }

        // add each node's coordinates in turn following dijkstra path

        for (int i = 0; i < dijkstraPath.Count; i++) {
            //check which floor
            if (databaseHelper.GetNodeFloor(dijkstraPath[i]) == 0) {
                // ground floor
                floor0Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i])); // node 
                
                // now need to check for any edge vertices from this node to the one after and get them in the correct order
                if (i != dijkstraPath.Count -1) { // so dijksta path i + 1 can be taken
                    if (databaseHelper.GetEdgeIfEdgeVerticesExist(dijkstraPath[i], dijkstraPath[i+1]) == -1) {
                        // no edge vertices exist
                        // if the node after is on the other floor, add these node coordinates
                        if (databaseHelper.GetNodeFloor(dijkstraPath[i+1]) == 1) {
                            // add the current node to other floor
                            floor1Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i]));

                            // and add the next node to this floor
                            floor0Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i+1]));
                        }
                    }
                    else {
                        // get edge id so can get edge vertices
                        int edgeID = databaseHelper.GetEdgeIfEdgeVerticesExist(dijkstraPath[i], dijkstraPath[i+1]);
                        List<double[]> edgeVertices = databaseHelper.GetEdgeVertices(edgeID, dijkstraPath[i]);
                        // add them to current floor path
                        for (int j = 0; j < edgeVertices.Count; j++) {
                            floor0Path.Add(new double[2] {edgeVertices[j][0], edgeVertices[j][1]});
                        }

                        // if the node after is on the other floor, add the edge vertices to this floor too
                        if (databaseHelper.GetNodeFloor(dijkstraPath[i+1]) == 1) {
                            // add the current node to other floor
                            floor1Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i]));

                            //add the edge vertices to the other floor too
                            for (int j = 0; j < edgeVertices.Count; j++) {
                                floor1Path.Add(new double[2] {edgeVertices[j][0], edgeVertices[j][1]});
                            }

                            // and add the next node to this floor
                            floor0Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i+1]));
                        }
                    }
                }
                
                // check if last node was on different floor, and if so add to breaks
                if (i != 0) {
                    if (databaseHelper.GetNodeFloor(dijkstraPath[i-1]) == 1) {
                        floor1BreakIndex.Add(((floor1Path.Count-1)*4)+0);
                        floor1BreakIndex.Add(((floor1Path.Count-1)*4)+1);
                        floor1BreakIndex.Add(((floor1Path.Count-1)*4)+2);
                        floor1BreakIndex.Add(((floor1Path.Count-1)*4)+3);
                    }
                }
            }
            else if (databaseHelper.GetNodeFloor(dijkstraPath[i]) == 1) {
                // first floor
                floor1Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i])); // node
                
                // now need to check for any edge vertices from this node to the one after and get them in the correct order
                if (i != dijkstraPath.Count -1) { // so dijksta path i + 1 can be taken
                    if (databaseHelper.GetEdgeIfEdgeVerticesExist(dijkstraPath[i], dijkstraPath[i+1]) == -1) {
                        // no edge vertices exist, so move on
                        // if the node after is on the other floor, add these node coordinates
                        if (databaseHelper.GetNodeFloor(dijkstraPath[i+1]) == 0) {
                            // add the current node to other floor
                            floor0Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i]));

                            // and add the next node to this floor
                            floor1Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i+1]));
                        }
                    }
                    else {
                        // get edge id so can get edge vertices
                        int edgeID = databaseHelper.GetEdgeIfEdgeVerticesExist(dijkstraPath[i], dijkstraPath[i+1]);
                        List<double[]> edgeVertices = databaseHelper.GetEdgeVertices(edgeID, dijkstraPath[i]);
                        // add them to current floor path
                        for (int j = 0; j < edgeVertices.Count; j++) {
                            floor1Path.Add(new double[2] {edgeVertices[j][0], edgeVertices[j][1]});
                        }

                        // if the node after is on the other floor, add the edge vertices to this floor too
                        if (databaseHelper.GetNodeFloor(dijkstraPath[i+1]) == 0) {
                            // add the current node to other floor
                            floor0Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i]));

                            //add the edge vertices to the other floor too
                            for (int j = 0; j < edgeVertices.Count; j++) {
                                floor0Path.Add(new double[2] {edgeVertices[j][0], edgeVertices[j][1]});
                            }

                            // and add the next node to this floor
                            floor1Path.Add(databaseHelper.GetNodeCoordinates(dijkstraPath[i+1]));
                        }
                    }
                }
                // check if last node was on different floor, and if so add to breaks
                if (i != 0) {
                    if (databaseHelper.GetNodeFloor(dijkstraPath[i-1]) == 0) {
                        floor0BreakIndex.Add(((floor0Path.Count-1)*4)+0);
                        floor0BreakIndex.Add(((floor0Path.Count-1)*4)+1);
                        floor0BreakIndex.Add(((floor0Path.Count-1)*4)+2);
                        floor0BreakIndex.Add(((floor0Path.Count-1)*4)+3);
                    }
                }
            }
        }
        // finally deal with target location

        if (targetLocation.type == "N  ") {
            // is a node
            // so coordinates covered with dijstra path
        }
        else if (targetLocation.type == "RN ") {
            // is a room node
            // so coordinates covered with dijstra path
        }
        else if (targetLocation.type == "RNC") {
            // room is connected to node
            // add coordinates of room
            // get record to get coordinates
            string[] tRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);
            
            // get target node record to check which floor it is on
            if (databaseHelper.GetRoomFloor(targetLocation.id) == 0) {
                // ground floor
                // add room door
                floor0Path.Add(new double[2] {Math.Round(Convert.ToDouble(tRoomRecord[4]), 3), Math.Round(Convert.ToDouble(tRoomRecord[5]), 3)}); // room door
            }
            else if (databaseHelper.GetRoomFloor(targetLocation.id) == 1) {
                // first floor
                // add room door
                floor1Path.Add(new double[2] {Math.Round(Convert.ToDouble(tRoomRecord[4]), 3), Math.Round(Convert.ToDouble(tRoomRecord[5]), 3)}); // room door
            }
        }
        else if (targetLocation.type.Substring(0, 2) == "RE") {
            // attached to an edge
            // so add room coordinates and intersection coordinates
            // get record to get coordinates
            string[] tRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);

            // have to do this to get 
            if (databaseHelper.GetRoomFloor(targetLocation.id) == 0) {
                // ground floor
                // add room door and edge intersection
                double[] coordinates = databaseHelper.GetRoomEdgeInfoForIntersection(targetLocation.id);
                var (xIntercept, yIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5], coordinates[6]);
                floor0Path.Add(new double[2] {Math.Round(xIntercept, 3), Math.Round(yIntercept, 3)}); // intersection
                floor0Path.Add(new double[2] {Math.Round(Convert.ToDouble(tRoomRecord[4]), 3), Math.Round(Convert.ToDouble(tRoomRecord[5]), 3)}); // room door
            }
            else if (databaseHelper.GetRoomFloor(targetLocation.id) == 1) {
                // first floor
                // add room door and edge intersection
                double[] coordinates = databaseHelper.GetRoomEdgeInfoForIntersection(targetLocation.id);
                var (xIntercept, yIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(coordinates[0], coordinates[1], coordinates[2], coordinates[3], coordinates[4], coordinates[5], coordinates[6]);
                floor1Path.Add(new double[2] {Math.Round(xIntercept, 3), Math.Round(yIntercept, 3)}); // intersection
                floor1Path.Add(new double[2] {Math.Round(Convert.ToDouble(tRoomRecord[4]), 3), Math.Round(Convert.ToDouble(tRoomRecord[5]), 3)}); // room door
            }
        }

        return (floor0Path, floor1Path);
    }

    /**
    This function finds the paths for the ground and first floor and returns them.
    This works for method 1 (edge) pathfinding.
    */
    public (List<double[]>, List<double[]>) GetEdgePathCoordinates() {

        // now make list of arrays that holds coordinates for floor 0 and 1 paths

        floor0Path = new List<double[]>();
        floor1Path = new List<double[]>();

        // define room records
        string[] sRoomRecord = databaseHelper.GetRoomRecord(startLocation.id);
        string[] tRoomRecord = databaseHelper.GetRoomRecord(targetLocation.id);

        // now find floor0Path and floor1Path
        // both are on the same floor
        if (databaseHelper.GetRoomFloor(startLocation.id) == 0) {
            // ground floor
            // add s room door, s intersection, t intersection, s room door
            floor0Path.Add(new double[2] {Convert.ToDouble(sRoomRecord[4]), Convert.ToDouble(sRoomRecord[5])}); // s room door
            double[] sCoordinates = databaseHelper.GetRoomEdgeInfoForIntersection(startLocation.id);
            var (SxIntercept, SyIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(sCoordinates[0], sCoordinates[1], sCoordinates[2], sCoordinates[3], sCoordinates[4], sCoordinates[5], sCoordinates[6]);
            floor0Path.Add(new double[2] {Math.Round(SxIntercept, 3), Math.Round(SyIntercept, 3)}); // s intersection
            double[] tCoordinates = databaseHelper.GetRoomEdgeInfoForIntersection(targetLocation.id);
            var (TxIntercept, TyIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(tCoordinates[0], tCoordinates[1], sCoordinates[2], tCoordinates[3], tCoordinates[4], tCoordinates[5], tCoordinates[6]);
            floor0Path.Add(new double[2] {Math.Round(TxIntercept, 3), Math.Round(TyIntercept, 3)}); // t intersection
            floor0Path.Add(new double[2] {Convert.ToDouble(tRoomRecord[4]), Convert.ToDouble(tRoomRecord[5])}); // t room door
        }
        else if (databaseHelper.GetRoomFloor(startLocation.id) == 1) {
            // first floor
            // add s room door, s intersection, t intersection, s room door
            floor1Path.Add(new double[2] {Convert.ToDouble(sRoomRecord[4]), Convert.ToDouble(sRoomRecord[5])}); // s room door
            double[] sCoordinates = databaseHelper.GetRoomEdgeInfoForIntersection(startLocation.id);
            var (SxIntercept, SyIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(sCoordinates[0], sCoordinates[1], sCoordinates[2], sCoordinates[3], sCoordinates[4], sCoordinates[5], sCoordinates[6]);
            floor1Path.Add(new double[2] {Math.Round(SxIntercept, 3), Math.Round(SyIntercept, 3)}); // s intersection
            double[] tCoordinates = databaseHelper.GetRoomEdgeInfoForIntersection(targetLocation.id);
            var (TxIntercept, TyIntercept) = databaseHelper.CalcIntersectionOfEdgeAndRoomConnector(tCoordinates[0], tCoordinates[1], sCoordinates[2], tCoordinates[3], tCoordinates[4], tCoordinates[5], tCoordinates[6]);
            floor1Path.Add(new double[2] {Math.Round(TxIntercept, 3), Math.Round(TyIntercept, 3)}); // t intersection
            floor1Path.Add(new double[2] {Convert.ToDouble(tRoomRecord[4]), Convert.ToDouble(tRoomRecord[5])}); // t room door
        }
        
        return (floor0Path, floor1Path);
    }

    /**
    This function is mainly used in testing to output the results of CarryOutAndInterpretPathfinding()
    */
    public void ShowPathfindingResults() {

        Console.WriteLine($"Start Location: {startLocation.id}");
        Console.WriteLine($"Target Location: {targetLocation.id}\n");

        Console.WriteLine($"Start Node: {startLocation.node}");
        Console.WriteLine($"Target Node: {targetLocation.node}\n");

        Console.WriteLine($"Estimated Time: {estimatedTime}");
        Console.WriteLine($"ETA: {estimatedTimeOfArrival}\n");

        Console.WriteLine($"Estimated Distance: {estimatedDistance}\n");

        Console.Write("Dijkstra Path: ");
        for (int i = 0; i < dijkstraPath.Count; i++) {
            Console.Write(dijkstraPath[i]);
            if (i != dijkstraPath.Count - 1) {
                Console.Write(", ");
            }
            else {
                Console.Write("\n");
            }
        }
        Console.Write("\n");

        Console.WriteLine("Floor 0 Coordinates:");
        for (int i = 0; i < floor0Path.Count; i++) {
            for (int j = 0; j < floor0Path[i].Length; j++) {
                Console.Write(floor0Path[i][j]);
                if (j != floor0Path[i].Length - 1) {
                    Console.Write(", ");
                }
                else {
                    Console.Write("\n");
                }
            }
        }
        Console.Write("\n");

        Console.WriteLine("Floor 1 Coordinates:");
        for (int i = 0; i < floor1Path.Count; i++) {
            for (int j = 0; j < floor1Path[i].Length; j++) {
                Console.Write(floor1Path[i][j]);
                if (j != floor1Path[i].Length - 1) {
                    Console.Write(", ");
                }
                else {
                    Console.Write("\n");
                }
            }
        }
        Console.Write("\n");

        Console.WriteLine($"Elapsed Dijkstra Time: {stopwatch.ElapsedMilliseconds} ms\n");
    }

    /**
    This function changes the values of the text output boxes to the results of the pathfinding.
    */
    public void OutputTextResults() {
        // make sure correct for singlular minute and second eg 1 minute 1 second
        if (estimatedTime.Minutes == 1) {
            timeOutput.text = $"{estimatedTime.Minutes} minute ";
        }
        else {
            timeOutput.text = $"{estimatedTime.Minutes} minutes ";
        }
        if (estimatedTime.Hours == 1) {
            timeOutput.text += $"{estimatedTime.Seconds} second";
        }
        else {
            timeOutput.text += $"{estimatedTime.Seconds} seconds";
        }
        ETAOutput.text = $"{estimatedTimeOfArrival.Hours:D2}:{estimatedTimeOfArrival.Minutes:D2} arrival";
        distanceOutput.text = $"{estimatedDistance} metres";
    }

    /**
    This function resets all of the output text boxes to empty strings.
    */
    public void ResetOutput() {
        timeOutput.text = "";
        ETAOutput.text = "";
        distanceOutput.text = "";
    }
    
    /**
    This function returns true if any of the text results are not empty.
    */
    public bool TextResultsVisible() {
        if (timeOutput.text == "" && ETAOutput.text == "" && distanceOutput.text == "") {
            return false;
        }
        else {
            return true;
        }
    }

    /**
    This function sets showResults to false.
    */
    public void HidePathfindingResults() {
        showResults = false;
    }

    /**
    This procedure validates the start location's input and adjusts it as necessary.
    */
    public void ValidateStartLocation() {
        // check if empty and if so set error message to empty
        if (startLocationInput.text == "") {
            startLocation.id = "";
            startLocation.userText = "";
            errorMessage.text = "";
        }
        // check if it is a room id that exactly matches a database entry.
        else if (databaseHelper.RoomIDExists(startLocationInput.text)) {
            // startLocationInput exists as room id so capitalise and set to both user and program start location
            startLocation.id = startLocationInput.text.ToUpper();
            if (databaseHelper.GetRoomNameFromID(startLocation.id) != "") {
                startLocation.userText = databaseHelper.GetRoomNameFromID(startLocation.id);
            }
            else {
                startLocation.userText = startLocationInput.text.ToUpper();
            }
            startLocationInput.text = startLocation.userText;
        }
        // check if it is a room name that exactly matches
        else if (databaseHelper.RoomNameExists(startLocationInput.text)) {
            // startLocationInput exists as room_name so get room id and set user and program start location
            startLocation.id = databaseHelper.GetRoomIDFromName(startLocationInput.text);
            startLocation.userText = databaseHelper.GetRoomNameFromName(startLocationInput.text);
            startLocationInput.text = startLocation.userText;
        }
        // check if it appears in part of a room name but only in one, not multiple
        else if (databaseHelper.GetRoomNameSubstringCount(startLocationInput.text) == 1) {
            // startLocationInput exists as a substring of a single room_name so get room id and set user and program start location
            startLocation.id = databaseHelper.GetRoomIDFromSubstringName(startLocationInput.text);
            startLocation.userText = databaseHelper.GetRoomNameFromSubstringName(startLocationInput.text);
            startLocationInput.text = startLocation.userText;
        }
        // check if it appears as an exact node_id
        else if (databaseHelper.NodeIDExists(startLocationInput.text) ) {// && double.TryParse(startLocationInput.text, out _)) {
            // startLocationInput exists as a node id so set startLocation.id to both user and program start location
            startLocation.id = startLocationInput.text;
            if (databaseHelper.GetNodeNameFromID(startLocation.id) != "") {
                startLocation.userText = databaseHelper.GetNodeNameFromID(startLocation.id);
            }
            else {
                startLocation.userText = "Node " + startLocationInput.text;
            }
            startLocationInput.text = startLocation.userText;
        }
        // check if it is a node name that exactly matches
        else if (databaseHelper.NodeNameExists(startLocationInput.text)) {
            // startLocationInput exists as node name so get node id and set user and program start location
            startLocation.id = Convert.ToString(databaseHelper.GetNodeIDFromName(startLocationInput.text));
            startLocation.userText = databaseHelper.GetNodeNameFromName(startLocationInput.text);
            startLocationInput.text = startLocation.userText;
        }
        // check if it appears in part of a node name but only in one, not multiple
        else if (databaseHelper.GetNodeNameSubstringCount(startLocationInput.text) == 1) {
            // startLocationInput exists as a substring of a single node name so get node id and set user and program start location
            startLocation.id = Convert.ToString(databaseHelper.GetNodeIDFromSubstringName(startLocationInput.text));
            startLocation.userText = databaseHelper.GetNodeNameFromSubstringName(startLocationInput.text);
            startLocationInput.text = startLocation.userText;
        }
        // error message if multiple substring records found
        else if (databaseHelper.GetRoomNameSubstringCount(startLocationInput.text) > 1 || databaseHelper.GetNodeNameSubstringCount(startLocationInput.text) > 1) {
            // startLocationInput exists as a substring of multiple room_names so set startLocation.id to ""
            startLocation.id = "";
            startLocation.userText = startLocationInput.text;
            if (errorMessage.text != "Invalid characters detected in the input.") {
                errorMessage.text = "Start location was too vague. Please be more specific.";
            }
        }
        // error message if no substring records found
        else {
            // startLocationInput does not exist as a substring of any node names so set startLocation.id to ""
            startLocation.id = "";
            startLocation.userText = startLocationInput.text;
            if (errorMessage.text != "Invalid characters detected in the input.") {
                errorMessage.text = "Start location was not found.";
            }
        }

        if (startLocation.id != "") {
            errorMessage.text = "";
        }

        startLocation.SetupLocation();
        showResults = false;
    }

    /**
    This procedure validates the target location's input and adjusts it as necessary.
    */
    public void ValidateTargetLocation() {
        // check if empty and if so set error message to empty
        if (targetLocationInput.text == "") {
            targetLocation.id = "";
            targetLocation.userText = "";
            errorMessage.text = "";
        }
        // check if it is a room id that exactly matches a database entry.
        else if (databaseHelper.RoomIDExists(targetLocationInput.text)) {
            // targetLocationInput exists as room id so capitalise and set to both user and program target location
            targetLocation.id = targetLocationInput.text.ToUpper();
            if (databaseHelper.GetRoomNameFromID(targetLocation.id) != "") {
                targetLocation.userText = databaseHelper.GetRoomNameFromID(targetLocation.id);
            }
            else {
                targetLocation.userText = targetLocationInput.text.ToUpper();
            }
            targetLocationInput.text = targetLocation.userText;
        }
        // check if it is a room name that exactly matches
        else if (databaseHelper.RoomNameExists(targetLocationInput.text)) {
            // targetLocationInput exists as room_name so get room id and set user and program target location
            targetLocation.id = databaseHelper.GetRoomIDFromName(targetLocationInput.text);
            targetLocation.userText = databaseHelper.GetRoomNameFromName(targetLocationInput.text);
            targetLocationInput.text = targetLocation.userText;
        }
        // check if it appears in part of a room name but only in one, not multiple
        else if (databaseHelper.GetRoomNameSubstringCount(targetLocationInput.text) == 1) {
            // targetLocationInput exists as a substring of a single room_name so get room id and set user and program target location
            targetLocation.id = databaseHelper.GetRoomIDFromSubstringName(targetLocationInput.text);
            targetLocation.userText = databaseHelper.GetRoomNameFromSubstringName(targetLocationInput.text);
            targetLocationInput.text = targetLocation.userText;
        }
        // check if it appears as an exact node_id
        else if (databaseHelper.NodeIDExists(targetLocationInput.text)) {
            // targetLocationInput exists as a node id so set targetLocation.id to both user and program target location
            targetLocation.id = targetLocationInput.text;
            if (databaseHelper.GetNodeNameFromID(targetLocation.id) != "") {
                targetLocation.userText = databaseHelper.GetNodeNameFromID(targetLocation.id);
            }
            else {
                targetLocation.userText = "Node " + targetLocationInput.text;
            }
            targetLocationInput.text = targetLocation.userText;
        }
        // check if it is a node name that exactly matches
        else if (databaseHelper.NodeNameExists(targetLocationInput.text)) {
            // targetLocationInput exists as node name so get node id and set user and program target location
            targetLocation.id = Convert.ToString(databaseHelper.GetNodeIDFromName(targetLocationInput.text));
            targetLocation.userText = databaseHelper.GetNodeNameFromName(targetLocationInput.text);
            targetLocationInput.text = targetLocation.userText;
        }
        // check if it appears in part of a node name but only in one, not multiple
        else if (databaseHelper.GetNodeNameSubstringCount(targetLocationInput.text) == 1) {
            // targetLocationInput exists as a substring of a single node name so get node id and set user and program target location
            targetLocation.id = Convert.ToString(databaseHelper.GetNodeIDFromSubstringName(targetLocationInput.text));
            targetLocation.userText = databaseHelper.GetNodeNameFromSubstringName(targetLocationInput.text);
            targetLocationInput.text = targetLocation.userText;
        }
        // error message if multiple substring records found
        else if (databaseHelper.GetRoomNameSubstringCount(targetLocationInput.text) > 1 || databaseHelper.GetNodeNameSubstringCount(targetLocationInput.text) > 1) {
            // targetLocationInput exists as a substring of multiple room_names so set targetLocation.id to ""
            targetLocation.id = "";
            targetLocation.userText = targetLocationInput.text;
            if (errorMessage.text != "Invalid characters detected in the input.") {
                errorMessage.text = "Target location was too vague. Please be more specific.";
            }
        }
        // error message if no substring records found
        else {
            // targetLocationInput does not exist as a substring of any node names so set targetLocation.id to ""
            targetLocation.id = "";
            targetLocation.userText = targetLocationInput.text;
            if (errorMessage.text != "Invalid characters detected in the input.") {
                errorMessage.text = "Target location was not found.";
            }
        }

        if (targetLocation.id != "") {
            errorMessage.text = "";
        }

        targetLocation.SetupLocation();
        showResults = false;
    }

    /**
    This function swaps the start and target locations.
    */
    public void SwapStartAndTargetLocations() {
        // temporary storage for swapping
        string tempId = startLocation.id;
        int tempNode = startLocation.node;
        string tempType = startLocation.type;
        bool tempFloor = startLocation.floor;
        Vector2 tempCoordinates = startLocation.coordinates;
        string tempUserText = startLocation.userText;

        // swap with the other location
        startLocation.id = targetLocation.id;
        startLocation.node = targetLocation.node;
        startLocation.type = targetLocation.type;
        startLocation.floor = targetLocation.floor;
        startLocation.coordinates = targetLocation.coordinates;
        startLocation.userText = targetLocation.userText;

        // set the other location's fields to this instance's original values
        targetLocation.id = tempId;
        targetLocation.node = tempNode;
        targetLocation.type = tempType;
        targetLocation.floor = tempFloor;
        targetLocation.coordinates = tempCoordinates;
        targetLocation.userText = tempUserText;
        
        // update shown text
        startLocationInput.text = startLocation.userText;
        targetLocationInput.text = targetLocation.userText;

        // and stop displaying results
        showResults = false;
    }

    /**
    This function sets start dijkstra to true.
    */
    public void StartDijkstra() {
        startDijkstra = true;
    }
}
