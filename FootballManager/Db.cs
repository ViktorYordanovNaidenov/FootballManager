using System;
using System.Data.SqlClient; // Тази библиотека ни трябва за MS SQL Server

namespace FootballManager
{
    public static class Db
    {

        // В Db.cs
        // Внимавай да не изтриеш кавичките или точката и запетаята!
        private static string connectionString = @"Server=.\SQLEXPRESS;Database=FootballManagerDB;Integrated Security=True;TrustServerCertificate=True;";

        // Метод за взимане на отворена връзка
        public static SqlConnection GetConnection()
        {
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }
    }
}