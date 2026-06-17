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
        }

        private void ChargerTousLesClients()
        {
            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();

                    // Requête SQL intelligente : elle compte les commandes et détermine le statut de fidélité
                    string query = @"
                SELECT 
                    c.id_client, 
                    c.nom, 
                    c.prenom, 
                    c.sexe, 
                    c.telephone, 
                    c.email, 
                    c.ville, 
                    c.quartier, 
                    c.date_inscription,
                    COUNT(co.id_commande) AS nb_commandes,
                    CASE 
                        WHEN COUNT(co.id_commande) BETWEEN 0 AND 5 THEN 'Nouveau'
                        WHEN COUNT(co.id_commande) BETWEEN 6 AND 15 THEN 'Fidèle'
                        ELSE 'VIP'
                    END AS fidelite
                FROM clients c
                LEFT JOIN commandes co ON c.id_client = co.id_client
                GROUP BY c.id_client
                ORDER BY c.id_client ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter da = new MySqlDataAdapter(cmd))
                        {
                            DataTable dtClients = new DataTable();
                            da.Fill(dtClients);

                            // Liaison des données à ton DataGrid des clients
                            DgClients.ItemsSource = dtClients.DefaultView;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors du chargement des clients : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Sécurité pour éviter le crash au chargement du composant
            if (dtClients == null || dtClients.DefaultView == null || TxtRecherche == null)
                return;

            // Protection basique contre les injections de guillemets dans le filtre
            string filtre = TxtRecherche.Text.Replace("'", "''").Trim();

            if (string.IsNullOrEmpty(filtre))
            {
                dtClients.DefaultView.RowFilter = string.Empty;
            }
            else
            {
                dtClients.DefaultView.RowFilter = $"nom LIKE '%{filtre}%' OR prenom LIKE '%{filtre}%' OR telephone LIKE '%{filtre}%'";
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


