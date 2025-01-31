using System;
using System.Collections.Generic;
using UnityEngine;
using MySqlConnector;
using System.Text.RegularExpressions;


public class DatabaseHelperScript : MonoBehaviour
{
    // fields
    [SerializeField] private UserSettingsScript userSettings;
    [SerializeField] private DijkstraPathfinderScript dijkstraPathfinder;
    public MySqlConnection connection;
    private string server;
    private string database;
    private string uid;
    private string password;

    // constructors
    public DatabaseHelperScript(){

        // configure connection settings
        server = "localhost";
        database = "stanavappdb";
        uid = "app_user";
        password = "dfx0qx!m~?08.Ok9?CHq";
        string connectionString;
        connectionString = "SERVER=" + server + ";" + "DATABASE=" + database + ";" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";

        connection = new MySqlConnection(connectionString);
    }

    /** 
    This function attempts to open connection to the database.
    It returns true if it is succesful and false if not. 
    */
    private bool OpenConnection() {
        
        try {
            connection.Open();
            return true;

        }
        catch (MySqlException ex){
            // two most common errors are
            // 0: cannot connect to server
            // 1045: invalid username and or password
            switch (ex.Number)
            {
                case 0:
                    Debug.Log("Cannot connect to sever.");
                    break;
                case 1045:
                    Debug.Log("Invalid username/password.");
                    break;
                default:
                    Debug.Log("An error has occured: " + ex.Message);
                    break;
            }
            return false;
        }

    }

    /**
    This function attempts to close the database connection.
    It returns true if it is succesful and false if not.
    */
    private bool CloseConnection() {

        try {
            connection.Close();
            return true;
        }
        catch (MySqlException ex){
            Console.WriteLine(ex.Message);
            return false;
        }
    }

    /**
    This function takes a select query and returns a list of field names and a list of values.
    It works for any select statement.
    */
    private (List<string>, List<List<object>>) ExecuteSelect(string query) {

        // to hold results
        List<List<object>> columnedValues = new List<List<object>>();
        // to hold field names
        List<string> fieldNames = new List<string>();

        if (OpenConnection() == true) {
            try
            {   
                // using is more efficient and disposes of resources when done
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        // read field names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            fieldNames.Add(reader.GetName(i));
                        }

                        // read data
                        while (reader.Read())
                        {
                            var rowValues = new List<object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowValues.Add(reader[i]);
                            }
                            columnedValues.Add(rowValues);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error executing query: " + ex.Message);
            }
            finally
            {
                CloseConnection();
            }
        }

