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
using ElKharis.Models;

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
            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;

            // Centralisation du chargement des données
            RafraichirDashboard();
        }

        // Méthode publique pour pouvoir rafraîchir les stats depuis une autre fenêtre si nécessaire
        public void RafraichirDashboard()
        {
            ChargerStatistiques();
            ChargerCommandesRecentes();
            ChargerServicesPlusDemandes();
        }

       private void ChargerStatistiques()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // Requête SQL pour récupérer le total par jour sur les 7 derniers jours
                    string query = @"SELECT 
                                DATE(date_commande) AS date_brute,
                                DATE_FORMAT(date_commande, '%a') AS jour_nom,
                                DATE_FORMAT(date_commande, '%d') AS jour_num,
                                SUM(montant_total) AS total_jour
                             FROM commandes
                             WHERE date_commande >= DATE_SUB(CURDATE(), INTERVAL 6 DAY)
                             GROUP BY DATE(date_commande)
                             ORDER BY DATE(date_commande) ASC";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        using (MySqlDataReader reader = cmd.ExecuteReader())
                        {
                            var donneesBrutes = new List<dynamic>();
                            decimal montantMaximum = 0;

                            while (reader.Read())
                            {
                                string jourNom = reader["jour_nom"].ToString() ?? "";
                                string jourNum = reader["jour_num"].ToString() ?? "";

                                // Traduction des jours de MySQL (anglais court) vers le français
                                jourNom = jourNom.ToLower() switch
                                {
                                    "mon" => "Lun.",
                                    "tue" => "Mar.",
                                    "wed" => "Mer.",
                                    "thu" => "Jeu.",
                                    "fri" => "Ven.",
                                    "sat" => "Sam.",
                                    "sun" => "Dim.",
                                    _ => jourNom
                                };

                                string affichageJour = $"{jourNom} {jourNum}";
                                decimal total = Convert.ToDecimal(reader["total_jour"]);

                                if (total > montantMaximum) montantMaximum = total;

                                donneesBrutes.Add(new { JourLibelle = affichageJour, Total = total });
                            }

                            // Sécurité division par zéro
                            if (montantMaximum == 0) montantMaximum = 1;

                            // Échelle visuelle (pixels max de la barre)
                            double hauteurMaxGraphique = 140;

                            // Construction finale de la liste pour le Binding WPF
                            var donneesGraphique = donneesBrutes.Select(d => new
                            {
                                Jour = d.JourLibelle,
                                MontantFormatte = d.Total > 0 ? string.Format("{0:N0} F", d.Total).Replace(",", " ") : "0 F",
                                HauteurBarre = (double)(d.Total / montantMaximum) * hauteurMaxGraphique + 5
                            }).ToList();

                            // Remplissage du graphique
                            //IcGraphiqueChiffre.ItemsSource = donneesGraphique;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement du graphique sur 7 jours : " + ex.Message,
                                "Erreur graphique", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ChargerCommandesRecentes()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
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
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT 
                                        s.nom_service, 
                                        COUNT(dc.id) AS total_demandes
                                     FROM detail_commandes dc
                                     INNER JOIN services s ON dc.id = s.id
                                     GROUP BY s.id, s.nom_service
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

        // ==========================================
        //        NAVIGATION OPTIMISÉE (SUITE)
        // ==========================================

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientsWindow cltWin = new ClientsWindow();
                cltWin.Owner = this; // Démarre la relation Parent -> Enfant
                cltWin.Show();
                this.Hide(); // Cache simplement le dashboard sans le détruire
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des clients : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ArticlesWindow articles = new ArticlesWindow();
                articles.Owner = this;
                articles.Show();
                this.Hide();
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
                services.Owner = this;
                services.Show();
                this.Hide();
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
                fenetreCommandes.Owner = this; // Très important !
                fenetreCommandes.Show();
                this.Hide();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre d : {ex.Message}",
                                "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close(); // Ici le Close est normal car on quitte l'application vers le login
            }
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void BtnMenuFacturation_Click(object sender, RoutedEventArgs e)
        {
            FacturesWindow facWindow = new FacturesWindow();
            facWindow.ShowDialog();
        }
    }
}