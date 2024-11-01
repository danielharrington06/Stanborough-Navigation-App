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
    public double[,] GetMapEdges() {
        
        // query db
        var (mapEdgeFields, mapEdgeValues) = ExecuteSelect("SELECT point_1_x, point_1_y, point_2_x, point_2_y FROM tblMapEdge");

        double[,] mapEdges = new double[mapEdgeValues.Count,4];

        for (int i = 0; i < mapEdgeValues.Count; i++) {
            for (int j = 0; j < mapEdgeValues[i].Count; j++) {
                mapEdges[i, j] = Convert.ToDouble(mapEdgeValues[i][j]);
            }
        }

        return mapEdges;
    }

    /**
    This function is only used in testing but uses SQL to save a map edge
    */
    public void SaveMapEdge(float point1x, float point1y, float point2x, float point2y) {

        // query db
        ExecuteInsert("INSERT INTO tblMapEdge (point_1_x, point_1_y, point_2_x, point_2_y, floor_0, floor_1) VALUES ("+Convert.ToString(point1x)+", "+Convert.ToString(point1y)+", " +Convert.ToString(point2x)+", " +Convert.ToString(point2y)+", 1, 0)");
    }
}
