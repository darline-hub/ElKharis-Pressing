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
using ElKharis.Database;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour CommandesWindow.xaml
    /// </summary>
        public partial class CommandesWindow : Window
        {
            private DataTable tableCommandes;

        public CommandesWindow()
        {

                tableCommandes = new DataTable();
                InitializeComponent();
                ChargerDonnees();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
        }

        // 1. CHARGEMENT DES DONNÉES DEPUIS WAMPSERVER
        public void ChargerDonnees()
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT c.id_commande, c.numero_commande, c.date_commande, 
                                        c.montant_total, c.statut_commande, cl.nom
                                 FROM commandes c
                                 LEFT JOIN clients cl ON c.id_client = cl.id_client
                                 ORDER BY c.id_commande DESC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataAdapter adapter = new MySqlDataAdapter(cmd))
                        {
                            tableCommandes = new DataTable();
                            adapter.Fill(tableCommandes);
                            DgCommandes.ItemsSource = tableCommandes.DefaultView;
                        }
                    }
                }
                CalculerStatistiques();
            }

            // 2. MISE À JOUR DES COMPTEURS DU HAUT
            private void CalculerStatistiques()
            {
                if (tableCommandes == null) return;

                // Filtrage basé sur ton champ 'statut_commande'
                int total = tableCommandes.Rows.Count;
                int attente = tableCommandes.Select("statut_commande = 'En attente'").Length;
                int cours = tableCommandes.Select("statut_commande = 'En traitement'").Length;
                int pretes = tableCommandes.Select("statut_commande = 'Prête'").Length;

                TxtStatTotal.Text = total.ToString();
                TxtStatEnAttente.Text = attente.ToString();
                TxtStatEnCours.Text = cours.ToString();
                TxtStatPretes.Text = pretes.ToString();
            }

            // 3. BARRE DE RECHERCHE INSTANTANÉE
            private void TxtRecherche_TextChanged(object sender, TextChangedEventArgs e)
            {
                if (tableCommandes == null) return;

                string filtre = TxtRecherche.Text.Trim().Replace("'", "''");
                // Filtre sur le numéro de commande ou le nom du client
                tableCommandes.DefaultView.RowFilter = $"numero_commande LIKE '%{filtre}%' OR nom_client LIKE '%{filtre}%'";
            }

        private void BtnNouvelleCommande_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // On tente d'instancier et d'afficher la fenêtre
                NouvelleCommandeWindow win = new NouvelleCommandeWindow();

                if (win.ShowDialog() == true)
                {
                    ChargerDonnees(); // On rafraîchit la liste si l'enregistrement a réussi
                }
            }
            catch (Exception ex)
            {
                // Si un bug se produit à l'initialisation, ce message va tout nous dire !
                MessageBox.Show($"Erreur critique lors de l'ouverture du formulaire : {ex.Message}\n\nDétails : {ex.InnerException?.Message}",
                                "Erreur d'ouverture",
                                MessageBoxButton.OK,
                                MessageBoxImage.Error);
            }
        }

        private void BtnRetourDashboard_Click(object sender, RoutedEventArgs e)
            {
                DashboardWindow dashboard = new DashboardWindow();
                dashboard.Show();
                this.Close();
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