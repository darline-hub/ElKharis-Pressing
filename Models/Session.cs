using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElKharis.Models
{
    internal class Session
    {
        public static string NomUtilisateur { get; set; } = "Invité";
        public static string Role { get; set; } = "Non défini";
        public static int IdUtilisateur { get; set; } = 0;
        public static string Email { get; set; } = "Non defini";
    }
}
