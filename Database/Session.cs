using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElKharis.Database
{
    public static class Session
    {
        public static int IdUtilisateur { get; set; }
        public static string NomUtilisateur { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
    }
}
