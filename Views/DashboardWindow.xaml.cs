using System;
using System.Collections.Generic;
using System.Data;
using System.Windows;
using ElKharis.Database;
using MySql.Data.MySqlClient;
using ElKharis.Models;
using LiveCharts;
using LiveCharts.Wpf;

namespace ElKharis.Views
{
    public partial class DashboardWindow : Window
    {
        // Déclaration des propriétés requises pour LiveCharts au niveau de la classe
        public SeriesCollection SeriesCollectionChiffre { get; set; }
        public List<string> LabelsXDates { get; set; }

        public DashboardWindow()
        {
            InitializeComponent();

            TxtUserNom.Text = Session.NomUtilisateur;
            TxtUserRole.Text = Session.Role;

            // Initialisation des objets LiveCharts avant le chargement
            SeriesCollectionChiffre = new SeriesCollection();
            LabelsXDates = new List<string>();

            RafraichirDashboard();
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



        public void RafraichirDashboard()
        {
            ChargerIndicateursDuHaut();
            ChargerGraphiqueCombinaison(); // Notre nouveau graphique dynamique
            ChargerCommandesRecentes();
            ChargerServicesPlusDemandes();
        }

        private void ChargerIndicateursDuHaut()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // 1. Commandes du jour
                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(*) FROM commandes WHERE DATE(date_commande) = CURDATE()", conn))
                    {
                        TxtCommandesJour.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }

                    // 2. Chiffre d'Affaires Cumulé Global
                    using (MySqlCommand cmd = new MySqlCommand("SELECT IFNULL(SUM(montant_verse), 0) FROM paiements", conn))
                    {
                        decimal caGlobal = Convert.ToDecimal(cmd.ExecuteScalar());
                        TxtChiffreAffaires.Text = string.Format("{0:N0}", caGlobal).Replace(",", " ");
                    }

                    // 3. Clients Actifs
                    using (MySqlCommand cmd = new MySqlCommand("SELECT COUNT(DISTINCT id_client) FROM commandes", conn))
                    {
                        TxtClientsActifs.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur indicateurs : " + ex.Message);
            }
        }

        private void ChargerGraphiqueCombinaison()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // Requête SQL pour récupérer le chiffre d'affaires des 7 derniers jours réels
                    string queryGraph = @"SELECT DATE(date_paiement) AS date_p, 
                                         SUM(montant_verse) AS total_jour
                                  FROM paiements
                                  WHERE date_paiement >= DATE_SUB(CURDATE(), INTERVAL 8 DAY)
                                  GROUP BY DATE(date_paiement)
                                  ORDER BY DATE(date_paiement) ASC";

                    // Préparation du dictionnaire pour stocker les jours (uniquement du Lundi au Vendredi)
                    var statsSemaine = new Dictionary<string, (string Label, double Montant)>();

                    // On remonte le temps pour trouver les 5 derniers jours ouvrés (du Lundi au Vendredi)
                    int joursAjoutes = 0;
                    int decalage = 6; // On regarde sur la semaine glissante

                    while (joursAjoutes < 5 && decalage >= 0)
                    {
                        DateTime dateCible = DateTime.Today.AddDays(-decalage);

                        // On exclut le Samedi (Saturday) et le Dimanche (Sunday)
                        if (dateCible.DayOfWeek != DayOfWeek.Saturday && dateCible.DayOfWeek != DayOfWeek.Sunday)
                        {
                            string key = dateCible.ToString("yyyy-MM-dd");

                            string jourFr = dateCible.DayOfWeek switch
                            {
                                DayOfWeek.Monday => "Lun.",
                                DayOfWeek.Tuesday => "Mar.",
                                DayOfWeek.Wednesday => "Mer.",
                                DayOfWeek.Thursday => "Jeu.",
                                DayOfWeek.Friday => "Ven.",
                                _ => dateCible.ToString("ddd")
                            };

                            statsSemaine.Add(key, (jourFr, 0.0));
                            joursAjoutes++;
                        }
                        decalage--;
                    }

