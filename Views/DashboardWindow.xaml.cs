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
using ElKharis.Views;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    /// <summary>
    /// Logique d'interaction pour DashboardWindow.xaml
    /// </summary>
    public partial class DashboardWindow : Window
    {
        public DashboardWindow()
        {
            InitializeComponent();
            ChargerStatistiques();
            ChargerCommandesRecentes();
            ChargerServicesPlusDemandes();
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;
        }
        
        private void ChargerStatistiques()
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string qCommandes = "SELECT COUNT(*) FROM commandes WHERE DATE(date_commande) = CURDATE()";
                    using (MySqlCommand cmd = new MySqlCommand(qCommandes, conn))
                    {
                        int nbCommandes = Convert.ToInt32(cmd.ExecuteScalar()!);
                        TxtCommandesJour.Text = nbCommandes.ToString();
                    }

                    string qChiffre = "SELECT IFNULL(SUM(montant_total), 0) FROM commandes";
                    using (MySqlCommand cmd = new MySqlCommand(qChiffre, conn))
                    {
                        decimal chiffreAffaires = Convert.ToDecimal(cmd.ExecuteScalar()!);
                        TxtChiffreAffaires.Text = string.Format("{0:N0}", chiffreAffaires).Replace(",", " ");
                    }

                    string qClients = "SELECT COUNT(*) FROM clients";
                    using (MySqlCommand cmd = new MySqlCommand(qClients, conn))
                    {
                        int nbClients = Convert.ToInt32(cmd.ExecuteScalar()!);
                        TxtClientsActifs.Text = nbClients.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des statistiques : " + ex.Message,
                                "Erreur de données", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerCommandesRecentes()
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT 
                                CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom_client,
                                DATE_FORMAT(c.date_commande, '%H:%i') AS heure_commande,
                                c.statut_commande,
                                CONCAT(FORMAT(c.montant_total, 0), ' FCFA') AS montant_formatte
                             FROM commandes c
                             INNER JOIN clients cl ON c.id_client = cl.id_client
                             ORDER BY c.date_commande DESC
                             LIMIT 8";

                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    System.Data.DataTable dt = new System.Data.DataTable();
                    da.Fill(dt);

                    LvCommandesRecentes.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des commandes récentes : " + ex.Message,
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerServicesPlusDemandes()
        {
            try
            {
                using (MySqlConnection conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT 
                                s.nom_service, 
                                COUNT(dc.id_service) AS total_demandes
                             FROM detail_commandes dc
                             INNER JOIN services s ON dc.id_service = s.id_service
                             GROUP BY s.id_service, s.nom_service
                             ORDER BY total_demandes DESC
                             LIMIT 3";

                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    System.Data.DataTable dt = new System.Data.DataTable();
                    da.Fill(dt);

                    int maxDemandes = 10;
                    if (dt.Rows.Count > 0)
                    {
                        maxDemandes = Convert.ToInt32(dt.Rows[0]["total_demandes"]);
                    }

                    dt.Columns.Add("MaxDemandes", typeof(int));
                    foreach (System.Data.DataRow row in dt.Rows)
                    {
                        row["MaxDemandes"] = maxDemandes;
                    }
                    IcServicesPopulaires.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des services populaires : " + ex.Message,
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ClientsWindow());
        }
        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ArticlesWindow());
        }
        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            MainFrame.Navigate(new ServicesWindow());
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommandesWindow fenetreCommandes = new ElKharis.Views.CommandesWindow();
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
