using System;
using System.Data;
using MySql;
using MySql.Data.MySqlClient;

string connString = "server=localhost;user=root;database=your_db;password=your_password;";
using (MySqlConnection conn = new MySqlConnection(connString))
{
    try
    {
        conn.Open();
        Console.WriteLine("Connection successful!");
        // Execute queries here
    }
    catch (MySqlException ex)
    {
        Console.WriteLine("Error: " + ex.Message);
    }
}