using System;
using System.Collections.Generic;
using System.Data;
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
    /// Logique d'interaction pour NouveauClientWindow.xaml
    /// </summary>
    public partial class NouveauClientWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";

        private bool isEditMode = false;
        private int clientIdModif = 0;

        public NouveauClientWindow()
        {
            InitializeComponent();
            isEditMode = false;
        }
        public NouveauClientWindow(DataRow rowModif)
        {
            InitializeComponent();
            isEditMode = true;

            // Récupération de l'ID unique du client à modifier
            clientIdModif = Convert.ToInt32(rowModif["id_client"]);

            // Remplissage automatique des champs XAML avec les données actuelles de la ligne SQL
            TxtNom.Text = rowModif["nom"]?.ToString();
            TxtPrenom.Text = rowModif["prenom"]?.ToString();
            TxtTelephone.Text = rowModif["telephone"]?.ToString();
            TxtEmail.Text = rowModif["email"]?.ToString();
            TxtVille.Text = rowModif["ville"]?.ToString();
            TxtQuartier.Text = rowModif["quartier"]?.ToString();

            // Gestion du ComboBox Sexe
            string sexe = rowModif["sexe"]?.ToString() ?? "M";
            if (CboSexe != null)
            {
                if (sexe == "M" || sexe == "Masculin") CboSexe.SelectedIndex = 0;
                else if (sexe == "F" || sexe == "Féminin") CboSexe.SelectedIndex = 1;
            }
        }
        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            string nom = TxtNom.Text.Trim();
            string prenom = TxtPrenom.Text.Trim();
            string telephone = TxtTelephone.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string ville = TxtVille.Text.Trim();
            string quartier = TxtQuartier.Text.Trim();

            string sexe = "M";
            if (CboSexe?.SelectedItem is ComboBoxItem selectedItem)
            {
                sexe = selectedItem.Content?.ToString() ?? "M"; // (Tu peux laisser ton code de sexe existant ici)
            }

            // 1. Validation des champs requis
            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(telephone))
            {
                MessageBox.Show("Le nom et le numéro de téléphone sont obligatoires !", "Données manquantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // CORRECTION 3 : Exiger STRICTEMENT le caractère '@' et un point si l'email est renseigné
            if (!string.IsNullOrEmpty(email))
            {
                if (!email.Contains("@") || !email.Contains("."))
                {
                    MessageBox.Show("Veuillez entrer une adresse email valide contenant un '@' et un point !", "Format Email Incorrect", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
            }
            else
            {
                // Si tu veux que l'email soit TOUJOURS obligatoire (pas optionnel), active ce bloc :
                // MessageBox.Show("L'adresse email est obligatoire !", "Données manquantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                // return;
            }

            long nouveauIdClient = 0;

            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    // Requête d'insertion simple
                    string query = @"INSERT INTO clients (nom, prenom, sexe, telephone, email, ville, quartier) 
                            VALUES (@nom, @prenom, @sexe, @telephone, @email, @ville, @quartier)";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", nom);
                        cmd.Parameters.AddWithValue("@prenom", prenom);
                        cmd.Parameters.AddWithValue("@sexe", sexe);
                        cmd.Parameters.AddWithValue("@telephone", telephone);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@ville", ville);
                        cmd.Parameters.AddWithValue("@quartier", quartier);

                        cmd.ExecuteNonQuery();
                    }

                    // CORRECTION REFIABILISÉE POUR L'ID : On récupère l'id de manière isolée et sécurisée
                    string idQuery = "SELECT LAST_INSERT_ID();";
                    using (MySqlCommand idCmd = new MySqlCommand(idQuery, conn))
                    {
                        nouveauIdClient = Convert.ToInt64(idCmd.ExecuteScalar());
                    }
                }

                MessageBox.Show($"Client '{nom}' enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                // Action de redirection vers la commande
                if (ChkOuvrirCommande.IsChecked == true && nouveauIdClient > 0)
                {
                    this.Hide();

                    // Appel de la fenêtre de commande
                    NouvelleCommandeWindow cmdWindow = new NouvelleCommandeWindow(nouveauIdClient);
                    cmdWindow.Owner = this.Owner;
                    cmdWindow.ShowDialog();
                }

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la sauvegarde : \n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        
        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            // Ferme simplement la fenêtre sans sauvegarder
            this.DialogResult = false;
            this.Close();
        }
        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
