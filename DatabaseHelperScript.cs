using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MySqlConnector;


public class DatabaseHelperScript : MonoBehaviour
{
    // fields
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
        uid = "root";
        password = "rU2n4s?Qf6gEb!pIbci8";
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
                    Console.WriteLine("Cannot connect to sever.");
                    break;
                case 1045:
                    Console.WriteLine("Invalid username/password.");
                    break;
                default:
                    Console.WriteLine("An error has occured.");
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
    This function takes an insert, delete or update query and carries out the action.
    It returns whether or not it was successful.
    It cannot be used by select as this returns results.
    */
    private bool ExecuteSqlVoid(string query) {

        bool complete = false;
        // open connection
        if (OpenConnection() == true) {
            // create command
            MySqlCommand command = new MySqlCommand(query, connection);
            // execute command
            command.ExecuteNonQuery();
            // close connection
            CloseConnection();

            complete = true;
        }
        return complete;
    }

    // The following three methods are not technically needed
    // but are nice as it will be clearer when reading code what functions do.

    private bool ExecuteInsert(string query) {
        return ExecuteSqlVoid(query);
    }

    private bool ExecuteUpdate(string query) {
        return ExecuteSqlVoid(query);
    }

    private bool ExecuteDelete(string query) {
        return ExecuteSqlVoid(query);
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
            // create mysql command
            MySqlCommand command = new MySqlCommand(query, connection);
            // execute command
            MySqlDataReader reader = command.ExecuteReader();

            // read field names
            for (int i = 0; i < reader.FieldCount; i++) {
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

            // close connection
            CloseConnection();
        }

        return (fieldNames, columnedValues);
    }