        return (fieldNames, columnedValues);
    }

    

    /**
    This function takes a select query and returns a list of field names and a list of values.
    It works with a paramater to allow for paramaterised queries.
    */
    public (List<string>, List<List<object>>) ExecuteParametrisedSelect(string query, Dictionary<string, object> parameters)
    {
        // Validate that query only contains letters, numbers, spaces, underscores, and safe SQL characters
        if (!Regex.IsMatch(query, @"^[a-zA-Z0-9 ]+$"))
        {
            dijkstraPathfinder.errorMessage.text = "Invalid characters detected in the input.";
            throw new ArgumentException("Invalid characters detected in the query.");
        }

        List<List<object>> columnedValues = new List<List<object>>();
        List<string> fieldNames = new List<string>();

        if (OpenConnection())
        {
            try
            {   
                // using is more efficient and disposes of resources when done
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // add parameters safely
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        // read field names
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            fieldNames.Add(reader.GetName(i));
                        }

                        // read data
                        while (reader.Read())
                        {
                            var rowValues = new List<object>();
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                rowValues.Add(reader[i]);
                            }
                            columnedValues.Add(rowValues);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error executing query: " + ex.Message);
            }
            finally
            {
                // closes connection regardless
                CloseConnection();
            }
        }

        return (fieldNames, columnedValues);
    }

    /**
    This function carries out the scalar select.
    This is useful for Count(*) or average of values.
    It takes a query and returns -1 if there is an error.
    */
    public double ExecuteScalarSelect(string query)
    {   
        double scalarValue = -1;

        if (OpenConnection())
        {
            try
            {   
                // using is more efficient and disposes of resources when done
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        // execute scalar will only return one value
                        object result = command.ExecuteScalar();
                        if (result != null) {
                            scalarValue = double.Parse(result+"");
                        }
                    }
                }  
            }
            catch (Exception ex)
            {
                Debug.Log("Error executing query: " + ex.Message);
            }
            finally
            {
                // closes connection regardless
                CloseConnection();
            }
        }

        return scalarValue;
    }

    /**
    This function carries out a scalar select and returns a double which is -1 if an error occured.
    It works with a paramater to allow for paramaterised queries.
    */
    public double ExecuteParametrisedScalarSelect(string query, Dictionary<string, object> parameters)
    {   
        double scalarValue = -1;

        // Validate that query only contains letters, numbers, spaces, underscores, and safe SQL characters
        if (!Regex.IsMatch(query, @"^[a-zA-Z0-9 ]+$"))
        {
            dijkstraPathfinder.errorMessage.text = "Invalid characters detected in the input.";
            return scalarValue;
        }

        if (OpenConnection())
        {
            try
            {   
                // using is more efficient and disposes of resources when done
                using (MySqlCommand command = new MySqlCommand(query, connection))
                {
                    // add parameters safely
                    foreach (var param in parameters)
                    {
                        command.Parameters.AddWithValue(param.Key, param.Value);
                    }

                    // execute scalar will only return one value
                    object result = command.ExecuteScalar();
                    if (result != null) {
                        scalarValue = double.Parse(result+"");
                    }
                }
            }  
            catch (Exception ex)
            {
                Console.WriteLine("Error executing query: " + ex.Message);
            }
            finally
            {
                // closes connection regardless
                CloseConnection();
            }
        }

        return scalarValue;
    }

    /**
    This procedure outputs the result of a select query neatly.
    */
    public void ShowSelectResult((List<string>, List<List<object>> ) values) {

        var (fieldNames, dataValues) = values;

        // output field names
        Console.WriteLine(string.Join(", ", fieldNames));

        // output values
        foreach (var row in dataValues)
        {
            Console.WriteLine(string.Join("\t", row)); // tab-separated for better readability
        }
    }

    /**
    This function uses SQL to get the number of nodes in tblnode.
    */
    public int GetNumberOfNodes() {
        
        return Convert.ToInt32(ExecuteScalarSelect("SELECT Count(node_id) FROM tblnode"));
    }

    /**
    This function uses SQL to get all the nodes in the database so that their id and index can be used appropriately.
    */
    public int[] GetNodeIDsInDatabase() {

        var (nodeFields, nodeValues)  = ExecuteSelect("SELECT node_id FROM tblnode");

        int[] nodeArray = new int[nodeValues.Count];

        // now loop through nodeValues and put value in nodeArray
        for (int i = 0; i < nodeValues.Count; i++) {
            
            nodeArray[i] = Convert.ToInt32(nodeValues[i][0]);
        }

        return nodeArray;
    }

    /**
    This function uses SQL to return the node_1_id, node_2_id, weight, edge_type_id from the database 
    so the distance and info matrices can be built.
    */
    public string[,] GetEdgesToBuildMatrices() {

        // query db for all edges
        var (edgeFields, edgeValues) = ExecuteSelect("SELECT node_1_id, node_2_id, weight, edge_type_id FROM tbledge");

        //get number of times need to loop from the edgeValues
        int numberOfEdges = edgeValues.Count;

        string[,] edgeInfo = new string[numberOfEdges,4];

        for (int i = 0; i < numberOfEdges; i++) {
            for (int j = 0; j < 4; j++) {
                edgeInfo[i,j] = Convert.ToString(edgeValues[i][j]) + "";
            }
        }

        return edgeInfo;
    }

    /**
    This function uses SQL to get the same information as above to build the one-way matrices by removing
    the data found with this function as appropriate.
    The only fields needed are the node_id's as this is what is needed to remove from the matrix.
    */
    public string[,] GetOneWayEdgesToBuildMatrix() {
        // query db for all edges
        var (edgeFields, edgeValues) = ExecuteSelect("SELECT node_1_id, node_2_id FROM tbledge WHERE one_way = true");

        //get number of times need to loop from the edgeValues
        int numberOfEdges = edgeValues.Count;

        string[,] edgeInfo = new string[numberOfEdges,2];

        for (int i = 0; i < numberOfEdges; i++) {
            for (int j = 0; j < 2; j++) {
                edgeInfo[i,j] = Convert.ToString(edgeValues[i][j]) + "";
            }
        }

        return edgeInfo;
    }

    /**
    This function uses SQL to source the boolean value controlling whether the time of day
    can be used in calculations for estimations of time.
    */
    public bool GetUseCongestionEstimation() {
        double boolValue = ExecuteScalarSelect("SELECT setting_value FROM tblsetting WHERE setting_name = \"useTimeOfDayForCalculationDB\"");
        if (boolValue == 0 || boolValue == 1) {
            return Convert.ToBoolean(boolValue);
        }
        else {
            Console.WriteLine("Expected a Boolean, but received " + Convert.ToString(boolValue));
            return false; // play it safe by returning false
        }
    }

    /**
    This function uses SQL to source the velocity for the specified edge type and a boolean
    representing the use (1) or not (0) of a slower velocity.
    */
    public double GetVelocityValue(char edgeType, bool useSlowVal) {
        
        double velocityValue;

        if (!useSlowVal) { // normal velocity
            velocityValue = ExecuteScalarSelect("SELECT normal_velocity FROM tbledgetype WHERE edge_type_id = \"" + Convert.ToString(edgeType) + "\"");
        }
        else { // slow/congested velocity
            velocityValue = ExecuteScalarSelect("SELECT congestion_velocity FROM tbledgetype WHERE edge_type_id = \"" + Convert.ToString(edgeType) + "\"");
        }

        return velocityValue;
    }

    /**
    This function uses SQL to get all congestion times and return a list of time spans
    representing this information.
    */
    public List<TimeSpan> GetCongestionTimes() {

        var (timeFields, timeValues) = ExecuteSelect("SELECT * FROM tblsetting WHERE setting_name like \"congestionTime%\"");

        //get number of times
        int numberOfTimes = timeValues.Count;

        // just in case the order has been changed, get indexes manually
        int timeFieldIndex = timeFields.IndexOf("setting_value");

        List<TimeSpan> congestionTimes = new List<TimeSpan>();

        for (int i = 0; i < numberOfTimes; i++) {

            // get time from time values
            // "" + so that there isnt a warning about possibly null value being set to non nullable type
            string time = "" + Convert.ToString(timeValues[i][timeFieldIndex]); // returns a time

            // now need to get out of weird format and convert into a timespan that gets appended to list of time spans
            // format is 00,00,00
            int hours = Convert.ToInt32(time.Substring(0, 2)); // start at character 0 and take two characters
            int minutes = Convert.ToInt32(time.Substring(3, 2)); // start at character 3 and take two characters
            int seconds = Convert.ToInt32(time.Substring(6, 2)); // start at character 6 and take two characters

            congestionTimes.Add(new TimeSpan(hours, minutes, seconds));            

        }

        return congestionTimes;
    }

    /**
    This function uses SQL to get the margin time (amount of time after a specified congestion time for
    which the corridor is still busy.
    */
    public TimeSpan GetCongestionDuration() {

        // get margin time from sql query
        var (timeFields, timeValues) = ExecuteSelect("SELECT setting_value FROM tblsetting WHERE setting_name = \"congestionDuration\"");

        string congestionDuration = "" + Convert.ToString(timeValues[0][0]);

        // now need to get out of weird format and convert into a timespan that gets appended to list of time spans
        // format is 00,00,00
        int hours = Convert.ToInt32(congestionDuration.Substring(0, 2)); // start at character 0 and take two characters
        int minutes = Convert.ToInt32(congestionDuration.Substring(3, 2)); // start at character 3 and take two characters
        int seconds = Convert.ToInt32(congestionDuration.Substring(6, 2)); // start at character 6 and take two characters

        // place nicely here
        TimeSpan congestionDurationTS = new TimeSpan(hours, minutes, seconds);

        return congestionDurationTS;
    }

    /**
    This function uses SQL to get all the edges that it should draw on the map.
    */
    public double[,] GetMapEdges(bool floor) {

        // query db
        int floorNum = Convert.ToInt32(floor);
        var (mapEdgeFields, mapEdgeValues) = ExecuteSelect("SELECT point_1_x, point_1_y, point_2_x, point_2_y FROM tblMapEdge WHERE floor_"+floorNum+" = 1");

        double[,] mapEdges = new double[mapEdgeValues.Count,4];

        for (int i = 0; i < mapEdgeValues.Count; i++) {
            for (int j = 0; j < mapEdgeValues[i].Count; j++) {
                mapEdges[i, j] = Convert.ToDouble(mapEdgeValues[i][j]);
            }
        }

        return mapEdges;
    }

    /**
    This function uses SQL to get the coordinates of all edges: the simple and hard ones
    */
    public double[,] GetEdgeCoordinates(bool floor) {

        int floorNum;
        if (!floor) { // floor = false so 0
            floorNum = 0;
        }
        else { // floor = true so 1
            floorNum = 1;
        }

        // simple is where there are not vertices involved
        // get all coordinates for non vertex edges
        var (simpleEdgeFields, simpleEdgeValues) = ExecuteSelect("select t1.x_coordinate, t1.y_coordinate,  t2.x_coordinate, t2.y_coordinate from tblEdge inner join tblNode t1 on tblEdge.node_1_id = t1.node_id inner join tblNode t2 on tblEdge.node_2_id = t2.node_id left join tblEdgeVertex ON tblEdge.edge_id = tblEdgeVertex.edge_id where t1.x_coordinate is not null and t1.y_coordinate is not null and t2.x_coordinate is not null and t2.y_coordinate is not null and tblEdgeVertex.edge_id is null and (t1.floor = " + floorNum + " or t2.floor = " + floorNum + ") order by tblEdge.edge_id asc");
        int numSimpleEdges = simpleEdgeValues.Count;

        // hard is where there are vertices involved
        // get all distinct vertexed edge id's and node coordinates
        var (hardUniqueEdgeFields, hardUniqueEdgeValues) = ExecuteSelect("select distinct tblEdge.edge_id, t1.x_coordinate, t1.y_coordinate,  t2.x_coordinate, t2.y_coordinate from tblEdge inner join tblEdgeVertex on tblEdge.edge_id = tblEdgeVertex.edge_id inner join tblNode t1 on tblEdge.node_1_id = t1.node_id inner join tblNode t2 on tblEdge.node_2_id = t2.node_id where (t1.floor = " + floorNum + " or t2.floor = " + floorNum + ") and t1.x_coordinate is not null and t1.y_coordinate is not null and t2.x_coordinate is not null and t2.y_coordinate is not null order by edge_id asc;");
        // get all edge vertexes ordered first by edge id to match with above, then by order
        var (hardCoordinateEdgeFields, hardCoordinateEdgeValues) = ExecuteSelect("select tblEdgeVertex.edge_id, tblEdgeVertex.x_coordinate, tblEdgeVertex.y_coordinate from tbledgeVertex inner join tblEdge on tblEdge.edge_id = tblEdgeVertex.edge_id inner join tblNode t1 on tblEdge.node_1_id = t1.node_id inner join tblNode t2 on tblEdge.node_2_id = t2.node_id where (t1.floor = " + floorNum + " or t2.floor = " + floorNum + ") and t1.x_coordinate is not null and t1.y_coordinate is not null and t2.x_coordinate is not null and t2.y_coordinate is not null order by edge_id asc, vertex_order asc");

        // define array to store coordinates
        // number of lines is equal to the number of simple edges (naturally) plus the number of hard edge id's plus the number of hard edge vertices
        // this is because a single hard edge (with no vertices (despite the contradiction of it being hard)) would have one line
        // one vertex for this edge would make two lines, two vertices, three lines total
        // therefore for hard edges, total lines is hard distinct edges + hard edge vertices
        double[,] edges = new double[numSimpleEdges + hardUniqueEdgeValues.Count + hardCoordinateEdgeValues.Count,4];

        for (int i = 0; i < numSimpleEdges; i++) {
            for (int j = 0; j < 4; j++) {
                edges[i, j] = Convert.ToDouble(simpleEdgeValues[i][j]);
            }
        }

        // add a row to hardCoordinateEdgeValues so that we can always get the vertexIndex+1, otherwise runtime error
        hardCoordinateEdgeValues.Add(new List<object> {-1, 0, 0});

        // now go through hard edges
        int currentEdgeIndex = 0; // the index from distinct edge id's
        int vertexIndex = 0; // increments for each new row in coordinate edgevalues
        int writeRow = numSimpleEdges; // which row in edges currently writing to
        while (currentEdgeIndex < hardUniqueEdgeValues.Count) { // stop once exhausted all unique edge id's
            int currentEdgeID = (int)hardUniqueEdgeValues[currentEdgeIndex][0];
            // loops through the section where the egde id's are all the same

            //first add line for from node 1 to first vertex
            edges[writeRow, 0] = Convert.ToDouble(hardUniqueEdgeValues[currentEdgeIndex][1]);
            edges[writeRow, 1] = Convert.ToDouble(hardUniqueEdgeValues[currentEdgeIndex][2]);
            edges[writeRow, 2] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][1]);
            edges[writeRow, 3] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][2]);

            writeRow++;

            while ((int)hardCoordinateEdgeValues[vertexIndex+1][0] == currentEdgeID) {
                // connect vertex index to vertex index + 1
                edges[writeRow, 0] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][1]);
                edges[writeRow, 1] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][2]);
                edges[writeRow, 2] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex + 1][1]);
                edges[writeRow, 3] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex + 1][2]);

                vertexIndex++;
                writeRow++;
            }
            // now add line for from last vertex to node 2
            edges[writeRow, 0] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][1]);
            edges[writeRow, 1] = Convert.ToDouble(hardCoordinateEdgeValues[vertexIndex][2]);
            edges[writeRow, 2] = Convert.ToDouble(hardUniqueEdgeValues[currentEdgeIndex][3]);
            edges[writeRow, 3] = Convert.ToDouble(hardUniqueEdgeValues[currentEdgeIndex][4]);

            vertexIndex++;
            writeRow++;
            currentEdgeIndex++;
        }

        return edges;
    }

    /**
    This function uses SQL to get all room connector lines.
    */
    public double[,] GetRoomConnectionCoordinates(bool floor) {

        int floorNum;
        
        if (!floor) { // floor = false so 0
            floorNum = 0;
        }
        else { // floor = true so 1
            floorNum = 1;
        }

        // query for coordinates and angle for connecting room to edge
        var (roomEdgeFields, roomEdgeValues) = ExecuteSelect("select t1.x_coordinate, t1.y_coordinate,  t2.x_coordinate, t2.y_coordinate, tR.x_coordinate, tR.y_coordinate, tR.door_angle from tblRoom tR inner join tblEdge on tR.edge_id = tblEdge.edge_id inner join tblNode t1 on tblEdge.node_1_id = t1.node_id inner join tblNode t2 on tblEdge.node_2_id = t2.node_id where tR.x_coordinate is not null and tR.y_coordinate is not null and tR.door_angle is not null and (t1.floor = " + floorNum + " or t2.floor = " + floorNum + ") order by tR.edge_id asc");

        // query for coordinates for connecting room to node
        var (roomNodeFields, roomNodeValues) = ExecuteSelect("select tN.x_coordinate, tN.y_coordinate, tR.x_coordinate, tR.y_coordinate from tblRoom tR inner join tblnode tN on tN.node_id = tR.node_id where tR.x_coordinate is not null and tR.y_coordinate is not null and tN.x_coordinate is not null and tN.y_coordinate is not null and (tN.floor = " + floorNum +")");

        // get number of each type
        int numEdgeConnects = roomEdgeValues.Count;
        int numNodeConnects = roomNodeValues.Count;

        // a single edge or node connect means a single line is needed
        double[,] connectors = new double[numEdgeConnects + numNodeConnects,4];

        //iterate through room edge values, finding the intersection points and filling them in
        for (int i = 0; i < numEdgeConnects; i++) {

            var (xIntercept, yIntercept) = CalcIntersectionOfEdgeAndRoomConnector(Convert.ToDouble(roomEdgeValues[i][0]), Convert.ToDouble(roomEdgeValues[i][1]), Convert.ToDouble(roomEdgeValues[i][2]), Convert.ToDouble(roomEdgeValues[i][3]), Convert.ToDouble(roomEdgeValues[i][4]), Convert.ToDouble(roomEdgeValues[i][5]), Convert.ToDouble(roomEdgeValues[i][6]));

            //update connectors array
            connectors[i, 0] = Convert.ToDouble(roomEdgeValues[i][4]);
            connectors[i, 1] = Convert.ToDouble(roomEdgeValues[i][5]);
            connectors[i, 2] = xIntercept;
            connectors[i, 3] = yIntercept;
                
        }

        //iterate through room node fields, copying data in
        for (int i = 0; i < numNodeConnects; i++) {
            for (int j = 0; j < 4; j++) {
                connectors[i + numEdgeConnects, j] = Convert.ToDouble(roomNodeValues[i][j]);
            }
        }


        return connectors;
    }

    /**
    This function uses coordinate geometry to find the intersection of a line defined by a set of coordinates
    and a line defined by a start coordinate and an angle in degrees.
    */
    public (double, double) CalcIntersectionOfEdgeAndRoomConnector(double node1X, double node1Y, double node2X, double node2Y, double roomX, double roomY, double angle) {

        double xIntercept, yIntercept;
        // deal with possibility that angle might be 90 or -90 which leads to undefined tan output
        if (Convert.ToDouble(angle) == 90 || Convert.ToDouble(angle) == -90) {
            // check if edge is horizontal
            if (Convert.ToDouble(node1Y) == Convert.ToDouble(node2Y)) {
                // door angle is straight up or down and the edge is exactly horizontal
                xIntercept = roomX;
                yIntercept = node1Y;
            }
            else {
                // edge is not perpendicular but room connector goes straight up
                // really should never execute, but have to code to be safe
                // derived from equation for a line
                // m1 is gradient of the edge
                double m1 = (Convert.ToDouble(node1Y) - Convert.ToDouble(node2Y)) / (Convert.ToDouble(node1X) - Convert.ToDouble(node2X));
                xIntercept = Convert.ToDouble(roomX);
                yIntercept = m1*(xIntercept - Convert.ToDouble(node1X)) + Convert.ToDouble(node1Y);
            }
        }
        // deal with possibility that edge angle might be 0 or 180 which leads to infinite edge gradient
        else if (Convert.ToDouble(angle) == 0 || Convert.ToDouble(angle) == 180) {
            // check if edge is vertical
            if (Convert.ToDouble(node1X) == Convert.ToDouble(node2X)) {
                // door angle is straight left or right and the edge is exactly vertical
                xIntercept = node1X;
                yIntercept = roomY;
            }
            else {
                xIntercept = double.NaN;
                yIntercept = double.NaN;
            }
        }
        else {
            // derived from equation for a line
            // m1 is gradient of the edge
            double m1 = (Convert.ToDouble(node1Y) - Convert.ToDouble(node2Y)) / (Convert.ToDouble(node1X) - Convert.ToDouble(node2X));
            
            // m2 is gradient of the room connector
            double m2 = Convert.ToDouble(MathF.Tan(Convert.ToSingle(angle)*MathF.PI/180));

            xIntercept = (m1*Convert.ToDouble(node1X) - m2*Convert.ToDouble(roomX) + Convert.ToDouble(roomY) - Convert.ToDouble(node1Y)) / (m1 - m2);
            yIntercept = m1*(xIntercept - Convert.ToDouble(node1X)) + Convert.ToDouble(node1Y);
        }

        return (xIntercept, yIntercept);
    }

    /**
    This function uses SQL to get the room_id, node_id, edge_id for a roomID.
    It will be used to figure out if a room is connected to node or edge.
    */
    public string[] GetRoomConnectionType(string room_id) {

        // query db
        var (roomFields, roomValues) = ExecuteSelect("select room_id, edge_id, node_id from tblRoom where room_id = \"" + room_id + "\"");

        // now format for return
        string[] roomInfo = new string[3];
        roomInfo[0] = "" + Convert.ToString(roomValues[0][0]);
        roomInfo[1] = "" + Convert.ToString(roomValues[0][1]);
        roomInfo[2] = "" + Convert.ToString(roomValues[0][2]);

        return roomInfo;
    }

    /**
    This function uses SQL to get an edge's record
    */
    public string[] GetEdgeRecord(int edge_id) {

        // query db
        var (edgeFields, edgeValues) = ExecuteSelect("select edge_id, node_1_id, node_2_id, weight, edge_type_id, one_way from tblEdge where edge_id = \"" + edge_id + "\"");

        // now format for return
        string[] edgeInfo = new string[6];
        edgeInfo[0] = "" + Convert.ToString(edgeValues[0][0]);
        edgeInfo[1] = "" + Convert.ToString(edgeValues[0][1]);
        edgeInfo[2] = "" + Convert.ToString(edgeValues[0][2]);
        edgeInfo[3] = "" + Convert.ToString(edgeValues[0][3]);
        edgeInfo[4] = "" + Convert.ToString(edgeValues[0][4]);
        edgeInfo[5] = "" + Convert.ToString(edgeValues[0][5]);

        return edgeInfo;
    }

    /**
    This function uses SQL to get an node's record
    */
    public string[] GetNodeRecord(int node_id) {

        // query db
        var (nodeFields, nodeValues) = ExecuteSelect("select node_id, x_coordinate, y_coordinate, floor, node_name, node_descript from tblNode where node_id = \"" + node_id + "\"");

        // now format for return
        string[] nodeInfo = new string[6];
        nodeInfo[0] = "" + Convert.ToString(nodeValues[0][0]);
        nodeInfo[1] = "" + Convert.ToString(nodeValues[0][1]);
        nodeInfo[2] = "" + Convert.ToString(nodeValues[0][2]);
        nodeInfo[3] = "" + Convert.ToString(nodeValues[0][3]);
        nodeInfo[4] = "" + Convert.ToString(nodeValues[0][4]);
        nodeInfo[5] = "" + Convert.ToString(nodeValues[0][5]);

        return nodeInfo;
    }

    /**
    This function uses SQL to get an node's record
    */
    public string[] GetRoomRecord(string room_id) {

        // query db
        var (roomFields, roomValues) = ExecuteSelect("select room_id, room_name, edge_id, node_id, x_coordinate, y_coordinate, door_angle, faculty_id, room_type from tblRoom where room_id = \"" + room_id + "\"");
        // now format for return
        string[] roomInfo = new string[9];
        roomInfo[0] = "" + Convert.ToString(roomValues[0][0]);
        roomInfo[1] = "" + Convert.ToString(roomValues[0][1]);
        roomInfo[2] = "" + Convert.ToString(roomValues[0][2]);
        roomInfo[3] = "" + Convert.ToString(roomValues[0][3]);
        roomInfo[4] = "" + Convert.ToString(roomValues[0][4]);
        roomInfo[5] = "" + Convert.ToString(roomValues[0][5]);
        roomInfo[6] = "" + Convert.ToString(roomValues[0][6]);
        roomInfo[7] = "" + Convert.ToString(roomValues[0][7]);
        roomInfo[8] = "" + Convert.ToString(roomValues[0][8]);

        return roomInfo;
    }

    /**
    This function uses SQL to get the floor that a node is on
    */
    public int GetNodeFloor(int node_id) {
        return Convert.ToInt32(ExecuteScalarSelect("select floor from tblnode where node_id = \"" + node_id + "\""));
    }

    /**
    This function uses SQL to get the floor that a node is on. It works for rooms that are connected to nodes and edges.
    */
    public int GetRoomFloor(string room_id) {
        string[] roomRecord = GetRoomRecord(room_id);
        if (roomRecord[2] == "" && roomRecord[3] != "") {
            // connected to node or is node
            return Convert.ToInt32(ExecuteScalarSelect("select floor from tblroom inner join tblnode on tblroom.node_id = tblnode.node_id where room_id = \"" + room_id + "\""));

        }
        else if (roomRecord[2] != "" && roomRecord[3] == "") {
            // connected to edge
            return Convert.ToInt32(ExecuteScalarSelect("select floor from tblroom inner join tbledge on tbledge.edge_id = tblroom.edge_id inner join tblnode on tbledge.node_1_id = tblnode.node_id where room_id = \"" + room_id + "\""));
        }
        else {
            return -1;
        }
    }
    /**
    This function uses SQL to get the coordinates of a node.
    */
    public double[] GetNodeCoordinates(int node_id) {
        var(nodeFields, nodeValues) = ExecuteSelect("select x_coordinate, y_coordinate from tblnode where node_id = \"" + node_id + "\"");
        return new double[] {Math.Round(Convert.ToDouble(nodeValues[0][0]), 3), Math.Round(Convert.ToDouble(nodeValues[0][1]), 3)};
    }
    
    /**
    This function uses SQL to get the coordinates of the edge and coordinates and angle of a room thats attached to an edge
    */
    public double[] GetRoomEdgeInfoForIntersection(string room_id) {
        var (roomEdgeFields, roomEdgeValues) = ExecuteSelect("select t1.x_coordinate, t1.y_coordinate,  t2.x_coordinate, t2.y_coordinate, tR.x_coordinate, tR.y_coordinate, tR.door_angle from tblRoom tR inner join tblEdge on tR.edge_id = tblEdge.edge_id inner join tblNode t1 on tblEdge.node_1_id = t1.node_id inner join tblNode t2 on tblEdge.node_2_id = t2.node_id where tR.x_coordinate is not null and tR.y_coordinate is not null and tR.door_angle is not null and tR.room_id = \"" + room_id + "\" order by tR.edge_id asc");

        double[] result = new double[7];
        result[0] = Convert.ToDouble(roomEdgeValues[0][0]);
        result[1] = Convert.ToDouble(roomEdgeValues[0][1]);
        result[2] = Convert.ToDouble(roomEdgeValues[0][2]);
        result[3] = Convert.ToDouble(roomEdgeValues[0][3]);
        result[4] = Convert.ToDouble(roomEdgeValues[0][4]);
        result[5] = Convert.ToDouble(roomEdgeValues[0][5]);
        result[6] = Convert.ToDouble(roomEdgeValues[0][6]);

        return result;
    }

    /**
    This function uses SQL to return the edge id if the given nodes have an edge that exists in tblEdgeVertex.
    It returns -1 if no edge is found.
    */
    public int GetEdgeIfEdgeVerticesExist(int node_1_id, int node_2_id) {

        // query db
        return Convert.ToInt32(ExecuteScalarSelect("select distinct tblEdge.edge_id  from tblEdge inner join tblEdgeVertex on tblEdge.edge_id = tblEdgeVertex.edge_id where (node_1_id = " + node_1_id + " and node_2_id = " + node_2_id + ") or (node_1_id = " + node_2_id + " and node_2_id = " + node_1_id + ")"));

    }

    /**
    This function gets all the edge vertices for a given edge and startnode, ordering them appropriately
    */
    public List<double[]> GetEdgeVertices(int edge_id, int node_id) {

        string[] edgeRecord = GetEdgeRecord(edge_id);
        //query db for edge vertices for the edge
        // if the first specified node is the start node, the order will be correct in tbledgevertex, so ascending
        // otherwise, use descending coordinateValues;
        if (Convert.ToInt32(edgeRecord[1]) == node_id) {
            // can use ascending order of "vertex_order"
            var (coordinateFields, coordinateValues) = ExecuteSelect("select tblEdgeVertex.x_coordinate, tblEdgeVertex.y_coordinate from tbledgeVertex inner join tblEdge on tblEdge.edge_id = tblEdgeVertex.edge_id where tblEdge.edge_id = " + edge_id + " order by vertex_order asc");
            List<double[]> coordinates = new List<double[]>();
            for (int i = 0; i < coordinateValues.Count; i++) {
                coordinates.Add(new double[2] {Math.Round(Convert.ToDouble(coordinateValues[i][0]), 3), Math.Round(Convert.ToDouble(coordinateValues[i][1]), 3)});
            }
            return coordinates;
        }
        else {
            // can use descending order of "vertex_order"
            var (coordinateFields, coordinateValues) = ExecuteSelect("select tblEdgeVertex.x_coordinate, tblEdgeVertex.y_coordinate from tbledgeVertex inner join tblEdge on tblEdge.edge_id = tblEdgeVertex.edge_id where tblEdge.edge_id = " + edge_id + " order by vertex_order desc");
            List<double[]> coordinates = new List<double[]>();
            for (int i = 0; i < coordinateValues.Count; i++) {
                coordinates.Add(new double[2] {Math.Round(Convert.ToDouble(coordinateValues[i][0]), 3), Math.Round(Convert.ToDouble(coordinateValues[i][1]), 3)});
            }
            return coordinates;
        }
        // had to rewrite code as vs code wasn't convinced that coordinateValues would exist otherwise
    }

    /**
    This function uses SQL to get the start type (node or room) of a given input location.
    */
    public string GetLocationType(string location) {

        bool isNum;
        // check if input is num
        try {
            Convert.ToInt32(location);
            isNum = true;
        }
        catch {
            isNum = false;
        }

        // default query results to 0
        double roomQuery = 0;
        double nodeQuery = 0;
        // check tblroom
        if (isNum) {
            // check tblnode
            nodeQuery = ExecuteScalarSelect("select Count(node_id) from tblNode where node_id = " + location);
        }
        else {
            roomQuery = ExecuteScalarSelect("select Count(room_id) from tblRoom where room_id = \"" + location + "\"");
        }
        
        if (roomQuery == 1 && nodeQuery == 0) {
            // is room
            // now figure out which type
            string[] roomRecord = GetRoomRecord(location);
            if (roomRecord[2] == "" && roomRecord[3] != "") {
                // connected to/is a node
                if (roomRecord[4] != "" && roomRecord[5] != "") {
                    return "RNC";
                }
                else {
                    return "RN ";
                }
            }
            else if (roomRecord[2] != "" && roomRecord[3] == ""){
                // is connected to an edge
                string[] edgeRecord = GetEdgeRecord(Convert.ToInt32(roomRecord[2]));
                // undirectional if actually undirectional or one-way system is off
                if (edgeRecord[5] == "False" || !userSettings.oneWaySystem) { 
                    // room connected to edge that is undirection
                    return "REU";
                }
                else if (edgeRecord[5] == "True"){
                    return "RED";
                }
                else { // one-way is null
                    Debug.Log($"One-way is null for room {location}.");
                    return "   ";

                }
            }
            else { // info for both node and edge in room record
                Debug.Log($"There is both a node_id and edge_id specified for room {location}.");
                return "   ";
            }
        }
        else if (roomQuery == 0 && nodeQuery == 1) {
            // is a node from tblnode
            return "N  ";
        }
        else { // no entry or two entries
            Debug.Log($"There is either no record matching input {location} in tblNode and tblRoom or a record in both tblNode and tblRoom.");
            return "   ";
        }
    }

    /**
    This function uses SQL to get the floor of a location.
    */
    public bool GetLocationFloor(string id, string type) {
        // check if room
        if (type.Substring(0, 1) == "R") {
            int floorInt = GetRoomFloor(id);
            if (floorInt == 0) {
                return false;
            }
            else {
                return true;
            }
        }
        // check if node
        else if (type.Substring(0, 1) == "N") {
            int floorInt = GetNodeFloor(Convert.ToInt32(id));
            if (floorInt == 0) {
                return false;
            }
            else {
                return true;
            }
        }
        // raise error
        else {
            // raise runtime error
            throw new ArgumentException("Invalid input for id.");
        }
    }

    /**
    This function uses SQL to get the coordinates of a location.
    */
    public Vector2 GetLocationCoordinates(string id, string type) {
        // check if room
        if (type.Substring(0, 1) == "R") {
            if (type == "RN ") { // room that is node so get the node coordinates
                string[] roomRecord = GetRoomRecord(id);
                string nodeID = roomRecord[3];
                string[] nodeRecord = GetNodeRecord(Convert.ToInt32(nodeID));
                return new Vector2(Convert.ToSingle(nodeRecord[1]), Convert.ToSingle(nodeRecord[2]));
            }
            else { // room that is not a node so has its own defined coordinates
                string[] roomRecord = GetRoomRecord(id);
                return new Vector2(Convert.ToSingle(roomRecord[4]), Convert.ToSingle(roomRecord[5]));
            }
        }
        // check if node
        else if (type.Substring(0, 1) == "N") {
            string[] nodeRecord = GetNodeRecord(Convert.ToInt32(id));
            return new Vector2(Convert.ToSingle(nodeRecord[1]), Convert.ToSingle(nodeRecord[2]));
        }
        // raise error
        else {
            // raise runtime error
            throw new ArgumentException("Invalid input for id.");
        }
    }

    /**
    This function uses SQL to get the camera's minimum size
    */
    public double GetMinCameraSize() {
        return ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"minCameraSize\"");
    }

    /**
    This function uses SQL to get the zoom step of the camera
    */
    public double GetNumCameraZoomIncrements() {
        return ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"cameraNumZoomIncrement\"");
    }

    /**
    This function uses SQL to get the map's maximum and minimum x and y values.
    Returns in order: {maxX, minX, maxY, minY}
    */
    public float[] GetMapBounds() {
        float maxX = Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"mapMaxX\""));
        float minX = Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"mapMinX\""));
        float maxY = Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"mapMaxY\""));
        float minY = Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"mapMinY\""));

        return new float[] {maxX, minX, maxY, minY};
    }

    /**
    This function uses SQL to get the map buffer - a value that extends the camera's possible movement over the map.
    */
    public float GetMapBuffer() {
        return Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"mapBuffer\""));
    }

    /**
    This function uses SQL to get the camera start coordinates.
    */
    public Vector3 GetCameraStartCoordinates() {
        // get from db, return z as 0 but dont take from it
        return new Vector3 (Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"cameraStartX\"")), Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"cameraStartY\"")), 0);
    }

    /**
    This function uses SQL to get the camera start size.
    */
    public float GetCameraStartSize() {
        return Convert.ToSingle(ExecuteScalarSelect("select setting_value from tblsetting where setting_name = \"cameraStartSize\""));
    }
    
    /**
    This function uses SQL to get all text labels for the specified floor.
    */
    public List<string[]> GetTextLabels(bool floor) {
        int floorNum;
        if (!floor) {
            floorNum = 0;
        }
        else {
            floorNum = 1;
        }

        var (textFields, textValues) = ExecuteSelect("select text_content, x_coordinate, y_coordinate, font_size, width, height from tbltextlabel where floor = " + Convert.ToString(floorNum));
        List<string[]> result = new List<string[]>(); // setyp return data structure
        for (int i = 0; i < textValues.Count; i++) {
            result.Add(new string[6]); // append a new array
            for (int j = 0; j < textValues[0].Count; j++) {
                result[i][j] = Convert.ToString(textValues[i][j]);
            }
        }
        return result;
    }

    /**
    This function uses SQL to get all toilet symbols for the specified floor.
    */
    public List<string[]> GetToiletSymbols(bool floor) {
        int floorNum;
        if (!floor) {
            floorNum = 0;
        }
        else {
            floorNum = 1;
        }

        var (toiletFields, toiletValues) = ExecuteSelect("select toilet_id, x_coordinate, y_coordinate, toilet_type from tbltoiletsymbol where floor = " + Convert.ToString(floorNum));
        List<string[]> result = new List<string[]>(); // setyp return data structure
        for (int i = 0; i < toiletValues.Count; i++) {
            result.Add(new string[4]); // append a new array
            for (int j = 0; j < toiletValues[0].Count; j++) {
                result[i][j] = Convert.ToString(toiletValues[i][j]);
            }
        }
        return result;
    }

    /**
    This function uses SQL to check if a given room id has a room name and returns it if it does or an empty string if not.
    */
    public string GetRoomNameFromID(string roomID) {
        var parameters = new Dictionary<string, object> {{"@room_id", roomID}};
        var (roomFields, roomValues) = ExecuteParametrisedSelect("select room_name from tblRoom where room_id = @room_id", parameters);
        if (roomValues.Count == 0) {
            return "";
        }
        else {
            return Convert.ToString(roomValues[0][0]);
        }
    }

    /**
    This function uses SQL to return true if a room id exists within tblRoom.
    */
    public bool RoomIDExists(string roomID) {
        var parameters = new Dictionary<string, object> {{"@room_id", roomID}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(room_id) from tblRoom where room_id = @room_id", parameters)) == 1;
    }

    /**
    This function uses SQL to return true if a room record exists for the room name.
    Not case sensitive.
    */
    public bool RoomNameExists(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", roomName}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(room_id) from tblRoom where room_name = @room_name", parameters)) == 1;
    }

    /**
    This function uses SQL to return the exact room id for the input room name.
    */
    public string GetRoomIDFromName(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", roomName}};
        var (roomFields, roomValues) = ExecuteParametrisedSelect("select room_id from tblRoom where room_name = @room_name", parameters);
        return Convert.ToString(roomValues[0][0]);
    }

    /**
    This function uses SQL to return the correctly title cased room name for the input room name.
    */
    public string GetRoomNameFromName(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", roomName}};
        var (roomFields, roomValues) = ExecuteParametrisedSelect("select room_name from tblRoom where room_name = @room_name", parameters);
        return Convert.ToString(roomValues[0][0]);
    }

    /**
    This function uses SQL to return the number of records where the input is a substring of room name.
    */
    public int GetRoomNameSubstringCount(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", "%" + roomName + "%"}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(room_id) from tblRoom where room_name like @room_name", parameters));
    }

    /**
    This function uses SQL to return the exact room_id where there is only one record where the input is a substring of the room name.
    */
    public string GetRoomIDFromSubstringName(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", "%" + roomName + "%"}};
        var (roomFields, roomValues) = ExecuteParametrisedSelect("select room_id from tblRoom where room_name like @room_name", parameters);
        return Convert.ToString(roomValues[0][0]);
    }

    /**
    This function uses SQL to return the room_name where there is only one record where the input is a substring of the room name.
    */
    public string GetRoomNameFromSubstringName(string roomName) {
        var parameters = new Dictionary<string, object> {{"@room_name", "%" + roomName + "%"}};
        var (roomFields, roomValues) = ExecuteParametrisedSelect("select room_name from tblRoom where room_name like @room_name", parameters);
        return Convert.ToString(roomValues[0][0]);
    }

    /**
    This function uses SQL to return true if the node id matches a record
    */
    public bool NodeIDExists(string nodeID) {
        if (!int.TryParse(nodeID, out _)) { // contains non intger characters
            return false;
        }
        var parameters = new Dictionary<string, object> {{"@node_id", nodeID}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(node_id) from tblNode where node_id = @node_id", parameters)) == 1;
    }

    /**
    This function uses SQL to check if a given node id has a node name and returns it if it does or an empty string if not.
    */
    public string GetNodeNameFromID(string nodeID) {
        var parameters = new Dictionary<string, object> {{"@node_id", nodeID}};
        var (nodeFields, nodeValues) = ExecuteParametrisedSelect("select node_name from tblNode where node_id = @node_id", parameters);
        if (nodeValues.Count == 0) {
            return "";
        }
        else {
            return Convert.ToString(nodeValues[0][0]);
        }
    }

    /**
    This function uses SQL to return true if a node record exists for the node name.
    */
    public bool NodeNameExists(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", nodeName}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(node_id) from tblNode where node_name = @node_name", parameters)) == 1;
    }

    /**
    This function uses SQL to return the exact node id for the input node name/
    */
    public int GetNodeIDFromName(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", nodeName}};
        var (nodeFields, nodeValues) = ExecuteParametrisedSelect("select node_id from tblNode where node_name = @node_name", parameters);
        return Convert.ToInt32(nodeValues[0][0]);
    }

    /**
    This function uses SQL to return the correctly title cased node name for the input node name.
    */
    public string GetNodeNameFromName(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", nodeName}};
        var (nodeFields, nodeValues) = ExecuteParametrisedSelect("select node_name from tblNode where node_name = @node_name", parameters);
        return Convert.ToString(nodeValues[0][0]);
    }

    /**
    This function uses SQL to return the number of records where the input is a substring of node name.
    */
    public int GetNodeNameSubstringCount(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", "%" + nodeName + "%"}};
        return Convert.ToInt32(ExecuteParametrisedScalarSelect("select count(node_id) from tblNode where node_name like @node_name", parameters));
    }

    /**
    This function uses SQL to return the exact node_id where there is only one record where the input is a substring of the node name.
    */
    public int GetNodeIDFromSubstringName(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", "%" + nodeName + "%"}};
        var (nodeFields, nodeValues) = ExecuteParametrisedSelect("select node_id from tblNode where node_name like @node_name", parameters);
        return Convert.ToInt32(nodeValues[0][0]);
    }

    /**
    This function uses SQL to return the node_name where there is only one record where the input is a substring of the node name.
    */
    public string GetNodeNameFromSubstringName(string nodeName) {
        var parameters = new Dictionary<string, object> {{"@node_name", "%" + nodeName + "%"}};
        var (nodeFields, nodeValues) = ExecuteParametrisedSelect("select node_name from tblNode where node_name like @node_name", parameters);
        return Convert.ToString(nodeValues[0][0]);
    }
}
