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
using ElKharis.Models;
using ElKharis.Services;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour ClientsWindow.xaml
    /// </summary>
    public partial class ClientsWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private DataTable dtClients = new DataTable();

        public ClientsWindow()
        {
            InitializeComponent();
            ChargerTousLesClients();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
            AppliquerRestrictionsDroits();
        }

        private void AppliquerRestrictionsDroits()
        {
            if (Session.Role == "Réceptionniste")
            {
                BtnServices.Visibility = Visibility.Collapsed;
                BtnArticles.Visibility = Visibility.Collapsed;
                // BtnUtilisateurs.Visibility = Visibility.Collapsed;
            }
            // Si c'est l'Administrateur, ils restent visibles par défaut (Visibility.Visible)
        }

        private void ChargerTousLesClients()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Requête optimisée avec IFNULL et les nouveaux paliers de fidélité
                    string query = @"
                SELECT 
                    c.id_client, 
                    c.nom, 
                    c.prenom, 
                    c.telephone, 
                    c.email, 
                    c.ville,
                    c.quartier,
                    IFNULL(SUM(co.montant_total), 0) AS chiffre_affaire,
                    CASE 
                        WHEN IFNULL(SUM(co.montant_total), 0) >= 100000 THEN 'Fidèle'
                        ELSE 'Nouveau'
                    END AS fidelite
                FROM clients c
                LEFT JOIN commandes co ON c.id_client = co.id_client
                GROUP BY c.id_client, c.nom, c.prenom, c.telephone, c.email, c.ville, c.quartier
                ORDER BY chiffre_affaire DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            dtClients.Clear();
                            da.Fill(dtClients);
                        }
                    }

                    // Liaison des données au DataGrid
                    DgClients.ItemsSource = dtClients.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des clients : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 1. SÉCURITÉ : Si la table de données n'est pas encore chargée, on arrête tout pour éviter le crash
            if (dtClients == null || dtClients.DefaultView == null || TxtRecherche == null)
                return;

            // 2. Nettoyage du texte saisi (gestion des apostrophes pour éviter les erreurs SQL)
            string filtre = TxtRecherche.Text.Replace("'", "''").Trim();

            try
            {
                if (string.IsNullOrEmpty(filtre))
                {
                    // Si la barre est vide, on réaffiche tout
                    dtClients.DefaultView.RowFilter = string.Empty;
                }
                else
                {
                    // 3. CORRECTION DU FILTRE : On vérifie bien les vrais noms de colonnes de ta table 'clients'
                    // D'après ton fichier SQL, les colonnes sont 'nom' et 'prenom'
                    dtClients.DefaultView.RowFilter = $"nom LIKE '%{filtre}%' OR prenom LIKE '%{filtre}%' OR telephone LIKE '%{filtre}%'";
                }
            }
            catch (Exception ex)
            {
                // Au lieu de crasher et de fermer la fenêtre, l'application affichera calmement l'erreur
                MessageBox.Show($"Erreur lors de la recherche : {ex.Message}", "Recherche", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // OUVRIR EN MODE AJOUT
        private void BtnNouveauClient_Click(object sender, RoutedEventArgs e)
        {
            NouveauClientWindow frm = new NouveauClientWindow();
            if (frm.ShowDialog() == true)
            {
                ChargerTousLesClients(); // Recharge le tableau si enregistré
            }
        }

        // OUVRIR EN MODE MODIFICATION (via Bouton)
        private void BtnModifierClient_Click(object sender, RoutedEventArgs e)
        {
            OuvrirFormulaireModification();
        }

        // OUVRIR EN MODE MODIFICATION (via Double-clic sur une ligne)
        private void DgClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            OuvrirFormulaireModification();
        }

        private void OuvrirFormulaireModification()
        {
            if (DgClients.SelectedItem is DataRowView rowView)
            {
                NouveauClientWindow frm = new NouveauClientWindow(rowView.Row);
                frm.Owner = this;
                if (frm.ShowDialog() == true)
                {
                    ChargerTousLesClients();
                }
            }
            else
            {
                MessageBox.Show("Veuillez sélectionner un client dans la liste à modifier.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // SUPPRESSION
        private void BtnSupprimerClient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is DataRowView row)
            {
                string nomComplet = $"{row["nom"]} {row["prenom"]}";
                int idClient = Convert.ToInt32(row["id_client"]);

                MessageBoxResult result = MessageBox.Show($"Voulez-vous supprimer '{nomComplet}' ?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using (MySqlConnection conn = new MySqlConnection(connectionString))
                        {
                            conn.Open();
                            string query = "DELETE FROM clients WHERE id_client = @id";
                            MySqlCommand cmd = new MySqlCommand(query, conn);
                            cmd.Parameters.AddWithValue("@id", idClient);
                            cmd.ExecuteNonQuery();
                        }
                        ChargerTousLesClients();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erreur : Ce client possède des commandes.\n{ex.Message}", "Impossible", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnFactures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FacturesWindow factures = new FacturesWindow();
                factures.Owner = this; // Très important !
                factures.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre de facturation : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DashboardWindow dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre du tableau de bord : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }

        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArticlesWindow articles = new ArticlesWindow();
                articles.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des articles : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            
        }
        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ServicesWindow services = new ServicesWindow();
                services.Show();
                this.Close();

            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des services : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommandesWindow fenetreCommandes = new CommandesWindow();
                fenetreCommandes.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }

}