    /**
    This function carries out the scalar select.
    This is useful for Count(*) or average of values.
    It takes a query and returns -1 if there is an error.
    */
    private double ExecuteScalarSelect(string query) {

        double scalarValue = -1;

        // open connection
        if (OpenConnection() == true) {
            // create mysql command
            MySqlCommand command = new MySqlCommand(query, connection);
            // execute scalar will only return one value
            scalarValue = double.Parse(command.ExecuteScalar()+"");
            // close connection
            CloseConnection();

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
    This function uses SQL to get the number of nodes in tblnode.
    */
    public int GetNumberOfEdges() {
        
        return Convert.ToInt32(ExecuteScalarSelect("SELECT Count(edge_id) FROM tbledge"));
    }

    /**
    This function uses SQL to get all nodes in tblnode.
    */
    public (List<string>, List<List<object>>) GetNodes() {

        return ExecuteSelect("SELECT * FROM tblnode");
    }

    /**
    This function uses SQL to get all records in tbledge.
    */
    public (List<string>, List<List<object>>) GetEdges() {

        return ExecuteSelect("SELECT * FROM tbledge");
    }

    /**
    This function uses SQL to get all edges that are one way
    */
    public (List<string>, List<List<object>>) GetOneWayEdges() {

        return ExecuteSelect("SELECT * FROM tbledge WHERE one_way = true");
    }
    
    /**
    This function uses SQL to get all edge ID's
    */
    public int[] GetEdgeIDs() {

        // query db
        var (edgeFields, edgeValues) = ExecuteSelect("select edge_id from tblEdge order by edge_id asc");
        int numberOfEdges = edgeValues.Count;

        // return array
        int[] edgeIDs = new int[numberOfEdges];

        for (int i = 0; i < numberOfEdges; i++) {
            edgeIDs[i] = Convert.ToInt32(edgeValues[i][0]);
        }

        return edgeIDs;
    }

    /**
    This function uses SQL to source the boolean value controlling whether the time of day
    can be used in calculations for estimations of time.
    */
    public bool GetTimeOfDayDB() {
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
    This procedure is only used in testing but uses SQL to save a map edge
    */
    public void SaveMapEdge(float point1x, float point1y, float point2x, float point2y) {

        // query db
        ExecuteInsert("INSERT INTO tblMapEdge (point_1_x, point_1_y, point_2_x, point_2_y, floor_0, floor_1) VALUES ("+Convert.ToString(point1x)+", "+Convert.ToString(point1y)+", " +Convert.ToString(point2x)+", " +Convert.ToString(point2y)+", 1, 0)");
    }

    /**
    This procedure is only used in development to enter edges into SQL server
    */
    public void SaveMapEdge2(string points) {
        ExecuteInsert("INSERT INTO tblMapEdge (point_1_x, point_1_y, point_2_x, point_2_y, floor_0, floor_1) VALUES (" + points + ")");
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
            double xIntercept, yIntercept;
            // deal with possibility that angle might be 90 or -90 which leads to undefined tan output
            if (Convert.ToDouble(roomEdgeValues[i][6]) == 90 || Convert.ToDouble(roomEdgeValues[i][6]) == -90) {
                // check if edge is horizontal
                if (Convert.ToDouble(roomEdgeValues[i][1]) == Convert.ToDouble(roomEdgeValues[i][3])) {
                    // door angle is straight up or down and the edge is exactly horizontal
                    xIntercept = Convert.ToDouble(roomEdgeValues[i][4]);
                    yIntercept = Convert.ToDouble(roomEdgeValues[i][1]);
                }
                else {
                    // edge is not perpendicular but room connector goes straight up
                    // really should never execute, but have to code to be safe

                    // derived from equation for a line
                    // m1 is gradient of the edge
                    double m1 = (Convert.ToDouble(roomEdgeValues[i][1]) - Convert.ToDouble(roomEdgeValues[i][3])) / (Convert.ToDouble(roomEdgeValues[i][0]) - Convert.ToDouble(roomEdgeValues[i][2]));

                    xIntercept = Convert.ToDouble(roomEdgeValues[i][4]);
                    yIntercept = m1*(xIntercept - Convert.ToDouble(roomEdgeValues[i][0])) + Convert.ToDouble(roomEdgeValues[i][1]);

                }
            }
            // deal with possibility that edge angle might be 0 or 180 which leads to infinite edge gradient
            else if (Convert.ToDouble(roomEdgeValues[i][6]) == 0 || Convert.ToDouble(roomEdgeValues[i][6]) == 180) {
                // check if edge is vertical
                if (Convert.ToDouble(roomEdgeValues[i][0]) == Convert.ToDouble(roomEdgeValues[i][2])) {
                    // door angle is straight left or right and the edge is exactly vertical
                    xIntercept = Convert.ToDouble(roomEdgeValues[i][0]);
                    yIntercept = Convert.ToDouble(roomEdgeValues[i][5]);
                }
                else {
                    Debug.Log("Error: edge is vertical but room connector is not");
                    xIntercept = double.NaN;
                    yIntercept = double.NaN;

                }
            }
            else {
                // derived from equation for a line
                // m1 is gradient of the edge
                double m1 = (Convert.ToDouble(roomEdgeValues[i][1]) - Convert.ToDouble(roomEdgeValues[i][3])) / (Convert.ToDouble(roomEdgeValues[i][0]) - Convert.ToDouble(roomEdgeValues[i][2]));
                
                // m2 is gradient of the room connector
                double m2 = Convert.ToDouble(Mathf.Tan(Convert.ToSingle(roomEdgeValues[i][6])*Mathf.PI/180));
 
                xIntercept = (m1*Convert.ToDouble(roomEdgeValues[i][0]) - m2*Convert.ToDouble(roomEdgeValues[i][4]) + Convert.ToDouble(roomEdgeValues[i][5]) - Convert.ToDouble(roomEdgeValues[i][1])) / (m1 - m2);
                yIntercept = m1*(xIntercept - Convert.ToDouble(roomEdgeValues[i][0])) + Convert.ToDouble(roomEdgeValues[i][1]);
            }

            
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
}
