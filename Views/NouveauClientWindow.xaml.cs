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
        private int clientIdModif = 0;

        public NouveauClientWindow()
        {
            InitializeComponent();
        }

        public NouveauClientWindow(DataRow rowModif)
        {
            InitializeComponent();

            // Récupération de l'ID unique du client à modifier
            clientIdModif = Convert.ToInt32(rowModif["id_client"]);

            // Remplissage automatique des champs XAML avec les données actuelles de la ligne SQL
            TxtNom.Text = rowModif["nom"]?.ToString() ?? string.Empty;
            TxtPrenom.Text = rowModif["prenom"]?.ToString() ?? string.Empty;
            TxtTelephone.Text = rowModif["telephone"]?.ToString() ?? string.Empty;
            TxtEmail.Text = rowModif["email"]?.ToString() ?? string.Empty;
            TxtVille.Text = rowModif["ville"]?.ToString() ?? string.Empty;
            TxtQuartier.Text = rowModif["quartier"]?.ToString() ?? string.Empty;

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
                sexe = selectedItem.Content?.ToString() ?? "M";
            }

            if (string.IsNullOrEmpty(nom) || string.IsNullOrEmpty(telephone))
            {
                MessageBox.Show("Le nom et le numéro de téléphone sont obligatoires !", "Données manquantes", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Exigence stricte du caractère '@' et '.' si l'email est rempli
            if (!string.IsNullOrEmpty(email) && (!email.Contains("@") || !email.Contains(".")))
            {
                MessageBox.Show("Veuillez entrer une adresse email valide !", "Format Incorrect", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            long nouveauIdClient = 0;

            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;
                    if (conn.State != System.Data.ConnectionState.Open) conn.Open();

                    string query;
                    // Si clientIdModif > 0, on met à jour le client existant (Mode Edition), sinon on fait un INSERT
                    if (clientIdModif > 0)
                    {
                        query = @"UPDATE clients 
                                  SET nom = @nom, prenom = @prenom, sexe = @sexe, telephone = @telephone, 
                                      email = @email, ville = @ville, quartier = @quartier 
                                  WHERE id_client = @id_client";
                    }
                    else
                    {
                        query = @"INSERT INTO clients (nom, prenom, sexe, telephone, email, ville, quartier) 
                                  VALUES (@nom, @prenom, @sexe, @telephone, @email, @ville, @quartier)";
                    }

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@nom", nom);
                        cmd.Parameters.AddWithValue("@prenom", prenom);
                        cmd.Parameters.AddWithValue("@sexe", sexe);
                        cmd.Parameters.AddWithValue("@telephone", telephone);
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@ville", ville);
                        cmd.Parameters.AddWithValue("@quartier", quartier);

                        if (clientIdModif > 0)
                        {
                            cmd.Parameters.AddWithValue("@id_client", clientIdModif);
                        }

                        cmd.ExecuteNonQuery();
                    }

                    // Enregistrement de l'identifiant pour la suite du traitement
                    if (clientIdModif > 0)
                    {
                        nouveauIdClient = clientIdModif;
                    }
                    else
                    {
                        // Récupération sécurisée du dernier ID inséré pour un nouveau client
                        using (MySqlCommand idCmd = new MySqlCommand("SELECT LAST_INSERT_ID();", conn))
                        {
                            nouveauIdClient = Convert.ToInt64(idCmd.ExecuteScalar());
                        }
                    }
                }

                MessageBox.Show($"Client '{nom}' enregistré avec succès !", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                // Si l'utilisateur veut lier immédiatement une commande (Uniquement disponible sur l'élément graphique s'il existe)
                if (ChkOuvrirCommande != null && ChkOuvrirCommande.IsChecked == true && nouveauIdClient > 0)
                {
                    this.Visibility = Visibility.Collapsed; // On cache proprement la fiche client

                    // On ouvre la commande en lui donnant le nouvel ID
                    NouvelleCommandeWindow cmdWindow = new NouvelleCommandeWindow(nouveauIdClient);
                    cmdWindow.Owner = this.Owner;
                    cmdWindow.ShowDialog();
                }

                // IMPORTANT : Qu'on ait fait une commande ou pas, ou qu'on ait annulé la commande, 
                // on valide TOUJOURS le DialogResult pour forcer la liste en arrière-plan à se recharger.
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