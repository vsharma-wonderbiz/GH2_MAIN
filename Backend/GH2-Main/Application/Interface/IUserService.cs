using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Application.DTOS;
using AuthMicroservice.Application.Dtos;

namespace Application.Interface
{
    public  interface IUserService
    {
        //Task<List<UserDto>> GetAllAsync();
        Task<UserDto> GetByIdAsync(int id);
        Task<UserDto> CreateAsync(CreateUserDto createUserDto);

        Task LogoutAsync(string refreshToken);

        Task<UserDto> GetCurrentUserAsync(ClaimsIdentity identity);

        Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto loginDto);  
    }
}
