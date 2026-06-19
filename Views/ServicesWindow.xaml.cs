using ElKharis.Models;
using ElKharis.Services;
using System;
using System.Windows;
using System.Windows.Controls;

namespace ElKharis.Views
{
    public partial class ServicesWindow : Window
    {
        private readonly ServiceRepository _serviceRepository = new ServiceRepository();
        private ServiceModel? _selectedService;

        public ServicesWindow()
        {
            InitializeComponent();
            ChargerServices();
        }

        private void ChargerServices()
        {
            try
            {
                DgServices.ItemsSource = null;
                DgServices.ItemsSource = _serviceRepository.GetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur lors du chargement des services : " + ex.Message,
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnAjouter_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtNomService.Text) ||
                string.IsNullOrWhiteSpace(TxtCoefficient.Text))
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            if (!decimal.TryParse(TxtCoefficient.Text, out decimal coefficient))
            {
                MessageBox.Show("Le coefficient doit être un nombre.");
                return;
            }

            try
            {
                ServiceModel service = new ServiceModel
                {
                    NomService = TxtNomService.Text.Trim(),
                    CoefficientPrix = coefficient
                };

                _serviceRepository.Add(service);

                NettoyerChamps();
                ChargerServices();

                MessageBox.Show("Service ajouté avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur lors de l'ajout du service : " + ex.Message,
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnModifier_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
            {
                MessageBox.Show("Veuillez sélectionner un service.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtNomService.Text) ||
                string.IsNullOrWhiteSpace(TxtCoefficient.Text))
            {
                MessageBox.Show("Veuillez remplir tous les champs.");
                return;
            }

            if (!decimal.TryParse(TxtCoefficient.Text, out decimal coefficient))
            {
                MessageBox.Show("Le coefficient doit être un nombre.");
                return;
            }

            try
            {
                _selectedService.NomService = TxtNomService.Text.Trim();
                _selectedService.CoefficientPrix = coefficient;

                _serviceRepository.Update(_selectedService);

                NettoyerChamps();
                ChargerServices();

                MessageBox.Show("Service modifié avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur lors de la modification du service : " + ex.Message,
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void BtnSupprimer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedService == null)
            {
                MessageBox.Show("Veuillez sélectionner un service.");
                return;
            }

            MessageBoxResult result = MessageBox.Show(
                "Voulez-vous vraiment supprimer ce service ?",
                "Confirmation",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                _serviceRepository.Delete(_selectedService.Id);

                NettoyerChamps();
                ChargerServices();

                MessageBox.Show("Service supprimé avec succès.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Erreur lors de la suppression du service : " + ex.Message,
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void DgServices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedService = DgServices.SelectedItem as ServiceModel;

            if (_selectedService != null)
            {
                TxtNomService.Text = _selectedService.NomService;
                TxtCoefficient.Text = _selectedService.CoefficientPrix.ToString();
            }
        }

        private void NettoyerChamps()
        {
            TxtNomService.Clear();
            TxtCoefficient.Clear();
            _selectedService = null;
            DgServices.SelectedItem = null;
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
                MessageBox.Show("Erreur lors de l'ouverture du tableau de bord : " + ex.Message);
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
                MessageBox.Show("Erreur lors de l'ouverture des commandes : " + ex.Message);
            }
        }

        private void BtnClients_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ClientsWindow clients = new ClientsWindow();
                clients.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'ouverture des clients : " + ex.Message);
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
                MessageBox.Show("Erreur lors de l'ouverture des factures : " + ex.Message);
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
                MessageBox.Show("Erreur lors de l'ouverture des articles : " + ex.Message);
            }
        }

        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            ChargerServices();
        }

        private void BtnQuitter_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}