                    // Remplissage avec les vraies valeurs de la base de données
                    using (MySqlCommand cmd = new MySqlCommand(queryGraph, conn))
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (reader["date_p"] != DBNull.Value)
                            {
                                string dateBDD = Convert.ToDateTime(reader["date_p"]).ToString("yyyy-MM-dd");
                                if (statsSemaine.ContainsKey(dateBDD))
                                {
                                    string labelExistant = statsSemaine[dateBDD].Label;
                                    double totalJour = Convert.ToDouble(reader["total_jour"]);
                                    statsSemaine[dateBDD] = (labelExistant, totalJour);
                                }
                            }
                        }
                    }

                    // Extraction finale pour LiveCharts
                    var valeursChiffre = new ChartValues<double>();
                    LabelsXDates.Clear();

                    foreach (var kvp in statsSemaine)
                    {
                        LabelsXDates.Add(kvp.Value.Label);       // Ajoutera uniquement "Lun.", "Mar.", "Mer.", "Jeu.", "Ven."
                        valeursChiffre.Add(kvp.Value.Montant);   // Montant associé
                    }

                    // Construction de l'affichage unique (Barre + Ligne de tendance)
                    SeriesCollectionChiffre = new SeriesCollection
            {
                // 1. Les Barres Bleues du Chiffre d'Affaires
                new ColumnSeries
                {
                    Title = "Chiffre d'Affaires",
                    Values = valeursChiffre,
                    Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(84, 153, 255)),
                    DataLabels = true // Montant affiché au-dessus de la barre
                },
                // 2. La ligne unique de liaison
                new LineSeries
                {
                    Title = "Évolution",
                    Values = valeursChiffre,
                    Fill = System.Windows.Media.Brushes.Transparent,
                    Stroke = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(40, 167, 69)), // Ligne verte oblique
                    PointGeometrySize = 6,
                    LineSmoothness = 0
                }
            };

                    // Injection dynamique dans le XAML
                    GraphiqueChiffre.Series = SeriesCollectionChiffre;
                    AxeXDates.Labels = LabelsXDates.ToArray();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur graphique : " + ex.Message);
            }
        }
        private void ChargerCommandesRecentes()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    // Correction ici : l'alias est bien 'nom_client' pour résoudre votre erreur XAML
                    string query = @"SELECT 
                                        CONCAT(cl.nom, ' ', IFNULL(cl.prenom, '')) AS nom,
                                        DATE_FORMAT(c.date_commande, '%H:%i') AS heure_commande,
                                        c.statut_commande,
                                        CONCAT(FORMAT(c.montant_total, 0), ' FCFA') AS montant_formatte
                                     FROM commandes c
                                     INNER JOIN clients cl ON c.id_client = cl.id_client
                                     ORDER BY c.date_commande DESC LIMIT 8";

                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);
                    LvCommandesRecentes.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur commandes récentes : " + ex.Message);
            }
        }

        private void ChargerServicesPlusDemandes()
        {
            try
            {
                using (MySqlConnection? conn = DbConnection.GetConnection())
                {
                    if (conn == null) return;

                    string query = @"SELECT s.nom_service, COUNT(dc.id) AS total_demandes
                                     FROM detail_commandes dc
                                     INNER JOIN services s ON dc.id = s.id
                                     GROUP BY s.id, s.nom_service
                                     ORDER BY total_demandes DESC LIMIT 3";

                    MySqlDataAdapter da = new MySqlDataAdapter(query, conn);
                    DataTable dt = new DataTable();
                    da.Fill(dt);

                    int maxDemandes = dt.Rows.Count > 0 ? Convert.ToInt32(dt.Rows[0]["total_demandes"]) : 10;
                    dt.Columns.Add("MaxDemandes", typeof(int));
                    foreach (DataRow row in dt.Rows) { row["MaxDemandes"] = maxDemandes; }

                    IcServicesPopulaires.ItemsSource = dt.DefaultView;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur services populaires : " + ex.Message);
            }
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Êtes-vous sûr de vouloir vous déconnecter ?", "Déconnexion", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                this.Close();
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientsWindow cltWin = new ClientsWindow();
                cltWin.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des clients : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des articles : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CommandesWindow commandes = new CommandesWindow();
                commandes.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des commandes : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnFactures_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                FacturesWindow factures = new FacturesWindow();
                factures.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des paiements et factures : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
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
                MessageBox.Show($"Erreur lors de l'ouverture de la fenêtre des services : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

    }
}