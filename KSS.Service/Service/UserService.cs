using AutoMapper;
using KSS.Dto;
using KSS.Entity;
using KSS.Helper;
using KSS.Repository.IRepository;
using KSS.Service.IService;
using System.Linq.Expressions;

namespace KSS.Service.Service
{
    public class UserService : BaseService<User, UserDto, UserDto, UserDto>, IUserService
    {
        private readonly IUserRepository _userRepository;
        private readonly IPersonApiClient _personApiClient;

        public UserService(IMapper mapper, IUserRepository repository, IPersonApiClient personApiClient) : base(mapper, repository)
        {
            _userRepository = repository;
            _personApiClient = personApiClient;
        }

        public async Task<UserDto> RegisterAsync(RegisterRequestDto request)
        {
            // Check if username already exists
            var existingUserByUsername = await _userRepository.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (existingUserByUsername != null)
            {
                throw new InvalidOperationException("Username already exists");
            }

            // Check if email already exists
            var existingUserByEmail = await _userRepository.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (existingUserByEmail != null)
            {
                throw new InvalidOperationException("Email already exists");
            }

            Guid? personId = request.PersonId;

            // If PersonId is not provided, create a new person with first and last name
            if (!personId.HasValue && !string.IsNullOrEmpty(request.FirstName) && !string.IsNullOrEmpty(request.LastName))
            {
                try
                {
                    var createPersonRequest = new CreatePersonRequestDto
                    {
                        FirstName = request.FirstName,
                        LastName = request.LastName,
                        PreferredLanguageId = 1, // Persian language ID (default for registration)
                        SexId = 1, // Default sex - should be configurable
                        NationalId = Guid.NewGuid().ToString("N")[..20], // Generate a temporary national ID
                        DateOfBirth = new DateTime(1990, 1, 1), // Default date of birth - should be configurable or from request
                        BirthCountryId = request.CountryId ?? 1, // Use CountryId from request or default
                        BirthRegionId = 1, // Default region
                        BirthCityId = 1, // Default city
                        NationalityCountryId = request.CountryId ?? 1 // Use CountryId from request or default
                    };

                    var createdPerson = await _personApiClient.CreatePersonAsync(createPersonRequest);
                    personId = createdPerson.Id;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create person record: {ex.Message}", ex);
                }
            }

            // Create new user
            var user = new User
            {
                Id = Guid.NewGuid(),
                PersonId = personId,
                Username = request.Username,
                Email = request.Email,
                Phone = request.Phone,
                CountryId = request.CountryId,
                PasswordHash = Authentication.PasswordHash(request.Password),
                IsActive = true,
                IsEmailVerified = false,
                IsPhoneVerified = false,
                FailedLoginAttempts = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);

            return _mapper.Map<UserDto>(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, string jwtSecret)
        {
            // Find user by username
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (user == null)
            {
                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Check if account is locked
            if (user.LockedUntil.HasValue && user.LockedUntil.Value > DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException($"Account is locked until {user.LockedUntil.Value:yyyy-MM-dd HH:mm:ss}");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive");
            }

            // Verify password
            if (!Authentication.PasswordVerify(user.PasswordHash, request.Password))
            {
                // Increment failed login attempts
                user.FailedLoginAttempts++;
                
                // Lock account after 5 failed attempts for 30 minutes
                if (user.FailedLoginAttempts >= 5)
                {
                    user.LockedUntil = DateTime.UtcNow.AddMinutes(30);
                }

                user.UpdatedAt = DateTime.UtcNow;
                _userRepository.Update(user);
                await _userRepository.SaveChangesAsync();

                throw new UnauthorizedAccessException("Invalid username or password");
            }

            // Reset failed login attempts on successful login
            user.FailedLoginAttempts = 0;
            user.LockedUntil = null;
            user.LastLoginAt = DateTime.UtcNow;
            user.UpdatedAt = DateTime.UtcNow;

            // Generate refresh token
            var refreshToken = Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpires = DateTime.UtcNow.AddDays(7); // 7 days expiry

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Generate JWT token
            var token = Authentication.JwtTokenGenerate(jwtSecret, user.Username, 60); // 60 minutes

            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken,
                TokenExpires = DateTime.UtcNow.AddMinutes(60),
                User = _mapper.Map<UserDto>(user)
            };
        }

        public async Task<UserDto?> GetByUsernameAsync(string username)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Username == username);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }

        public async Task<UserDto?> GetByEmailAsync(string email)
        {
            var user = await _userRepository.SingleOrDefaultAsync(u => u.Email == email);
            return user != null ? _mapper.Map<UserDto>(user) : null;
        }
    }
}
