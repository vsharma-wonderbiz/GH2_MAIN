using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.DTOS;
using Application.Interface;
using AuthMicroservice.Application.Dtos;
using AuthMicroservice.Domain.Entities;
using AutoMapper;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;

namespace Application.Services
{
    public class UserService :IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public UserService(IUserRepository userRepo,IMapper mapper,IConfiguration configuration)
        {
            _userRepository = userRepo;
            _mapper = mapper;
            _configuration = configuration; 
        }

        public async Task<UserDto> GetByIdAsync(int id)
        {
            var user = await _userRepository.GetByIdAsync(id);
            if (user == null)
                throw new Exception($"User with ID {id} not found.");
            return _mapper.Map<UserDto>(user);
        }

        public async Task<UserDto> CreateAsync(CreateUserDto createUserDto)
        {
            if (string.IsNullOrEmpty(createUserDto.Username) || string.IsNullOrEmpty(createUserDto.Email) || string.IsNullOrEmpty(createUserDto.Password))
                throw new Exception("Username, email, and password are required.");

            if (await _userRepository.UsernameExistsAsync(createUserDto.Username))
                throw new Exception($"Username '{createUserDto.Username}' is already taken.");

            if (await _userRepository.EmailExistsAsync(createUserDto.Email))
                throw new Exception($"Email '{createUserDto.Email}' is already registered.");

            var user = _mapper.Map<User>(createUserDto);
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password);
            user.Role = createUserDto.Role ?? "User";

            await _userRepository.AddAsync(user);
            return _mapper.Map<UserDto>(user);
        }


        public async Task<(string AccessToken, string RefreshToken)> LoginAsync(LoginDto loginDto)
        {
            if (string.IsNullOrWhiteSpace(loginDto.Email) || string.IsNullOrWhiteSpace(loginDto.Password))
                throw new Exception("Email and password are required.");

            var user = await _userRepository.GetByEmailAsync(loginDto.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
                throw new Exception("Invalid email or password.");

            // Generate tokens (you already have this method implemented)
            var (accessToken, refreshToken) = GenerateTokens(user);

            // Hash and store refresh token in the existing RefreshToken column
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7); // use config instead of hardcoded value
            await _userRepository.UpdateAsync(user);

            return (accessToken, refreshToken); // return raw refreshToken to client (cookie)
        }


        private (string AccessToken, string RefreshToken) GenerateTokens(User user)
        {
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var EmailEncrptkey = Encoding.UTF8.GetBytes(_configuration["Jwt:EncryptionKey"]);
            var tokenHandler = new JwtSecurityTokenHandler();

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim(ClaimTypes.Email,user.Email),
                    new Claim("UserId", user.UserId.ToString())
                }),
                Expires = DateTime.UtcNow.AddDays(1),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            var accessToken = tokenHandler.WriteToken(token);
            var refreshToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            return (accessToken, refreshToken);
        }


        public async Task LogoutAsync(string refreshToken)
        {
            if (!string.IsNullOrEmpty(refreshToken))
            {
                var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
                if (user != null)
                {
                    user.RefreshToken = null;
                    user.RefreshTokenExpiry = null;
                    await _userRepository.UpdateAsync(user);
                }
            }
        }

        public async Task<UserDto> GetCurrentUserAsync(ClaimsIdentity identity)
        {
            if (identity == null || !identity.IsAuthenticated)
                throw new Exception("User is not authenticated.");

            var email = identity.FindFirst(ClaimTypes.Email)?.Value;
            var user = await _userRepository.GetByEmailAsync(email);
            //if (user == null)
            //{
            //    var oAuthUser = await _oAuthUserRepository.GetByEmailAsync(email);
            //    if (oAuthUser != null)
            //        return _mapper.Map<UserDto>(oAuthUser);
            //    throw new Exception("User not found.");
            //}
            return _mapper.Map<UserDto>(user);
        }

        
    }
}
