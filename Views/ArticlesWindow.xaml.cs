using ElKharis.Models;
using ElKharis.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ElKharis.Views
{
    public partial class ArticlesWindow : Window
    {
        private readonly ArticleRepository _articleRepository = new ArticleRepository();
        private ArticleModel? _selectedArticle;

        public ArticlesWindow()
        {
            InitializeComponent();
            ChargerArticles();
        }

        private void ChargerArticles()
        {
            try
            {
                DgArticles.ItemsSource = null;
                DgArticles.ItemsSource = _articleRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des articles : " + ex.Message);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNomArticle.Text) ||
                string.IsNullOrWhiteSpace(TxtMontant.Text))
            {
                MessageBox.Show("Veuillez remplir le nom et le montant.");
                return;
            }

            if (!decimal.TryParse(TxtMontant.Text, out decimal montant))
            {
                MessageBox.Show("Le montant doit être un nombre.");
                return;
            }

            try
            {
                ArticleModel article = new ArticleModel
                {
                    NomArticle = TxtNomArticle.Text.Trim(),
                    Montant = montant,
                    Description = TxtDescription.Text.Trim()
                };

                _articleRepository.Add(article);
                NettoyerChamps();
                ChargerArticles();

                MessageBox.Show("Article ajouté avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'ajout de l'article : " + ex.Message);
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArticle == null)
            {
                MessageBox.Show("Veuillez sélectionner un article.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtNomArticle.Text) ||
                string.IsNullOrWhiteSpace(TxtMontant.Text))
            {
                MessageBox.Show("Veuillez remplir le nom et le montant.");
                return;
            }

            if (!decimal.TryParse(TxtMontant.Text, out decimal montant))
            {
                MessageBox.Show("Le montant doit être un nombre.");
                return;
            }

            try
            {
                _selectedArticle.NomArticle = TxtNomArticle.Text.Trim();
                _selectedArticle.Montant = montant;
                _selectedArticle.Description = TxtDescription.Text.Trim();

                _articleRepository.Update(_selectedArticle);
                NettoyerChamps();
                ChargerArticles();

                MessageBox.Show("Article modifié avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la modification de l'article : " + ex.Message);
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedArticle == null)
            {
                MessageBox.Show("Veuillez sélectionner un article.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Voulez-vous vraiment supprimer cet article ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _articleRepository.Delete(_selectedArticle.IdArticle);
                NettoyerChamps();
                ChargerArticles();

                MessageBox.Show("Article supprimé avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la suppression de l'article : " + ex.Message);
            }
        }

        private void DgArticles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedArticle = DgArticles.SelectedItem as ArticleModel;

            if (_selectedArticle != null)
            {
                TxtNomArticle.Text = _selectedArticle.NomArticle;
                TxtMontant.Text = _selectedArticle.Montant.ToString();
                TxtDescription.Text = _selectedArticle.Description;
            }
        }

        private void NettoyerChamps()
        {
            TxtNomArticle.Clear();
            TxtMontant.Clear();
            TxtDescription.Clear();
            _selectedArticle = null;
            DgArticles.SelectedItem = null;
        }

        private void BtnDashboard_Click(object sender, RoutedEventArgs e)
        {
            DashboardWindow dashboard = new DashboardWindow();
            dashboard.Show();
            this.Close();
        }

        private void BtnCommandes_Click(object sender, RoutedEventArgs e)
        {
            CommandesWindow commandes = new CommandesWindow();
            commandes.Show();
            this.Close();
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            ClientsWindow clients = new ClientsWindow();
            clients.Show();
            this.Close();
        }

        private void BtnFactures_Click(object sender, RoutedEventArgs e)
        {
            FacturesWindow factures = new FacturesWindow();
            factures.Show();
            this.Close();
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            ServicesWindow services = new ServicesWindow();
            services.Show();
            this.Close();
        }

        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            ChargerArticles();
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}