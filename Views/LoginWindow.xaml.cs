using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MySql.Data.MySqlClient;
using ElKharis.Database;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string password = TxtPassword.Password.Trim();

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs !", "Champs vides", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return; 

                    string query = "SELECT id, nom_utilisateur, role, email FROM utilisateurs WHERE nom_utilisateur = @username AND mot_de_passe = @password";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@username", username);
                        cmd.Parameters.AddWithValue("@password", password);

                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                Session.IdUtilisateur = reader.GetInt32("id");
                                Session.NomUtilisateur = reader.GetString("nom_utilisateur");
                                Session.Role = reader.GetString("role");
                                Session.Email = reader.GetString("email");

                                MessageBox.Show($"Connexion réussie ! Bienvenue {Session.NomUtilisateur}.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                                DashboardWindow dashboard = new DashboardWindow();
                                dashboard.Show();

                                this.Close();
                            }
                            else
                            {
                                MessageBox.Show("Nom d'utilisateur ou mot de passe incorrect.", "Échec de connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la connexion à la base de données : " + ex.Message, "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}