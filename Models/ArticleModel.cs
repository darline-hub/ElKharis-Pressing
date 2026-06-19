using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElKharis.Models
{
    public class ArticleModel
    {
        public int IdArticle { get; set; }
        public string NomArticle { get; set; } = string.Empty;
        public decimal Montant { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
