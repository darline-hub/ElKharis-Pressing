using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    public partial class FormUtilisateurWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private bool estModification = false;

        // Constructeur pour l'AJOUT
        public FormUtilisateurWindow()
        {
            InitializeComponent();
            estModification = false;
            TxtTitreFormulaire.Text = "Nouvel Utilisateur";
        }

        // Constructeur pour la MODIFICATION (on reçoit les données existantes)
        public FormUtilisateurWindow(string id, string nom, string email, string role)
        {
            InitializeComponent();
            estModification = true;
            TxtTitreFormulaire.Text = "Modifier l'Utilisateur";

            // Afficher le champ ID uniquement en modification
            PanelId.Visibility = Visibility.Visible;
            TxtId.Text = id;

            // Pré-remplir les champs
            TxtNomUtilisateur.Text = nom;
            TxtEmail.Text = email;

            // Sélectionner le bon rôle dans la ComboBox
            foreach (ComboBoxItem item in CboRole.Items)
            {
                if (item.Content.ToString() == role)
                {
                    CboRole.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validation des champs
            if (string.IsNullOrWhiteSpace(TxtNomUtilisateur.Text) || string.IsNullOrWhiteSpace(TxtEmail.Text) || CboRole.SelectedItem == null)
            {
                MessageBox.Show("Veuillez remplir tous les champs obligatoires.", "Champs requis", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string nom = TxtNomUtilisateur.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string role = ((ComboBoxItem)CboRole.SelectedItem)?.Content?.ToString() ?? "";

            // 2. Traitement en Base de Données
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = "";

                    if (estModification)
                    {
                        query = "UPDATE Utilisateurs SET nom_utilisateur = @nom, email = @email, role = @role WHERE id = @id";
                    }
                    else
                    {
                        // Note : Ajoute un champ mot_de_passe par défaut si ta table l'exige
                        query = "INSERT INTO Utilisateurs (nom_utilisateur, email, role) VALUES (@nom, @email, @role)";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", nom);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@role", role);

                        if (estModification)
                        {
                            cmd.Parameters.AddWithValue("@id", TxtId.Text);
                        }

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Opération enregistrée avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Indique à la fenêtre parente que tout s'est bien passé
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement : {ex.Message}", "Erreur SQL", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
