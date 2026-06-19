using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ElKharis.Services
{
    public static class DocumentService
    {
        public static void GenererDocumentPDF(int idCommande,
                                              string typeDocument,
                                              string cheminFichier)
        {
            MessageBox.Show(
                $"PDF à générer : {typeDocument}\nCommande : {idCommande}\nFichier : {cheminFichier}");
        }
    }
}
