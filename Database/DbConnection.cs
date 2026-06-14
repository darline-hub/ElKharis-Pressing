using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using MySql.Data.MySqlClient;


namespace ElKharis.Database
{
    internal class DbConnection
    {
        private static string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";

        public static MySqlConnection? GetConnection()
        {
            MySqlConnection conn = new MySqlConnection(connectionString);
            try
            {
                conn.Open();
                return conn;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de connexion à WampServer : " + ex.Message, "Erreur Base de données", MessageBoxButton.OK, MessageBoxImage.Error);
                return null;
            }
        }
    }
}


