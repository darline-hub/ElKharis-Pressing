using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;
using ElKharis.Database;
using BCrypt.Net;
using System.Text.RegularExpressions; // Déjà présent et maintenant exploité

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour SigninWindow.xaml
    /// </summary>
    public partial class SigninWindow : Window
    {
        public SigninWindow()
        {
            InitializeComponent();
        }

        private void BtnSignin_Click(object sender, RoutedEventArgs e)
        {
            string username = TxtUsername.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string password = TxtPassword.Password.Trim();

            string role = "Réceptionniste";
            // CORRECTION CS8600 : Utilisation de l'opérateur de coalescence active '??' 
            // et vérification que Content n'est pas nul avant le ToString
            if (CboRole?.SelectedItem is ComboBoxItem selectedItem)
            {
                role = selectedItem.Content?.ToString() ?? "Réceptionniste";
            }

            // 1. VÉRIFICATION DES CHAMPS VIDES
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires !", "Champs incomplets", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 2. VÉRIFICATION STRICTE DE L'EMAIL (Regex professionnelle avec '@' et domaine)
            string modeleEmail = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            if (!Regex.IsMatch(email, modeleEmail))
            {
                MessageBox.Show("Veuillez entrer une adresse email valide contenant un symbole '@' et un domaine (ex: nom@email.com) !", "Format incorrect", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 3. SÉCURITÉ : VÉRIFICATION DE LA LONGUEUR DU MOT DE PASSE (Minimum 6 caractères)
            if (password.Length < 6)
            {
                MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères pour des raisons de sécurité.", "Mot de passe trop court", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Hachage sécurisé du mot de passe après validation
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

            try
            {
                // CORRECTION CS8603 (si applicable ici) : On récupère la connexion.
                // On vérifie immédiatement qu'elle n'est pas nulle pour rassurer le compilateur.
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null)
                    {
                        MessageBox.Show("Erreur : La connexion à la base de données n'a pas pu être initialisée.", "Erreur Connexion", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    if (conn.State != System.Data.ConnectionState.Open)
                    {
                        conn.Open();
                    }

                    // Suite de ton code d'insertion (Vérification doublons + INSERT)...
                    string checkQuery = "SELECT COUNT(*) FROM utilisateurs WHERE nom_utilisateur = @username OR email = @email";
                    using (MySqlCommand checkCmd = new MySqlCommand(checkQuery, conn))
                    {
                        checkCmd.Parameters.AddWithValue("@username", username);
                        checkCmd.Parameters.AddWithValue("@email", email);

                        int userExists = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (userExists > 0)
                        {
                            MessageBox.Show("Ce nom d'utilisateur ou cet email est déjà utilisé !", "Doublon", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string insertQuery = "INSERT INTO utilisateurs (nom_utilisateur, email, role, mot_de_passe) VALUES (@p_user, @p_email, @p_role, @p_mdp)";
                    using (MySqlCommand cmd = new MySqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@p_user", username);
                        cmd.Parameters.AddWithValue("@p_email", email);
                        cmd.Parameters.AddWithValue("@p_role", role);
                        cmd.Parameters.AddWithValue("@p_mdp", hashedPassword);

                        if (TxtPassword.Password.Length < 6)
                        {
                            MessageBox.Show("Le mot de passe doit contenir au moins 6 caractères pour des raisons de sécurité.",
                                            "Mot de passe trop court", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        int lignesModifiees = cmd.ExecuteNonQuery();
                        if (lignesModifiees > 0)
                        {
                            MessageBox.Show($"L'employé '{username}' a été enregistré avec succès en tant que '{role}' !", "Inscription réussie", MessageBoxButton.OK, MessageBoxImage.Information);
                            this.DialogResult = true;
                            this.Close();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement dans la base de données : " + ex.Message, "Erreur Critique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}