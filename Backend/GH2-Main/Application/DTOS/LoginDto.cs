using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.DTOS
{
    //these dto is to tkae the data from teh frontend to get login in the system
    public class LoginDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
