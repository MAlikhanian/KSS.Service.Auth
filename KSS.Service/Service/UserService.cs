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
            // 1. Check username not exists
            var existingUserByUsername = await _userRepository.SingleOrDefaultAsync(u => u.Username == request.Username);
            if (existingUserByUsername != null)
                throw new InvalidOperationException("Username already exists");

            // 2. Check email not exists
            var existingUserByEmail = await _userRepository.SingleOrDefaultAsync(u => u.Email == request.Email);
            if (existingUserByEmail != null)
                throw new InvalidOperationException("Email already exists");

            // 3. Check phone not exists (if provided)
            if (!string.IsNullOrEmpty(request.Phone))
            {
                var existingUserByPhone = await _userRepository.SingleOrDefaultAsync(u => u.Phone == request.Phone);
                if (existingUserByPhone != null)
                    throw new InvalidOperationException("Phone already exists");
            }

            // 4. Call Person API to create person and get PersonId
            var createPersonRequest = new CreatePersonRequestDto
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                PreferredLanguageId = 1, // Persian
                SexId = 1,
                NationalId = Guid.NewGuid().ToString("N")[..20],
                DateOfBirth = new DateTime(1990, 1, 1),
                BirthCountryId = request.CountryId ?? 1,
                BirthRegionId = 1,
                BirthCityId = 1,
                NationalityCountryId = request.CountryId ?? 1
            };

            var createdPerson = await _personApiClient.CreatePersonAsync(createPersonRequest);

            // 5. Insert user with PersonId
            var user = new User
            {
                Id = Guid.NewGuid(),
                PersonId = createdPerson.Id,
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
