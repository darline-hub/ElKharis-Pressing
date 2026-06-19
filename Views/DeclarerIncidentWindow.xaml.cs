using System;
using System.Windows;
using System.Windows.Controls;
using MySql.Data.MySqlClient;

namespace ElKharis.Views
{
    public partial class DeclarerIncidentWindow : Window
    {
        private readonly string connectionString = "Server=localhost;Database=pressing_elkharis;Uid=root;Pwd=;";
        private int _idDetail; // L'identifiant de la ligne de l'article concerné

        public DeclarerIncidentWindow(int idDetail, string nomArticle)
        {
            InitializeComponent();
            _idDetail = idDetail;

            // Personnalise subtilement le titre avec le nom de l'article
            this.Title = $"Incident sur : {nomArticle}";
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            // Validations de sécurité
            if (CboTypeIncident.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner le type d'incident.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (CboResponsabilite.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner le responsable.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(TxtDescription.Text))
            {
                MessageBox.Show("Veuillez décrire précisément l'incident.", "Attention", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string typeIncident = (CboTypeIncident.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string responsabilite = (CboResponsabilite.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
            string description = TxtDescription.Text.Trim();

            try
            {
                using (MySqlConnection conn = new MySqlConnection(connectionString))
                {
                    conn.Open();
                    string query = @"INSERT INTO incidents_articles (id_detail, type_incident, description, responsabilite, statut_resolution) 
                                     VALUES (@idDetail, @type, @desc, @resp, 'En attente')";

                    using (MySqlCommand cmd = new MySqlCommand(query, conn))
                    {
                        cmd.Parameters.AddWithValue("@idDetail", _idDetail);
                        cmd.Parameters.AddWithValue("@type", typeIncident);
                        cmd.Parameters.AddWithValue("@desc", description);
                        cmd.Parameters.AddWithValue("@resp", responsabilite);

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("L'incident a été enregistré avec succès.", "Succès", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true; // Ferme la boîte de dialogue en signalant la réussite
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erreur lors de l'enregistrement de l'incident : {ex.Message}", "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}