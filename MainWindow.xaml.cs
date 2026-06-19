using ElKharis.Views;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ElKharis
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        // Code à mettre lors du clic sur le bouton "Gestion des Articles"
        private void BtnArticles_Click(object sender, RoutedEventArgs e)
        {
            ArticlesWindow articlesWin = new ArticlesWindow();
            articlesWin.Owner = this; // Permet de centrer la fenêtre par rapport à la principale
            articlesWin.ShowDialog(); // Ouvre la fenêtre en mode bloquant
        }

        // Code à mettre lors du clic sur le bouton "Gestion des Services"
        private void BtnServices_Click(object sender, RoutedEventArgs e)
        {
            ServicesWindow servicesWin = new ServicesWindow();
            servicesWin.Owner = this;
            servicesWin.ShowDialog();
        }
    }
}